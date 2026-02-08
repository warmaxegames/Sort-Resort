namespace SortResort
{
    public enum GameMode
    {
        FreePlay = 0,   // Green - no timer, no stars, no move limit
        StarMode = 1,   // Pink - stars tracked, move limit enforced, no timer
        TimerMode = 2,  // Blue - timer active, no stars, no move limit
        HardMode = 3    // Gold - timer + stars + move limit
    }
}
