namespace Phoenix.WorkshopTool
{
    interface IMod
    {
        string Title { get; }
        ulong ModId { get; }
        string ModPath { get; }
    }
}
