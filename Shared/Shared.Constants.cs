namespace IllusionPlugins
{
    internal static class Constants
    {
#if RG
        internal const string Prefix = "RG";
        internal const string GameName = "Room Girl";
        internal const string StudioProcessName = "RoomStudio";
        internal const string MainGameProcessName = "RoomGirl";
#elif HC
        internal const string Prefix = "HC";
        internal const string GameName = "HoneyCome";
        internal const string StudioProcessName = "DigitalCraft";
        internal const string MainGameProcessName = "HoneyCome";
#endif
    }
}
