﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FinBot.Handlers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class ConfigCommands : ModuleBase<ShardedCommandContext>
    {
        [Command("prefix"), Summary("Sets the new bot prefix for the current guild"), Remarks("(PREFIX)prefix <new_prefix>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task prefix([Remainder] string new_prefix)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.Administrator || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "prefix", new_prefix } };
                    collection.InsertOne(document);
                    Global.UpdatePrefix(_id, new_prefix, await Global.DeterminePrefix(Context));
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("prefix", new_prefix));
                    Global.UpdatePrefix(_id, new_prefix, await Global.DeterminePrefix(Context));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Prefix updated!");
                embed.WithDescription($"Set the prefix for this guild to: {new_prefix}");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("setwelcomechannel"), Summary("Sets the channel where welcome messages for new members/leaving members are sent"), Remarks("(PREFIX)setwelcomechannel <channel>"), 
            Alias("set_welcome_channel", "welcomechannel", "welcome_channel", "welcomemessages", "welcome_messages")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task SetWelcomeChannel([Remainder] SocketChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if(channel.GetType() == typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting welcome channel");
                    eb.WithDescription($"The welcome channel type must be a text channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());

                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "welcomechannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("welcomechannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the welcome channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("membercountchannel"), Summary("Sets the membercount channel"), Remarks("(PREFIX)membercountchannel <voice_channel>"),
            Alias("setmembercountchannel", "membercount_channel", "set_membercount_channel", "setmembercount")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task SetMembercountChannel([Remainder] SocketChannel parsedChannel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if (parsedChannel.GetType() != typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting membercount channel");
                    eb.WithDescription($"You must set the membercount channel to a voice channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());
                    return;
                }

                SocketVoiceChannel channel = (SocketVoiceChannel)parsedChannel;
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "membercountchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("membercountchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the membercount channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
                string msg = $"Total Users: {Context.Guild.MemberCount}";

                if (channel.Name != msg)
                {
                    await channel.ModifyAsync(x => x.Name = msg);
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }

        [Command("enablelevelling"), Summary("Enables guild user levelling"), Remarks("(PREFIX)enablelevelling <on/off/true/false>"), Alias("enable_levelling", "levelling")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task EnableLevelling([Remainder] string toggle)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.Administrator || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                bool enabled = false;

                if (toggle == "true" || toggle == "on")
                {
                    enabled = true;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "levelling", enabled } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("levelling", enabled));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set levelling to {enabled}!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("levellingchannel"), Summary("Sets the channel where users level up messages are setn"), Remarks("(PREFIX)levellingchannel <channel>"), Alias("levelling_channel", "levelingchannel", "leveling_channel")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task LevellingChannel([Remainder] SocketChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if (channel.GetType() == typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting levelling channel");
                    eb.WithDescription($"The levelling channel type must be a text channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());

                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "levellingchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("levellingchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the level log channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("modchannel"), Summary("sets the log channel for all user infractions"), Remarks("(PREFIX)modchannel <text_channel>"), Alias("modlogchannel", "mod_channel", "mod_log_channel", "moderatorlogchannel", "loggingchannel", "logchannel")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Modchannel([Remainder] SocketChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if (channel.GetType() == typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting moderation log channel");
                    eb.WithDescription($"The moderation log channel type must be a text channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());

                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "modlogchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("modlogchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the mod log channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }

        [Command("addtwitch"), Summary("Adds a user to the Twitch notifications list"), Remarks("(PREFIX)addtwitch <twitch user>"), Alias("add_twitch")]
        public async Task NotifyTwitch(string user)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                List<TwitchHandler.TwitchData> ValidateUser = await TwitchHandler.GetTwitchInfo(user);

                if(ValidateUser.Count == 0)
                {
                    await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", $"The user {user} could not be found on Twitch.", false, Color.Red).Build());
                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                try
                {
                    string itemVal = guild?.GetValue("TwitchUsers").ToJson();
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

                    if (re.IsMatch(user))
                    {
                        EmbedBuilder errembed = new EmbedBuilder();
                        errembed.WithTitle("Error");
                        errembed.WithDescription("This user is already included in the notification list!");
                        errembed.WithColor(Color.Red);
                        errembed.WithAuthor(Context.Message.Author);
                        await Context.Message.ReplyAsync("", false, errembed.Build());
                        return;
                    }
                }

                catch { }

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "TwitchUsers", user } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Push("TwitchUsers", user));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Twitch notification user list updated!");
                embed.WithDescription($"Successfully added notifications for when {user} goes live on Twitch!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.Channel.SendMessageAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("Twitchchannel"), Summary("Sets the Twitch live notification channeL"), Remarks("(PREFIX)twitchchannel <channel>"), Alias("Twitch_channel")]
        public async Task TwitchChannel([Remainder] SocketChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if (channel.GetType() == typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting Twitch notification channel");
                    eb.WithDescription($"The Twitch notifications channel type must be a text channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());

                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "TwitchChannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("TwitchChannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the Twitch notification channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }

        [Command("suggestionchannel"), Summary("Sets the guild suggestion channel"), Remarks("(PREFIX)suggestionchannel <channel>"), Alias("suggestionschannel", "suggestion_channel", "suggestions_channel", "suggestchannel", "suggest_channel")]
        public async Task SetSuggestionChannel([Remainder] SocketChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                if (channel.GetType() == typeof(SocketVoiceChannel))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error setting suggestions channel");
                    eb.WithDescription($"The suggestions channel type must be a text channel!");
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, eb.Build());

                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "suggestionschannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("suggestionschannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the suggestions channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }
    }
}
