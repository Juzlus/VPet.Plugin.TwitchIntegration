namespace VPet.Plugin.TwitchIntegration
{
    public class Data
    {
        public string id {  get; set; }
        public int viewer_count { get; set; }
        public string user_name { get; set; }
    }

    public class TwitchApiHelix
    {
        public int total { get; set; }
        public Data[] data { get; set; }
    }
}
