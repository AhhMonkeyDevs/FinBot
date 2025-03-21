﻿using Discord;
using Discord.Commands;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    public class HelpHandler : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public HelpHandler(CommandService service)
        {
            _service = service;
        }

        [Command("help"), Summary("gives you information on each command"), Remarks("(PREFIX)help(all commands)/(PREFIX)help <command>")]
        public async Task Help(params string[] arg)
        {
            switch(arg.Length)
            {
                case 0:
                    await HelpAsync();
                    break;
                case 1:
                    await HelpAsync(arg.First());
                    break;
                default:
                    await Context.Message.ReplyAsync("Please only enter one parameter");
                    break;
            }
        }

        /// <summary>
        /// Gets a list of all the available commands to the user.
        /// </summary>
        public async Task HelpAsync()
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Message.Author.ToString(),
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                    Url = Context.Message.GetJumpUrl()
                }
            };
            string guildPrefix = await Global.DeterminePrefix(Context);
            guildPrefix = guildPrefix.Replace("`", "\\`").Replace("__", "\\__").Replace("~~", "\\~~").Replace("```", "\\```");
            //guildPrefix = Regex.Replace(guildPrefix, @"`|~~|__|``````|```|\*{2,3}", $"\\");
            string description = null;
            PreconditionResult result;

            foreach (ModuleInfo module in _service.Modules)
            {
                description = null;

                foreach (CommandInfo cmd in module.Commands)
                {
                    result = await cmd.CheckPreconditionsAsync(Context);

                    if (Global.hiddenCommands.Contains(cmd.Name.ToString().ToLower())) //Took me a long while to noice...but without lowercasing the string, it'll fail, as I found out today.
                    {
                        continue;
                    }

                    if (result.IsSuccess)
                    {
                        description += $"{guildPrefix}{cmd.Aliases.First()}, \t";
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    switch (module.Name)
                    {
                        case "ModCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "Administrative commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "HelpHandler":
                            builder.AddField(x =>
                            {
                                x.Name = "Help commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "TTSCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "TTS commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;
                           
                        case "MusicModule":
                            builder.AddField(x =>
                            {
                                x.Name = "Music commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "MinecraftCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "Minecraft commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "ConfigCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "Config commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "GameCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "Game commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        default:
                            builder.AddField(x =>
                            {
                                x.Name = module.Name;
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;
                    }
                }
            }

            builder.WithCurrentTimestamp();
            builder.AddField("Help support me", "[To support the development & hosting fees of building FinBot, please help out by donating!](http://donate.finlaymitchell.ml/)");
            await Context.Message.ReplyAsync("", false, builder.Build());
        }

        /// <summary>
        /// Searches for a certain command and gives you detailed information on it.
        /// </summary>
        /// <param name="command">The name of the command the user is searching for.</param>
        public async Task HelpAsync(string command)
        {
            SearchResult result = _service.Search(Context, command);

            if (!result.IsSuccess || Global.hiddenCommands.Contains(command))
            {
                await Context.Message.ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string guild_prefix = await Global.DeterminePrefix(Context);
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };
            CommandInfo cmd;

            foreach (CommandMatch match in result.Commands)
            {
                cmd = match.Command;
                builder.AddField(x =>
                {
                    x.Name = "_ _"; //This makes an empty space, leaving the field without a name throws an exception and so this is essentially the only way to make a seemingly empty space.
                    x.Value = $"__**Aliases**__: {string.Join(", ", cmd.Aliases)}\n\n__**Summary**__: {cmd.Summary.Replace("(PREFIX)", ($"{guild_prefix}"))}\n\n__**Syntax**__: {cmd.Remarks.Replace("(PREFIX)", $"{guild_prefix}")}";
                    x.IsInline = true;
                });
            }

            await Context.Message.ReplyAsync("", false, builder.Build());
        }
    }
}