using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VPet.Plugin.TwitchIntegration
{
    public partial class Layout : UserControl
    {
        private Main main;
        int viewerCount, followerCount, subscribersCount = 0;
        string lastFollowerUsername, lastSubscriberUsername = null;

        public Layout(Main main)
        {
            this.main = main;
            this.InitializeComponent();

            this.HidePanel();
            this.Margin = new Thickness(0, 500, 0, 0);
            this.main.MW.Main.UIGrid.Children.Insert(0, (UIElement)this);
        }

        public void ShowHidePanel()
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.HidePanel();
                return;
            }

            this.UpdateValues();
            this.ShowPanel();
        }

        private void ShowPanel()
        {
            this.Visibility = Visibility.Visible;
        }

        private void HidePanel()
        {
            this.Visibility= Visibility.Collapsed;
        }

        private async void UpdateValues()
        {
            int[] layouts = this.main.settingsPanel.GetLayoutIndex();

            if (layouts.Any(n => n >= 1 && n <= 5))
            {
                string channelName = this.main.GetFromConfig("ChannelName", null);
                string twitchOauth = this.main.GetFromConfig("TwitchOauth", null);

                if (channelName is null || twitchOauth is null)
                    return;

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {twitchOauth}");
                    httpClient.DefaultRequestHeaders.Add("Client-ID", "be2fpi23gilp70xhqaenk3rltoyecx");

                    string channelId = await GetChannelId(httpClient, channelName);
                    if (string.IsNullOrEmpty(channelId))
                        return;

                    if (layouts.Any(n => n == 1))
                        this.viewerCount = await GetVewerCount(httpClient, channelId);
                    if (layouts.Any(n => n == 2 || n == 4))
                        (this.followerCount, this.lastFollowerUsername) = await GetFollowers(httpClient, channelId);
                    if (layouts.Any(n => n == 3 || n == 5))
                        (this.subscribersCount, this.lastSubscriberUsername) = await GetSubsribers(httpClient, channelId);
                }
            }

            this.ChangeValues(layouts);
        }

        public async void ChangeValues(int[] layouts)
        {
            async Task<string> ConvertValue(int x)
            {
                int index = layouts[x];
                switch (index)
                {
                    case 1:
                        return $"👁️ {this.viewerCount.ToString()}";
                    case 2:
                        return $"🤝 {this.followerCount.ToString()}";
                    case 3:
                        return $"❤️ {this.subscribersCount.ToString()}";
                    case 4:
                        return string.IsNullOrEmpty(this.lastFollowerUsername) ? "" : $"📢 {this.lastFollowerUsername}";
                    case 5:
                        return string.IsNullOrEmpty(this.lastSubscriberUsername) ? "" : $"🎉 {this.lastSubscriberUsername}";
                    case 6:
                        string customText = await ShowInputDialog(x);
                        return customText;
                    default:
                        return "";
                }
            }

            for (int i = 0; i <= layouts.Length; i++)
            {
                Border el = (Border)FindName($"BorderLayout_{i}");
                if (el is null) continue;

                if (layouts[i] == 0) {
                    el.Visibility = Visibility.Collapsed;
                } else
                    el.Visibility = Visibility.Visible;
            }

            this.TextLayout_0_0.Text = await ConvertValue(0);
            this.TextLayout_0_1.Text = await ConvertValue(1);
            this.TextLayout_0_2.Text = await ConvertValue(2);
            this.TextLayout_1_0.Text = await ConvertValue(3);
            this.TextLayout_1_1.Text = await ConvertValue(4);
            this.TextLayout_1_2.Text = await ConvertValue(5);
        }

        private async Task<string> ShowInputDialog(int x)
        {
            var inputDialog = new InputDialog(x);
            bool? result = inputDialog.ShowDialog();
            return inputDialog.resultText;
        }

        private async Task<string> GetChannelId(HttpClient httpClient, string channelName)
        {
            string apiUrl = $"https://api.twitch.tv/helix/users?login={channelName}";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseData = await response.Content.ReadAsStringAsync();
            TwitchApiHelix helix = JsonConvert.DeserializeObject<TwitchApiHelix>(responseData);

            if (helix.data is null || helix.data.Length == 0)
                return null;

            string channelId = helix.data[0].id;
            return channelId;
        }

        private async Task<int> GetVewerCount(HttpClient httpClient, string channelId)
        {
            string apiUrl = $"https://api.twitch.tv/helix/streams?user_id={channelId}";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return 0;

            string responseData = await response.Content.ReadAsStringAsync();
            TwitchApiHelix helix = JsonConvert.DeserializeObject<TwitchApiHelix>(responseData);

            if (helix.data is null || helix.data.Length == 0)
                return 0;

            int viewerCount = helix.data[0].viewer_count;
            return viewerCount;
        }

        private async Task<(int, string)> GetFollowers(HttpClient httpClient, string channelId)
        {
            string apiUrl = $"https://api.twitch.tv/helix/channels/followers?first=1&broadcaster_id={channelId}";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return (0, "");

            string responseData = await response.Content.ReadAsStringAsync();
            TwitchApiHelix helix = JsonConvert.DeserializeObject<TwitchApiHelix>(responseData);

            int followersCount = helix.total;
            if (helix.data is null || helix.data.Length == 0)
                return (followersCount, "");

            string lastFollowerUsername = helix.data[0].user_name is null ? "" : helix.data[0].user_name;
            return (followersCount, lastFollowerUsername);
        }

        private async Task<(int, string)> GetSubsribers(HttpClient httpClient, string channelId)
        {
            string apiUrl = $"https://api.twitch.tv/helix/subscriptions?first=1&broadcaster_id={channelId}";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return (0, "");

            string responseData = await response.Content.ReadAsStringAsync();
            TwitchApiHelix helix = JsonConvert.DeserializeObject<TwitchApiHelix>(responseData);

            int subscribersCount = helix.total;
            if (helix.data is null || helix.data.Length == 0)
                return (subscribersCount, "");

            string lastSubscriberUsername = helix.data[0].user_name is null ? "" : helix.data[0].user_name;
            return (subscribersCount, lastSubscriberUsername);
        }
    }
}
