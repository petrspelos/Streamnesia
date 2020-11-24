using System;
using System.IO;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Newtonsoft.Json;

namespace Streamnesia.Twitch
{
    public class Bot
    {
        private readonly TwitchClient client;

        public Action<int> OnCommandSelected;
        public Action<string> OnMessageSent;
        public Action<string, int> OnVoted;
        public Action<string> OnDeathSet;
        public Action<int> DevPayload;
        public Action<string> DevCommand;
	
        Dictionary<string, DateTime> CooldownTable;

        private const string BotConfigFile = "bot-config.json";
        private const string DeveloperUserId = "24577783";

        public Bot()
        {
            if (!File.Exists(BotConfigFile))
            {
                File.WriteAllText(BotConfigFile, JsonConvert.SerializeObject(new BotConfig()));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Please fill your {BotConfigFile} in the streamnesia directory.");
                Console.ResetColor();
                throw new Exception("Config file was not found");
            }

            var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(BotConfigFile));

            if (config.BotApiKey == "YOUR-API-KEY-HERE")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("YOU MUST SETUP YOUR BOT INFORMATION IN streamnesia/bot-config.json!");
                Console.ResetColor();
                throw new Exception("Config file was default");
            }

            CooldownTable = new Dictionary<string, DateTime>();
            ConnectionCredentials credentials = new ConnectionCredentials(config.BotName, config.BotApiKey);
	        var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, config.TwitchChannelName);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;

            client.Connect();
        }
  
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            //Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("[LIVE] Streamnesia bot joined the Twitch channel.");
            Console.WriteLine("--------------------------------------");
            Console.ResetColor();
            client.SendMessage(e.Channel, "PogChamp Streamnesia is up and running!");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("!f "))
            {
                if(OnDeathSet is null)
                    return;

                var remainder = e.ChatMessage.Message.Replace("!f ", string.Empty).Trim();
                if(string.IsNullOrWhiteSpace(remainder))
                    return;
                
                OnDeathSet.Invoke(remainder);
            }
            if (e.ChatMessage.Message.StartsWith("!message "))
            {
                if(OnMessageSent is null)
                    return;

                var remainder = e.ChatMessage.Message.Replace("!message ", string.Empty).Trim();
                if(string.IsNullOrWhiteSpace(remainder))
                    return;
                
                OnMessageSent.Invoke(remainder);
            }
            else if (e.ChatMessage.Message.StartsWith("!payload"))
            {
                if(OnCommandSelected is null)
                    return;

                var id = e.ChatMessage.UserId;

                if(!CooldownTable.ContainsKey(id))
                {
                    CooldownTable.Add(id, DateTime.Now);
                }
                else
                {
                    var lastCommand = CooldownTable[id];
                    var diff = DateTime.Now - lastCommand;
                    if(diff.TotalSeconds < 5)
                    {
                        return;
                    }

                    CooldownTable[id] = DateTime.Now;
                }

                var m = e.ChatMessage.Message.Replace("!payload", string.Empty);

                var success = int.TryParse(m, out var payloadIndex);

                if(!success)
                    return;

                OnCommandSelected.Invoke(payloadIndex);
            }
            else if (e.ChatMessage.Message.StartsWith("!vote"))
            {
                if(OnVoted is null)
                    return;

                var m = e.ChatMessage.Message.Replace("!vote", string.Empty);

                var success = int.TryParse(m, out var payloadIndex);

                if(!success)
                    return;

                OnVoted.Invoke(e.ChatMessage.DisplayName, payloadIndex);
            }
            else if (int.TryParse(e.ChatMessage.Message, out var chatMessageNumber))
            {
                OnVoted.Invoke(e.ChatMessage.DisplayName, chatMessageNumber);
            }
        }
        
        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.UserId != DeveloperUserId)
                return;

            if (e.WhisperMessage.Message.StartsWith("!p"))
            {
                if (!int.TryParse(e.WhisperMessage.Message.Substring(2), out var index))
                    return;

                DevPayload.Invoke(index);
            }

            DevCommand?.Invoke(e.WhisperMessage.Message);
        }
    }

    public class BotConfig
    {
        public string BotApiKey { get; set; } = "YOUR-API-KEY-HERE";
        public string BotName { get; set; } = "YOUR-BOT-NAME";
        public string TwitchChannelName { get; set; } = "TWITCH-CHANNEL-NAME";
    }
}
