namespace VPet.Plugin.TwitchIntegration
{
    public enum triggerType
    {
        ChatMessage,
        Follow,
        Sub,
        Resub,
        Bits,
        Raid
    }

    public class TriggerQueue
    {
        public string Timestamp { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public triggerType Type { get; set; }
        public string TypeChar { get; set; }
        public string Index { get; set; }
        
        public TriggerQueue(string username, string content, triggerType type, string timestamp, int index = 0)
        {
            Username = username;
            Content = content;
            Type = type;
            TypeChar = GetChar(type);
            Index = index.ToString();
            Timestamp = timestamp;
        }

        private string GetChar(triggerType type)
        {
            switch (type)
            {
                case triggerType.ChatMessage:
                    return "💬";
                case triggerType.Follow:
                    return "👋";
                case triggerType.Sub:
                    return "♔";
                case triggerType.Resub:
                    return "♛";
                case triggerType.Bits:
                    return "💎";
                case triggerType.Raid:
                    return "⚔️";
                default:
                    return null;
            }
        }
    }
}
