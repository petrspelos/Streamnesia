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
        TwitchClient client;
        private string _channel = "";
        public Action<int> OnCommandSelected;
        public Action<string> OnMessageSent;
        public Action<string, int> OnVoted;
        public Action<string> OnDeathSet;
	
        Dictionary<string, DateTime> CooldownTable;

        public Bot()
        {
            if(!File.Exists("bot-config.json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("bot-config.json is missing!");
                Console.ResetColor();
                throw new Exception("Config file was not found");
            }

            var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("bot-config.json"));

            if(config.BotApiKey == "YOUR-API-KEY-HERE")
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
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;

            client.Connect();
        }
  
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
  
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }
  
        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            client.SendMessage(e.Channel, "Streamnesia is up and running.");
            _channel = e.Channel;
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
            
        }
        
        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == "my_friend")
                client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
        }
        
        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            else
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }
    }

    public class BotConfig
    {
        public string BotApiKey { get; set; }
        public string BotName { get; set; }
        public string TwitchChannelName { get; set; }
    }
}
