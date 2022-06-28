namespace Phoenix.WorkshopTool
{
    public static class Constants
    {
#if SE
        public const string SEWT_Prefix = "[_SEWT_]";
#else
        public const string SEWT_Prefix = "[_MEWT_]";
#endif
        public const string ERROR_Reflection = "Could not reflect '{0}', some functions may not work. Has the game updated?";
    }
}
