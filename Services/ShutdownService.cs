﻿using System;
using System.Diagnostics;

namespace FinBot.Services
{
    public class ShutdownService
    {
        public ShutdownService(IServiceProvider services) { }

        /// <summary>
        /// Handles appropriate features to shut down the bot.
        /// </summary>
        public void Shutdown(uint type)
        {
            if (type == 0)
            {
                Global.savePrefixes();
                Environment.Exit(0);
            }

            else
            {
                Global.savePrefixes();
                Process.Start($"{AppDomain.CurrentDomain.FriendlyName}.exe");
                Environment.Exit(1);
            }
        }
    }
}
