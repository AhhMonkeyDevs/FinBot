import asyncio
import requests
import spotipy
from spotipy.oauth2 import SpotifyClientCredentials
from functools import partial
from main import FinBot
from Data import config
from difflib import SequenceMatcher


def find_closest(title, fulltext, options):
    sorted_options = []
    for option in options:
        # Format is: "10,232,035 views", so split to before space and replace commas to get "10232035"
        view_text = option.get("viewCount").get("text").split(" ")[0].replace(",", "")
        if view_text == "No":
            view_count = 0
        else:
            view_count = int(view_text)
        ratio = SequenceMatcher(None, title, option.get("title")).ratio()
        ratio *= SequenceMatcher(None, fulltext, option.get("title")).ratio()
        sorted_options.append((option, ratio * view_count))
    sorted_options.sort(key=lambda x: x[1], reverse=True)
    print([(x[0].get("title"), x[1]) for x in sorted_options])
    return [x[0] for x in sorted_options]


def transform_duration_to_ms(duration_string):
    total_ms = 0
    split_duration = duration_string.split(":")
    for index, num in enumerate(split_duration[::-1]):
        if index == 0:
            total_ms += int(num) * 1000
        elif index == 1:
            total_ms += int(num) * 60000
        else:
            total_ms += int(num) * 3600000
    return total_ms


class SpotifySearcher:
    def __init__(self, bot: FinBot):
        self.spotify = None
        self.bot = bot
        self.ready = False
        bot.loop.run_in_executor(None, self.authenticate)

    def authenticate(self):
        credentials = SpotifyClientCredentials(client_id=config.client_Id, client_secret=config.client_secret)
        self.spotify = spotipy.Spotify(client_credentials_manager=credentials)
        self.ready = True

    def get_playlist(self, playlist):
        try:
            response = self.spotify.playlist_items(playlist)
        except (requests.exceptions.HTTPError, spotipy.SpotifyException):
            return None
        items_response = response["items"]
        playlist_as_names = []
        for item in items_response:
            name = item.get("track").get("name")
            first_artist = item.get("track").get("artists")[0].get("name")
            url = item.get("track").get("external_urls").get("spotify")
            duration = item.get("track").get("duration_ms")
            album = item.get("track").get("album", {}).get("name", "")
            if first_artist == name:
                first_artist = album.partition("(")[0].strip(" ")
            full_search = f"{first_artist} \uFEFF{name}".replace(":", "").replace("\"", "")
            playlist_as_names.append((url, full_search, duration))
        return playlist_as_names

    def get_track(self, track):
        try:
            response = self.spotify.track(track)
        except (requests.exceptions.HTTPError, spotipy.SpotifyException):
            return None
        name = response.get("name")
        first_artist = response.get("artists")[0].get("name")
        album = response.get("album", {}).get("name", "")
        if first_artist == name:
            first_artist = album.partition("(")[0].strip(" ")
        full_search = f"{first_artist} \uFEFF{name}".replace(":", "").replace("\"", "")
        return (response.get('external_urls').get('spotify'), full_search,
                response.get("duration_ms"))

    async def handle_spotify(self, media_identifier):
        while not self.ready:
            await asyncio.sleep(1)
        playlist = await self.bot.loop.run_in_executor(None, partial(self.get_playlist, media_identifier))
        if playlist is None:
            track = await self.bot.loop.run_in_executor(None, partial(self.get_track, media_identifier))
            if track is None:
                return None
            return [track]
        return playlist
