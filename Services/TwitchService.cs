﻿using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Linq;
using FinBot.Handlers;
using Discord;

namespace FinBot.Services
{
    public class TwitchService : ModuleBase<ShardedCommandContext>
    {
        public static DiscordShardedClient _client;

        public TwitchService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            //CheckLiveUsers();
            TwitchHandler.GetAccessToken().Wait();
        }

        public async void CheckLiveUsers()
        {
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
            ulong _id = 0;
            EmbedBuilder eb = new EmbedBuilder();
            List<TwitchHandler.TwitchData> userInfo;
            Color TwitchColour = new Color(100, 65, 165);
            eb.Color = TwitchColour;
            string itemVal = "";
            BsonDocument item = null;
            List<string> stringArray = new List<string>();
            List<TwitchHandler.UserStreams> userStreams = new List<TwitchHandler.UserStreams>();
            string modlogchannel = "";
            SocketTextChannel logchannel;
            Dictionary<string, ulong> AlreadySent = new Dictionary<string, ulong>();

            while (true)
            {
                foreach (SocketGuild guild in _client.Guilds)
                {
                    _id = guild.Id;
                    item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                    try
                    {
                        itemVal = item?.GetValue("TwitchUsers").ToJson();
                    }

                    catch { continue; }

                    if (itemVal != null)
                    {
                        stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();

                        foreach (string user in stringArray)
                        { 
                            userStreams = await TwitchHandler.GetStreams(user);

                            if (userStreams.Count == 0)
                            {
                                if(AlreadySent.ContainsKey(user))
                                {
                                    List<string> IdenticalKeys = AlreadySent.Where(x => x.Key == user).Select(x => x.Key).ToList();

                                    foreach(string key in IdenticalKeys)
                                    {
                                        AlreadySent.Remove(key);
                                    }
                                }
                            }

                            if(AlreadySent.ContainsKey(user))
                            {
                                continue;
                            }

                            modlogchannel = await TwitchHandler.GetTwitchChannel(guild);

                            if (modlogchannel == "0")
                            {
                                continue;
                            }

                            logchannel = guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                            userInfo = await TwitchHandler.GetTwitchInfo(user);
                            eb.Title = $"{user} is live on Twitch!";
                            eb.ImageUrl = $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{user}.jpg";
                            eb.Description = $"[Watch {user} live on Twitch!](https://twitch.tv/{user})";
                            eb.AddField("Stream information", $"title: {userStreams[0].title}\ngame name: {userStreams[0].game_name}");
                            eb.Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = userInfo[0].profile_image_url,
                                Text = $"Live started at: {userStreams[0].started_at}"
                            };
                            AlreadySent.Add(user, guild.Id);
                            //AlreadySent.Append(user);
                            await logchannel.SendMessageAsync("", false, eb.Build());
                        }
                    }
                }
            }
        }
    }
}
