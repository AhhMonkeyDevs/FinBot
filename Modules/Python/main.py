import discord
from discord.ext import commands
import motor.motor_asyncio
import aiml

import asyncio
import os
import datetime
import time
from traceback import format_exc, print_tb
import re
import sys
from typing import Union
import ctypes

from Handlers.mongo_handler import MongoDB
from Data import config
from Handlers.storage_handler import DataHelper

sys.setrecursionlimit(10**4)
ctypes.windll.kernel32.SetConsoleTitleW("FinBot")


class FinBot(commands.Bot):
    def __init__(self):
        # Initialises the actual commands.Bot class
        intents = discord.Intents.all()
        intents.members = True
        super().__init__(command_prefix=self.determine_prefix, description=config.description,
                         loop=asyncio.new_event_loop(), intents=intents, case_insensitive=True, help_command=None)
        self.guild = None
        self.error_channel = None
        self.data = DataHelper()
        self.database_handler = None
        self.mongo: Union[MongoDB, None] = None
        self.aiml_kernel = aiml.Kernel()
        if os.path.isfile("bot_brain.brn"):
            self.aiml_kernel.bootstrap(brainFile="bot_brain.brn")
        else:
            self.aiml_kernel.bootstrap(learnFiles="std-startup.xml", commands="load aiml b")
            self.aiml_kernel.saveBrain("bot_brain.brn")
        self.aiml_kernel.verbose(0)

    async def determine_prefix(self, bot, message):
        if not hasattr(message, "guild") or message.guild is None:
            return ""
        if self.mongo is None:
            return f"a{config.prefix}"
        guild_document = await self.mongo.find_by_id(self.mongo.client.finlay.guilds, message.guild.id)
        if guild_document is None or guild_document.get("prefix") is None:
            return commands.when_mentioned_or(config.prefix)(bot, message)
        else:
            guild_prefix = guild_document.get("prefix")
            return commands.when_mentioned_or(guild_prefix)(bot, message)

    @staticmethod
    def create_completed_embed(title, text):
        embed = discord.Embed(title=title, description=text, colour=discord.Colour.green(),
                              timestamp=datetime.datetime.utcnow())
        return embed

    @staticmethod
    def create_error_embed(text):
        embed = discord.Embed(title="Error", description=text, colour=discord.Colour.red(),
                              timestamp=datetime.datetime.utcnow())
        return embed

    @staticmethod
    def split_text(full_text):
        while len(full_text) > 2000:
            newline_indices = [m.end() for m in re.finditer("\n", full_text[:2000])]
            if len(newline_indices) == 0:
                to_send = full_text[:2000]
                full_text = full_text[2000:]
            else:
                to_send = full_text[:newline_indices[-1]]
                full_text = full_text[newline_indices[-1]:]
            yield to_send
        if len(full_text) > 0:
            yield full_text


def get_bot():
    bot = FinBot()
    data = DataHelper()

    @bot.event
    async def on_ready():
        bot.guild = bot.get_guild(config.monkey_guild_id)
        bot.error_channel = bot.get_channel(config.error_channel_id)
        bot.mongo = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri)
        bot.mongo = MongoDB()

        for extension_name in config.extensions:
            print("Loading Cog named {}...".format(extension_name))
            bot.load_extension("Cogs.{}".format(extension_name))
            print("Loaded cog {}!".format(extension_name))
        print("Ready!")

    # noinspection PyUnusedLocal
    @bot.event
    async def on_error(method, *args, **kwargs):
        try:
            embed = discord.Embed(title=f" {FinBot.user}  experienced an error when running.",
                                  colour=discord.Colour.red())
            embed.description = format_exc()[:2000]
            print(format_exc())

        except Exception as e:
            print("Error in sending error to discord. Error was {}".format(format_exc()))
            print("Error sending to discord was {}".format(e))

    @bot.event
    async def on_command_error(ctx, error):
        if ctx.kwargs.get("error_handled", False):
            return
        if isinstance(error, commands.CommandNotFound) or isinstance(error, commands.DisabledCommand):
            return

        if isinstance(error, commands.CheckFailure):
            await ctx.send(embed=bot.create_error_embed(
                "You don't have permission to do that, {}.".format(ctx.message.author.mention)))
            return

        try:
            embed = discord.Embed(title=f"{FinBot.user} experienced an error in a command.",
                                  colour=discord.Colour.red())
            embed.description = format_exc()[:2000]
            embed.add_field(name="Command passed error", value=error)
            embed.add_field(name="Context", value=ctx.message.content)
            print_tb(error.__traceback__)
            guild_error_channel_id = data.get("guild_error_channels", {}).get(str(ctx.guild.id), 795057163768037376)
            error_channel = bot.get_channel(guild_error_channel_id)
            await error_channel.send(embed=embed)
            await ctx.reply(embed=embed)

        except Exception as e:
            print("Error in sending error to discord. Error was {}".format(error))
            print("Error sending to discord was {}".format(e))

    return bot


if __name__ == '__main__':
    FinBot_bot = get_bot()

    try:
        FinBot_bot.run(config.token)

    except discord.errors.LoginFailure:
        time.sleep(18000)

    except discord.errors.ConnectionClosed:
        if not asyncio.get_event_loop().is_closed():
            asyncio.get_event_loop().close()
        print("Connection Closed, exiting...")
        exit()
