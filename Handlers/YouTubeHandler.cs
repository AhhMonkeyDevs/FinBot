﻿using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

/// <summary>
/// The YouTube data separated into different sections(classes).
/// </summary>
namespace FinBot.Handlers
{
    public class YouTubeModel
    {
        public class Video
        {
            public string kind { get; set; }
            public string etag { get; set; }
            public string nextPageToken { get; set; }
            public string regionCode { get; set; }
            public Pageinfo pageInfo { get; set; }
            public Item[] items { get; set; }
        }

        public class Pageinfo
        {
            public int totalResults { get; set; }
            public int resultsPerPage { get; set; }
        }

        public class Item
        {
            public string kind { get; set; }
            public string etag { get; set; }
            public Id id { get; set; }
            public Snippet snippet { get; set; }
        }

        public class Id
        {
            public string kind { get; set; }
            public string videoId { get; set; }
        }

        public class Snippet
        {
            public DateTime publishedAt { get; set; }
            public string channelId { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public Thumbnails thumbnails { get; set; }
            public string channelTitle { get; set; }
            public string liveBroadcastContent { get; set; }
        }

        public class Thumbnails
        {
            public Default _default { get; set; }
            public Medium medium { get; set; }
            public High high { get; set; }
        }

        public class Default
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Medium
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class High
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }
    }

    /// <summary>
    /// functions to search through the YouTube API and manipulate data.
    /// </summary>
    public class YouTubeSearcher
    {
        /// <summary>
        /// Searches YouTube for a particular term.
        /// </summary>
        /// <param name="url">The search term for the YouTube request.</param>
        /// <returns>The data from the search in a json format.</returns>
        public string getYouTubeApiRequest(string url)
        {
            string reponse = string.Empty;
            string fullUrl = $"https://www.googleapis.com/youtube/v3/search?key={Global.YouTubeAPIKey}{url}";
            Console.WriteLine($"YouTube API Request {fullUrl}");
            string response = string.Empty;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = httpClient.GetStringAsync(url).Result;
            }
            return response;
        }

        /// <summary>
        /// Gets the latest video from a channel.
        /// </summary>
        /// <param name="id">Id of the YouTube channel..</param>
        /// <param name="numVideos">The number of videos data to retrieve.</param>
        /// <returns>Returns the URL of the video.</returns>
        public string getLatestVideoByID(string id, int numVideos = 1)
        {
            string videoURL = string.Empty;
            string url = $"&channelId={id}&part=snippet,id&order=date&maxResults={numVideos}";
            YouTubeModel.Video videos = JsonConvert.DeserializeObject<YouTubeModel.Video>(getYouTubeApiRequest(url));
            videoURL = $"https://www.youtube.com/watch?v={videos.items[0].id.videoId}";
            return videoURL;
        }

        /// <summary>
        /// Gets a "random" video by a creator.
        /// </summary>
        /// <param name="id">Id of the YouTube channel.</param>
        /// <param name="numVideos">The number of videos data to retrieve.</param>
        /// <returns>Returns the URL of the video.</returns>
        public string getRandomVideoByID(string id, int numVideos = 50)
        {
            string videoURL = string.Empty;
            string url = $"&channelId={id}&part=snippet,id&order=date&maxResults={numVideos}";
            YouTubeModel.Video videos = JsonConvert.DeserializeObject<YouTubeModel.Video>(getYouTubeApiRequest(url));
            Random getRandom = new Random();
            int random = getRandom.Next(0, numVideos);
            videoURL = $"https://www.youtube.com/watch?v={videos.items[random].id.videoId}";
            return videoURL;
        }

        /// <summary>
        /// Searches a channel for a keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search the channel for.</param>
        /// <param name="maxResults">The maximum number of results to retrieve.</param>
        /// <returns></returns>
        public async Task<List<SearchResult>> SearchChannelsAsync(string keyword = "space", int maxResults = 5)
        {
            YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Global.YouTubeAPIKey,
                ApplicationName = this.GetType().ToString()

            });
            SearchResource.ListRequest searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = keyword;
            searchListRequest.MaxResults = maxResults;
            SearchListResponse searchListResponse = await searchListRequest.ExecuteAsync();
            return searchListResponse.Items.ToList();
        }
    }
}