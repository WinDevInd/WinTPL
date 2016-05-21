using System;

namespace TPL.Timer
{
    public interface ITimedCallbackProvider
    {
        void SetAccuracy(TimeSpan span);
        void SetPeriodic(bool isPeriodic);
        void Start();
        void Stop();
        void Pause();
        void AddCallbackHandler(ITimedCallbackHandler handler);
    }
}
