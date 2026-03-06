namespace ZombieEscapePractice
{
    public static class TimerManager
    {
        private static List<CounterStrikeSharp.API.Modules.Timers.Timer> _activeTimers = new();

        public static void AddTimer(CounterStrikeSharp.API.Modules.Timers.Timer timer)
        {
            _activeTimers.Add(timer);
        }

        public static void CancelAll()
        {
            foreach (var timer in _activeTimers)
            {
                timer.Kill();
            }
            _activeTimers.Clear();
        }
    }
}