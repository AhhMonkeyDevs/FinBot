﻿using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace FinBot.Handlers.AutoMod //commented code is purely for whilst I test this new fancy mee6 like system
{
    public class LevellingHandler
    {
        DiscordSocketClient _client;

        public LevellingHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.MessageReceived += AddToDB;
            //_client.MessageReceived += CheckIfLevelUp;
        }

        //public Task AddToDB(SocketMessage arg)
        //{
        //    if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel))
        //    {
        //        return Task.CompletedTask;
        //    }

        //    SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
        //    long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
        //    SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
        //    using SQLiteCommand cmd2 = new SQLiteCommand(conn);
        //    conn.Open();
        //    cmd2.CommandText = $"SELECT * FROM Levels WHERE userId = '{arg.Author.Id}' AND guildId = '{chan.Guild.Id}'";
        //    using SQLiteDataReader reader = cmd2.ExecuteReader();
        //    long ans = 0;
        //    long XP = 0;
        //    long level = 0;
        //    bool ran = false;

        //    while (reader.Read())
        //    {
        //        ran = true;     
        //        ans = Now - reader.GetInt64(2);
        //        XP = reader.GetInt64(4);
        //        level = reader.GetInt64(3);

        //      //  if (ans >= Global.MinMessageTimestamp)
        //      //  {
        //            using SQLiteCommand cmd = new SQLiteCommand(conn);
        //            XP = XP + 20;
        //            cmd.CommandText = $"UPDATE Levels SET timestamp = {Now}, XP = {XP} WHERE guildId = '{chan.Guild.Id}' AND userId = '{arg.Author.Id}'";
        //            cmd.ExecuteNonQuery();
        //      //  }

        //    //    else
        //    //    {
        //    //        break;
        //    //    }
        //    }

        //    if(!ran)
        //    {
        //        using SQLiteCommand cmd3 = new SQLiteCommand(conn);
        //        cmd3.CommandText = $"INSERT INTO Levels(userId, guildId, timestamp, level, XP) VALUES({arg.Author.Id}, {chan.Guild.Id}, {Now}, 0, 20)";
        //        cmd3.ExecuteNonQuery();
        //    }

        //    conn.Close();
        //    return Task.CompletedTask;
        //}

        //private async Task CheckIfLevelUp(SocketMessage arg)
        //{
        //    if (arg.Author.IsBot)
        //    {
        //        return;
        //    }

        //    SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
        //    long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
        //    SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
        //    using SQLiteCommand cmd = new SQLiteCommand(conn);
        //    conn.Open();
        //    cmd.CommandText = $"SELECT * FROM Levels WHERE userId = '{arg.Author.Id}' AND guildId = '{chan.Guild.Id}'";
        //    using SQLiteDataReader reader = cmd.ExecuteReader();

        //    while (reader.Read())
        //    {
        //        long level = reader.GetInt64(3);
        //        long xpReq = 0;

        //        if (level == 0)
        //        {
        //            xpReq = 50;
        //        }

        //        else
        //        {
        //            xpReq = (level * Global.LevelMultiplier);
        //        }

        //        if (reader.GetInt64(4) >= xpReq)
        //        {
        //            level += 1;
        //            using SQLiteCommand cmd1 = new SQLiteCommand(conn);
        //            cmd1.CommandText = $"UPDATE Levels SET level = {level} WHERE guildId = '{chan.Guild.Id}' AND userId = '{arg.Author.Id}'";
        //            cmd1.ExecuteNonQuery();
        //            await arg.Channel.SendMessageAsync($"Congratulations, {arg.Author.Mention} for reaching level {level}!");
        //        }
        //    }

        //    conn.Close();
        //}

        public async Task AddToDB(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }

            SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
            long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
            using SQLiteCommand cmd2 = new SQLiteCommand(conn);
            conn.Open();
            cmd2.CommandText = $"SELECT * FROM Levels WHERE userId = '{arg.Author.Id}' AND guildId = '{chan.Guild.Id}'";
            using SQLiteDataReader reader = cmd2.ExecuteReader();
            long TimeStamp = 0;
            long XP = 0;
            long level = 0;
            bool ran = false;
            long xpToNextLevel = 0;
            long totalXP = 0;

            while (reader.Read())
            {
                ran = true;
                TimeStamp = Now - reader.GetInt64(2);
                
                if (TimeStamp >= Global.MinMessageTimestamp)
                {
                    XP = reader.GetInt64(4);
                    level = reader.GetInt64(3);
                    Random r = new Random();
                    XP += r.Next(15, 25);
                    totalXP =+ XP;
                    xpToNextLevel = (long)(5 * Math.Pow(level, 2) + 50 * level + 100);
                    using SQLiteCommand cmd1 = new SQLiteCommand(conn);

                    if (XP >= xpToNextLevel)
                    {
                        level += 1;
                        XP = XP - xpToNextLevel;
                        await arg.Channel.SendMessageAsync($"Congratulations, {arg.Author.Mention} for reaching level {level}!");
                    }

                    cmd1.CommandText = $"UPDATE Levels SET timestamp = {Now}, level = {level}, XP = {XP}, totalXP = {totalXP} WHERE guildId = '{chan.Guild.Id}' AND userId = '{arg.Author.Id}'";
                    cmd1.ExecuteNonQuery();
                }

                else
                {
                    return;
                }
            }

            if (!ran)
            {
                Random r = new Random();
                totalXP = +r.Next(15, 25);
                using SQLiteCommand cmd3 = new SQLiteCommand(conn);
                cmd3.CommandText = $"INSERT INTO Levels(userId, guildId, timestamp, level, XP, totalXP) VALUES({arg.Author.Id}, {chan.Guild.Id}, {Now}, 0, {XP}, {totalXP})";
                cmd3.ExecuteNonQuery();
            }

            conn.Close();
        }
    }
}
