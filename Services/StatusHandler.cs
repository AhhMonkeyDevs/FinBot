﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace FinBot.Services
{
    public class StatusHandler : ModuleBase<ShardedCommandContext>
    {
        private DiscordShardedClient _client;

        public StatusHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
           
            Timer t = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 10, 30).TotalMilliseconds, Enabled = true };
            t.Enabled = true;
            t.Elapsed += HandleStatusChange;
            t.Start();
        }

        /// <summary>
        /// Handles changing the bot status.
        /// </summary>
        /// <param name="sender">Timer-generated variable.</param>
        /// <param name="e">Timer-generated variable.</param>
        private void HandleStatusChange(object sender, ElapsedEventArgs e)
        {
            try
            {
                string game = GetStatus();
                _client.SetGameAsync(game, null, ActivityType.Playing);
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a "random" status to set.
        /// </summary>
        /// <returns>Returns a status as a string.</returns>
        private string GetStatus()
        {
            try
            {
                string[] Activity = { $"Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", $"Using {Process.GetCurrentProcess().PrivateMemorySize64 / (1024*1024)}%" +
                    $" of memory", $"Serving {_client.Guilds.Count} servers!", "Join our support server at server.finlaymitchell.ml", "Invite the bot at bot.finlaymitchell.ml", "View our website at finbot.finlaymitchell.ml!!!" };
                Random rand = new Random();
                int index = rand.Next(Activity.Length);
                return Activity[index];
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return "oop";
            }
        }
    }
}
