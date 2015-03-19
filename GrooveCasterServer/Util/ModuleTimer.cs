using System;
using System.Timers;

namespace GrooveCaster.Util
{
    public class ModuleTimer : IDisposable
    {
        private readonly Timer m_Timer;
        private readonly Action<ModuleTimer> m_Callback;
        private bool m_Disposed;

        private ModuleTimer(Action<ModuleTimer> p_Callback, double p_Interval, bool p_Repeat = false)
        {
            m_Callback = p_Callback;
            m_Disposed = false;

            m_Timer = new Timer()
            {
                Interval = p_Interval,
                AutoReset = p_Repeat
            };
            
            m_Timer.Elapsed += OnTimerElapsed;
        }

        ~ModuleTimer()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            m_Timer.Stop();
            m_Timer.Dispose();
        }

        private void OnTimerElapsed(object p_Sender, ElapsedEventArgs p_ElapsedEventArgs)
        {
            if (m_Disposed)
                return;

            try
            {
                m_Callback(this);
            }
            catch
            {
                Dispose();
            }
        }

        public void Start()
        {
            if (m_Disposed)
                return;

            m_Timer.Start();
        }

        public void Stop()
        {
            if (m_Disposed)
                return;

            m_Timer.Stop();
        }

        public void Cancel()
        {
            Dispose();
        }

        public static ModuleTimer SetTimeout(Action<ModuleTimer> p_Callback, double p_Interval)
        {
            var s_Timer = new ModuleTimer(p_Callback, p_Interval);
            return s_Timer;
        }

        public static ModuleTimer SetInterval(Action<ModuleTimer> p_Callback, double p_Interval)
        {
            var s_Timer = new ModuleTimer(p_Callback, p_Interval, true);
            return s_Timer;
        }

    }
}
