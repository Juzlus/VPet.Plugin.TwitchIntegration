using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Windows.Media;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using LinePutScript.Localization.WPF;
using System.Collections.Generic;

namespace VPet.Plugin.TwitchIntegration
{
    public class TwitchIntegration
    {
        Main main = null;
        TwitchClient twitchClient = null;
        ConnectionCredentials credentials = null;
        HttpListener listener = new HttpListener();

        string channelName = null;
        public bool isJoined = false;
        DateTime lastTimestamp = DateTime.UtcNow;
        private List<string> usernames = new List<string>();
        public List<TriggerQueue> triggerQueues = new List<TriggerQueue>();

        public void ConnectToTwitch(Main main)
        {
            const string twitchClientId = "be2fpi23gilp70xhqaenk3rltoyecx";
            const string twitchRedirectUri = "http://localhost:8080/callback/";
            const string twitchScopes = "chat:read+chat:edit+channel:moderate+channel:read:subscriptions+moderator:read:followers+channel:read:vips";
            string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={twitchClientId}&redirect_uri={twitchRedirectUri}&response_type=token&scope={twitchScopes}";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
            this.GetOauth(twitchRedirectUri, main);
        }

        private void GetOauth(string url, Main main)
        {
            listener.Prefixes.Clear();
            listener.Prefixes.Add(url);

            if (listener.IsListening)
                listener.Stop();

            listener.Start();

            bool hashConverted = false;
            bool stopListening = false;
            while (!stopListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) =>
                    {
                        string query = context.Request.Url.Query;
                        string[] queryParams = query?.TrimStart('?')?.Split('&')[0]?.Split('=');
                        string Oauth = (queryParams.Length >= 2) ? queryParams[1] : null;

                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        string responseString = (Oauth != null)
                            ? $"<html><head><meta charset=\"utf-8\"></head><body><h1>{"VPet Twitch Integration: Connection successfully completed".Translate()}</h1><h3>{"(You can now close this page)".Translate()}</h3></body></html>"
                            : $"<html><head><meta charset=\"utf-8\"></head><script>if(window.location.hash.includes('#')) window.location = window.location.hash.replace('#', '?');</script><body><h1>{"VPet Twitch Integration: Connection unsuccessful. Please try again.".Translate()}</h1></body></html>";
                        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                        response.ContentLength64 = buffer.Length;
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();

                        if (hashConverted)
                        {
                            listener.Stop();
                            stopListening = true;

                            main.SaveToConfig("TwitchOauth", Oauth);
                            main.settingsPanel.CheckConnection();
                        }
                        hashConverted = true;
                    });
                }
                catch (Exception ex) { }
            }
        }

        public async void StartListening(Main main)
        {
            this.main = main;

            this.DisconnectTwitchLib("Connecting to the channel...", Brushes.Orange);
            this.twitchClient = new TwitchClient();

            this.twitchClient.OnJoinedChannel += TwitchJoinedChannel;
            this.twitchClient.OnDisconnected += TwitchDisconnected;
            this.twitchClient.OnConnectionError += TwitchConnectionError;
            this.twitchClient.OnMessageReceived += TwitchNewMessage;
            this.twitchClient.OnNewSubscriber += TwitchNewSubscriberNotification;
            this.twitchClient.OnReSubscriber += TwitchResubNotification;
            this.twitchClient.OnRaidNotification += TwitchRaidNotification;

            this.channelName = main.GetFromConfig("ChannelName", null);
            string twitchOauth = main.GetFromConfig("TwitchOauth", null);
            if (twitchClient is null || channelName is null)
            {
            main.settingsPanel.ChangeStatus($"{"Failed to connect to channel".Translate()}: {this.channelName}!", Brushes.OrangeRed);
            return;
            }

            this.credentials = new ConnectionCredentials("VPet_Streaming_Integration", twitchOauth);
            this.twitchClient.Initialize(this.credentials, this.channelName);
            this.twitchClient.Connect();

            await Task.Delay(2000);
            if (!this.isJoined)
            {
                this.DisconnectTwitchLib($"{"Unable to connect to the channel".Translate()}: {this.channelName}!", Brushes.OrangeRed);
                return;
            }

            main.settingsPanel.ChangeStatus($"{"Connected to the channel".Translate()}: {this.channelName}", Brushes.LightSeaGreen);
        }

        public void CheckChannel()
        {
            if (this.isJoined)
                this.main.settingsPanel.ChangeStatus($"{"Connected to the channel".Translate()}: {this.channelName}", Brushes.LightSeaGreen);
        }

        private void TwitchJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.isJoined = true;
        }

        private void TwitchDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            this.isJoined = false;
            this.twitchClient?.Disconnect();
            this.DisconnectTwitchLib("Disconnected from the channel", Brushes.OrangeRed);
        }

        private void TwitchConnectionError(object sender, OnConnectionErrorArgs e)
        {
            this.DisconnectTwitchLib("An error occurred while joining the channel", Brushes.OrangeRed);
        }

        private void TwitchNewSubscriberNotification(object sender, OnNewSubscriberArgs e)
        {
            bool subscriptionActive = bool.Parse(this.main.GetFromConfig("SubscriptionActive", "false"));
            string subscriptionMessage = this.main.GetFromConfig("SubscriptionMessage", "Thanks {username} for subscribing!");

            if (!subscriptionActive)
                return;

            string username = e.Subscriber.DisplayName;
            this.AddToQueue(username, subscriptionMessage.Replace("{username}", username), triggerType.Sub);
        }

        private void TwitchResubNotification(object sender, OnReSubscriberArgs e)
        {
            bool resubActive = bool.Parse(this.main.GetFromConfig("ResubActive", "false"));
            string resubMessage = this.main.GetFromConfig("ResubMessage", "Thanks {username} for {months} months subscribing!");

            if (!resubActive)
                return;

            string username = e.ReSubscriber.DisplayName;
            string months = e.ReSubscriber.MsgParamCumulativeMonths;
            this.AddToQueue(username, resubMessage.Replace("{username}", username).Replace("{months}", months), triggerType.Resub);
        }

        private void TwitchRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            bool raidActive = bool.Parse(this.main.GetFromConfig("RaidActive", "false"));
            string raidMessage = this.main.GetFromConfig("RaidMessage", "Thanks {username} for the raid of {raid}!");

            if (!raidActive)
                return;

            string username = e.RaidNotification.DisplayName;
            string raids = e.RaidNotification.MsgParamViewerCount;
            this.AddToQueue(username, raidMessage.Replace("{username}", username).Replace("{raid}", raids), triggerType.Raid);
        }

        private async void TwitchNewMessage(object sender, OnMessageReceivedArgs e)
        {
            string message = e.ChatMessage.Message;
            string username = e.ChatMessage.Username;

            if (string.IsNullOrEmpty(message))
                return;

            string prefix = this.main.GetFromConfig("Prefix", null);
            if (!string.IsNullOrEmpty(prefix))
            {
                if (!message.StartsWith(prefix))
                    return;
                message = message.Substring(prefix.Length + 1);
            }

            if (this.usernames.Contains(username))
                return;

            if (File.Exists($"{this.main.path}//config//Blacklist.txt"))
            {
                var lines = File.ReadLines($"{this.main.path}//config//Blacklist.txt");
                var args = message.Trim().Split(' ');
                foreach (string arg in args)
                    foreach (string line in lines)
                        if (arg.ToLower() == line.ToLower().Trim())
                            return;
            }

            if (File.Exists($"{this.main.path}//config//BlockedUsers.txt"))
            {
                var lines = File.ReadLines($"{this.main.path}//config//BlockedUsers.txt");
                foreach (string line in lines)
                    if (username == line.ToLower().Trim())
                        return;
            }

            bool viewsCan = bool.Parse(this.main.GetFromConfig("ViewersCanUse", "true"));
            if (!viewsCan)
                if (!e.ChatMessage.IsVip && !e.ChatMessage.IsModerator && !e.ChatMessage.IsSubscriber)
                    return;

            bool subsCan = bool.Parse(this.main.GetFromConfig("SubscribersCanUse", "true"));
            if (!subsCan)
                if (e.ChatMessage.IsSubscriber)
                    return;

            bool modsCan = bool.Parse(this.main.GetFromConfig("ModeratorsCanUse", "true"));
            if (!modsCan)
                if (e.ChatMessage.IsVip || e.ChatMessage.IsModerator)
                    return;

            this.usernames.Add(username);
            this.AddToQueue(username, message.Replace("☒", ""), triggerType.ChatMessage);

            if (e.ChatMessage.IsModerator || e.ChatMessage.IsVip)
            {
                int modsCooldown = int.Parse(this.main.GetFromConfig("ModeratorsCooldown", "1"));
                await Task.Delay(modsCooldown * 1000);
            }
            else if (e.ChatMessage.IsSubscriber)
            {
                int subsCooldown = int.Parse(this.main.GetFromConfig("SubscribersCooldown", "1"));
                await Task.Delay(subsCooldown * 1000);
            }
            else
            {
                int viewsCooldown = int.Parse(this.main.GetFromConfig("ViewersCooldown", "5"));
                await Task.Delay(viewsCooldown * 1000);
            }

            this.usernames.Remove(username);
        }

        private async void AddToQueue(string username, string content, triggerType type)
        {
            int maxQueue = int.Parse(this.main.GetFromConfig("MaxQueue", "15"));
            int fullQueueIndex = int.Parse(this.main.GetFromConfig("FullQueueIndex", "50"));

            if (this.triggerQueues.Count >= maxQueue)
            {
                if (fullQueueIndex == 0)
                    return;
                else if (fullQueueIndex == 1)
                    this.triggerQueues.RemoveAt(0);
                else if (fullQueueIndex == 2)
                    this.triggerQueues.RemoveAt(this.triggerQueues.Count - 1);
            }

            if (this.triggerQueues.Count == 0)
                this.lastTimestamp = DateTime.UtcNow.AddSeconds(6);
            else
                this.lastTimestamp = this.lastTimestamp.AddSeconds(6);

            TriggerQueue trigger = new TriggerQueue(username, content, type, this.lastTimestamp.ToString("HH:mm:ss"));
            this.triggerQueues.Add(trigger);
            if (main.settingsPanel != null)
                this.main.settingsPanel.UpdateQueue();

            double diffTime = (this.lastTimestamp - DateTime.UtcNow).TotalMilliseconds;
            await Task.Delay((int)(diffTime > 0 ? diffTime : 0));

            if (!this.triggerQueues.Contains(trigger))
                return;

            this.triggerQueues.Remove(trigger);
            if (main.settingsPanel != null)
                this.main.settingsPanel.UpdateQueue();

            this.main.SendMsg(username, content);
        }

        public void DisconnectTwitchLib(string text, Brush color)
        {
            this.isJoined = false;
            if (this.twitchClient != null)
            {
                this.twitchClient.OnJoinedChannel -= TwitchJoinedChannel;
                this.twitchClient.OnDisconnected -= TwitchDisconnected;
                this.twitchClient.OnConnectionError -= TwitchConnectionError;
                this.twitchClient.OnMessageReceived -= TwitchNewMessage;
                this.twitchClient.OnNewSubscriber -= TwitchNewSubscriberNotification;
                this.twitchClient.OnReSubscriber -= TwitchResubNotification;
                this.twitchClient.OnRaidNotification -= TwitchRaidNotification;
            }
            this.triggerQueues.Clear();
            this.main.settingsPanel.ChangeStatus(text, color);
            this.main.settingsPanel.UpdateQueue();
        }
    }
}