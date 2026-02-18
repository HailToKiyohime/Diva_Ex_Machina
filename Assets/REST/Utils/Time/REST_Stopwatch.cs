//*********************************************************************
//  Dependencies: System
using System.Diagnostics;

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************


namespace REST.Utils
{
    public class REST_Stopwatch
    {
        private Stopwatch m_Stopwatch = new Stopwatch();
        private long m_Snapshot = 0;

        public long Snapshot => m_Snapshot;

        public void Restart()
        {
            m_Stopwatch.Restart();
        }
        
        public void Start()
        {
            m_Stopwatch.Start();
        }

        public long TakeSnapshot()
        {
            m_Snapshot = GetElapsedNanoseconds();
            return m_Snapshot;
        }

        public void Stop()
        {
            m_Stopwatch.Stop();
        }

        public long GetElapsedNanoseconds()
        {
            long elapsedTicks = m_Stopwatch.ElapsedTicks;
            return (long)((double)elapsedTicks / Stopwatch.Frequency * 1_000_000_000);
        }
    }
}
