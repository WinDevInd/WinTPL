using System;

namespace TPL.Timer
{
    public interface ITimedCallbackHandler
    {
        void OnStart();
        void OnTimedCallback(TimeSpan period);
        void OnStop();
    }
}
