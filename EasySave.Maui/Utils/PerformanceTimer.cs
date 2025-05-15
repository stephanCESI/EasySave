using System;
using System.Diagnostics;

namespace EasySave.Maui.Utils
{
    public class PerformanceTimer
    {
        private Stopwatch _stopwatch;

        public PerformanceTimer()
        {
            _stopwatch = new Stopwatch();
        }

        public void Start()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public double GetElapsedMilliseconds()
        {
            return _stopwatch.Elapsed.TotalMilliseconds;
        }

        public void Reset()
        {
            _stopwatch.Reset();
        }

        public double Measure(Action action)
        {
            try
            {
                Start();
                action();
                Stop();
                return GetElapsedMilliseconds();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur pendant la mesure de performance : {ex.Message}");
                return -1;
            }
        }
    }
}
