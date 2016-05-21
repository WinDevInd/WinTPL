using System;
using System.Collections.Generic;
using System.Linq;

namespace TPL.Timer
{
    class DelayedRunnable
    {
        public Action action { get; set; }
        public TimeSpan delayedBy { get; set; }
        public object Identifier { get; set; }
    }

    public class DelayedCallbackProvider : ITimedCallbackHandler
    {
        private object syncObj;
        private ITimedCallbackProvider callbackProvider;
        private TimeSpan accuracy;
        private Dictionary<object, DelayedRunnable> delayedActions;
        public DelayedCallbackProvider(ITimedCallbackProvider callbackProvider, TimeSpan accuracy)
        {
            this.callbackProvider = callbackProvider;
            this.accuracy = accuracy;
            delayedActions = new Dictionary<object, DelayedRunnable>();
            syncObj = new object();
        }

        public void StartLooper()
        {
            callbackProvider.AddCallbackHandler(this);
            callbackProvider.SetAccuracy(accuracy);
            callbackProvider.SetPeriodic(true);
            callbackProvider.Start();
        }

        public void PostDelayed(Action action, TimeSpan delayedBy, object identifier)
        {
            lock (syncObj)
            {
                if (!delayedActions.ContainsKey(identifier))
                {
                    delayedActions.Add(identifier, new DelayedRunnable { action = action, delayedBy = delayedBy });
                }
            }
        }

        public bool TryCancel(object identifier)
        {
            lock (syncObj)
            {
                if (delayedActions.ContainsKey(identifier))
                {
                    delayedActions.Remove(identifier);
                    return true;
                }
                else
                    return false;
            }
        }

        public void StopLooper()
        {
            lock(syncObj)
            {
                callbackProvider.Stop();
            }
        }

        void ITimedCallbackHandler.OnStart()
        {
            //don't have to do anything
        }

        void ITimedCallbackHandler.OnStop()
        {
            //don't have to do anything
        }

        void ITimedCallbackHandler.OnTimedCallback(TimeSpan period)
        {
            lock (syncObj)
            {
                var keys = delayedActions.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var delayedRunnable = delayedActions[keys[i]];
                    if (delayedRunnable.delayedBy < accuracy)
                    {
                        delayedActions.Remove(keys[i]);
                        if (delayedRunnable.action != null)
                        {
                            delayedRunnable.action();
                        }
                    }
                    else
                    {
                        delayedRunnable.delayedBy -= accuracy;
                    }
                }
            }
        }
    }
}
