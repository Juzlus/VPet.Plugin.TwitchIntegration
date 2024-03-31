using Panuon.WPF.UI;
using System.Windows;
using System;
using LinePutScript.Localization.WPF;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VPet.Plugin.TwitchIntegration
{
    public partial class winSettings : Window
    {
        private Main main;
        private bool isConfigLoaded = false;

        public winSettings(Main main)
        {
            this.main = main;
            main.settingsPanel = this;
            this.InitializeComponent();
            this.LoadConfig();
            this.CheckConnection();
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            this.main.twitch.ConnectToTwitch(this.main);
        }

        private void DisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            this.main.SaveToConfig("TwitchOauth", null);
            this.CheckConnection();
        }

        public void CheckConnection()
        {
            string Oauth = this.main.GetFromConfig("TwitchOauth", null);
            bool isOauth = string.IsNullOrEmpty(Oauth) ? false : true;

            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.ConnectButton.Visibility = !isOauth ? Visibility.Visible : Visibility.Collapsed;
                    this.ConnectButton.IsEnabled = !isOauth;
                    this.DisconnectButton.Visibility = isOauth ? Visibility.Visible : Visibility.Collapsed;
                    this.DisconnectButton.IsEnabled = isOauth;
                    this.StartButton.IsEnabled = isOauth;
                    this.ChangeStatus("Linked to a Twitch account", Brushes.DarkOliveGreen);
                    this.main.twitch.CheckChannel();
                    this.UpdateQueue();
                    this.Topmost = true;

                    if (!this.main.twitch.isJoined) return;
                    this.DisconnectButton.IsEnabled = false;
                    this.StartButton.IsEnabled = false;
                    this.StartButton.Visibility = Visibility.Collapsed;
                    this.StopButton.IsEnabled = true;
                    this.StopButton.Visibility = Visibility.Visible;
                    this.ChannelName.IsEnabled = false;
                    this.LayoutButton.IsEnabled = true;
                });
            }
            catch { }
        }

        public void UpdateQueue()
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (this.main.settingsPanel is null) return;
                    this.queueLimit.Text = $"{"Queue".Translate()} {this.main.twitch.triggerQueues.Count}/{this.main.GetFromConfig("MaxQueue", "50")}";
                    this.triggerQueueList.Items.Clear();
                    int index = 1;
                    foreach (TriggerQueue trigger in this.main.twitch.triggerQueues)
                    {
                        this.triggerQueueList.Items.Add(new TriggerQueue(trigger.Username, trigger.Content, trigger.Type, trigger.Timestamp, index));
                        index++;
                    }
                });
            }
            catch { }
        }

        private void StartListening(object sender, EventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.DisconnectButton.IsEnabled = false;
                    this.StartButton.IsEnabled = false;
                    this.StartButton.Visibility = Visibility.Collapsed;
                    this.StopButton.IsEnabled = true;
                    this.StopButton.Visibility = Visibility.Visible;
                    this.ChannelName.IsEnabled = false;
                    this.LayoutButton.IsEnabled = true;

                    this.main.twitch.StartListening(this.main);
                });
            }
            catch { }
        }

        private void StopListening(object sender, EventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.DisconnectButton.IsEnabled = true;
                    this.StartButton.IsEnabled = true;
                    this.StartButton.Visibility = Visibility.Visible;
                    this.StopButton.IsEnabled = false;
                    this.StopButton.Visibility = Visibility.Collapsed;
                    this.ChannelName.IsEnabled = true;
                    this.LayoutButton.IsEnabled = false;

                    this.main.twitch.DisconnectTwitchLib("Disconnected from the channel", Brushes.LightSeaGreen);
                });
            }
            catch { }
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            string fileName = btn.Name;
            this.main.OpenFile(fileName);
        }

        public void ChangeStatus(string Text, Brush Color)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.StatusText.Foreground = Color;
                    this.StatusText.Text = Text.Translate();
                });
            } catch (Exception e) { MessageBox.Show(e.Message); }
        }

        private void LoadConfig()
        {
            this.ChannelName.Text = this.main.GetFromConfig("ChannelName");
            this.ShowUsername.IsChecked = bool.Parse(this.main.GetFromConfig("ShowUsername", "true"));
            this.ReadUsername.IsChecked = bool.Parse(this.main.GetFromConfig("ReadUsername", "false"));
            this.Prefix.Text = this.main.GetFromConfig("Prefix", "null");
            this.MaxQueue.Value = int.Parse(this.main.GetFromConfig("MaxQueue", "15"));
            this.FullQueueIndex.SelectedIndex = int.Parse(this.main.GetFromConfig("FullQueueIndex", "50"));
            this.ViewersCooldown.Value = int.Parse(this.main.GetFromConfig("ViewersCooldown", "5"));
            this.SubscribersCooldown.Value = int.Parse(this.main.GetFromConfig("SubscribersCooldown", "1"));
            this.ModeratorsCooldown.Value = int.Parse(this.main.GetFromConfig("ModeratorsCooldown", "1"));
            this.ViewersCanUse.IsChecked = bool.Parse(this.main.GetFromConfig("ViewersCanUse", "true"));
            this.SubscribersCanUse.IsChecked = bool.Parse(this.main.GetFromConfig("SubscribersCanUse", "true"));
            this.ModeratorsCanUse.IsChecked = bool.Parse(this.main.GetFromConfig("ModeratorsCanUse", "true"));
            //this.FollowActive.IsChecked = bool.Parse(this.main.GetFromConfig("FollowActive", "false"));
            this.SubscriptionActive.IsChecked = bool.Parse(this.main.GetFromConfig("SubscriptionActive", "false"));
            this.ResubActive.IsChecked = bool.Parse(this.main.GetFromConfig("ResubActive", "false"));
            this.RaidActive.IsChecked = bool.Parse(this.main.GetFromConfig("RaidActive", "false"));
            //this.FollowMessage.Text = this.main.GetFromConfig("FollowMessage", "Thanks {username} for following up!");
            this.SubscriptionMessage.Text = this.main.GetFromConfig("SubscriptionMessage", "Thanks {username} for subscribing!");
            this.ResubMessage.Text = this.main.GetFromConfig("ResubMessage", "Thanks {username} for {months} months subscribing!");
            this.RaidMessage.Text = this.main.GetFromConfig("RaidMessage", "Thanks {username} for the raid of {raid}!");
            this.Layout_0_0.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_0_0", "0"));
            this.Layout_0_1.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_0_1", "0"));
            this.Layout_0_2.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_0_2", "0"));
            this.Layout_1_0.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_1_0", "0"));
            this.Layout_1_1.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_1_1", "0"));
            this.Layout_1_2.SelectedIndex = int.Parse(this.main.GetFromConfig("Layout_1_2", "0"));
            this.ReadUsername.IsEnabled = (bool)this.ShowUsername.IsChecked;
            this.isConfigLoaded = true;
        }

        private void ChangeSwitch(object sender, RoutedEventArgs e)
        {
            if (!this.isConfigLoaded) return;
            Switch toogle = (Switch)sender;
            this.main.SaveToConfig(toogle.Name, toogle.IsChecked.ToString());

            if (toogle.Name == this.ShowUsername.Name)
            {
                this.main.SaveToConfig(this.ReadUsername.Name, toogle.IsChecked == false ? "false" : this.ReadUsername.IsChecked.ToString());
                this.ReadUsername.IsEnabled = (bool)toogle.IsChecked;
            }
        }

        private void ChangeTextBox(object sender, TextChangedEventArgs e)
        {
            if (!this.isConfigLoaded) return;
            TextBox textBox = (TextBox)sender;
            this.main.SaveToConfig(textBox.Name, string.IsNullOrEmpty(textBox.Text) ? null : textBox.Text);
        }

        private void ChangeComboBox(object sender, SelectionChangedEventArgs e)
        {
            if (!this.isConfigLoaded) return;
            ComboBox comboBox = (ComboBox)sender;
            this.main.SaveToConfig(comboBox.Name, comboBox.SelectedIndex.ToString());
        }

        private void ChangeComboBoxLayout(object sender, SelectionChangedEventArgs e)
        {
            if (!this.isConfigLoaded) return;
            ComboBox comboBox = (ComboBox)sender;
            this.main.SaveToConfig(comboBox.Name, comboBox.SelectedIndex.ToString());
            this.main.layoutPanel.ChangeValues(this.GetLayoutIndex());
        }

        private void ChangeSlider(object sender, DragCompletedEventArgs e)
        {
            if (!this.isConfigLoaded) return;
            Slider slider = (Slider)sender;
            this.main.SaveToConfig(slider.Name, slider.Value.ToString());
        }

        private async void ShowLayout(object sender, RoutedEventArgs e)
        {
            this.main.layoutPanel.ShowHidePanel();
        }

        public int[] GetLayoutIndex()
        {
            int[] layouts = new int[] {
                this.Layout_0_0.SelectedIndex,
                this.Layout_0_1.SelectedIndex,
                this.Layout_0_2.SelectedIndex,
                this.Layout_1_0.SelectedIndex,
                this.Layout_1_1.SelectedIndex,
                this.Layout_1_2.SelectedIndex
            };

            return layouts;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.main.settingsPanel = (winSettings)null;
        }
    }
}