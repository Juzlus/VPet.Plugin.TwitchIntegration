using LinePutScript.Localization.WPF;
using System.Windows.Controls;
using System.Windows;
using VPet_Simulator.Windows.Interface;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

namespace VPet.Plugin.TwitchIntegration
{
    public class Main : MainPlugin
    {
        public string path;
        public string petName;
        private string configPath;

        public TwitchIntegration twitch;
        public winSettings settingsPanel;
        public Layout layoutPanel;

        public override string PluginName => nameof(Plugin.TwitchIntegration);

        public Main(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            this.Setup();
            this.CreateMenuItem(nameof(Plugin.TwitchIntegration));
        }

        private void Setup()
        {
            this.path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            this.petName = this.MW.Core.Save.Name;
            this.configPath = $"{this.path}//config//config.cfg";

            this.layoutPanel = new Layout(this);
            this.twitch = new TwitchIntegration();
        }

        private void CreateMenuItem(string buttonName)
        {
            MenuItem menuModConfig = this.MW.Main.ToolBar.MenuMODConfig;
            menuModConfig.Visibility = Visibility.Visible;

            MenuItem menuItem = new MenuItem()
            {
                Header = buttonName.Translate(),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            menuItem.Click += (RoutedEventHandler)((s, e) => this.Setting());
            menuModConfig.Items.Add((object)menuItem);
        }

        public override void Setting()
        {
            if (this.settingsPanel == null)
            {
                this.settingsPanel = new winSettings(this);
                this.settingsPanel.Show();
            }
            else
                this.settingsPanel.Topmost = true;
        }

        public void SaveToConfig(string key, string value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(this.configPath))
                xmlDoc.Load(this.configPath);
            else
            {
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("config.cfg");
                xmlDoc.AppendChild(root);
            }

            XmlElement node = xmlDoc.SelectSingleNode($"//{key}") as XmlElement;
            if (node is null)
            {
                node = xmlDoc.CreateElement(key);
                xmlDoc.DocumentElement?.AppendChild(node);
            }
            node.InnerText = value;
            try {
                xmlDoc.Save(this.configPath);
            } catch { }
        }

        public string GetFromConfig(string key, string alt = null)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (!File.Exists(this.configPath))
                return alt;

            xmlDoc.Load(this.configPath);
            XmlNode node = xmlDoc.SelectSingleNode($"//{key}");
            if (node != null)
                return node.InnerText;

            return alt;
        }

        public async void OpenFile(string fileName)
        {
            string filePath = $"{this.path}//config//{fileName}.txt";
            if (!File.Exists(filePath)) {
                File.Open(filePath, FileMode.Create);
                await Task.Delay(1000);
                if (File.Exists(filePath))
                    Process.Start(filePath);
            } else
                Process.Start(filePath);
        }

        public async void SendMsg(string msgAuthor, string msgContent)
        {
            bool showUsername = bool.Parse(this.GetFromConfig("ShowUsername", "true"));
            bool readUsername = bool.Parse(this.GetFromConfig("ReadUsername", "false"));
            if (!showUsername)
                msgAuthor = this.petName;
            
            Action sendMsg = async () =>
            {
                this.MW.Core.Save.Name = msgAuthor;
                this.MW.Main.Say(readUsername ? $"{msgAuthor}: {msgContent}" : msgContent);
            };

            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(sendMsg);

            await Task.Delay(1000);
            this.MW.Core.Save.Name = this.petName;
        }
    }
}
