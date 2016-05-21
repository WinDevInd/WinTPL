using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    public class ConcurrentTaskManager<T>
    {
        ConcurrentDictionary<string, InstanceQM<T>> RedundantTaskThrottler = new ConcurrentDictionary<string, InstanceQM<T>>();

        public async Task<T> SendOperationKeyAndWaitForAccessAsync(string operationKey)
        {
            bool isNewInstance = false;
            var throttler = RedundantTaskThrottler.AddOrUpdate(operationKey, (key) => { return null; }, (key, oldValue) =>
             {
                 if (oldValue == null)
                 {
                     isNewInstance = true;
                     return new InstanceQM<T>(1, true);
                 }
                 else
                     return oldValue;
             });

            if (throttler != null)
            {
                if (isNewInstance)
                {
                    await throttler.GetFreeInstanceAsync();
                }
                var obj = await throttler.GetFreeInstanceAsync();
                throttler.ReleaseInstance(obj);
                return obj;
            }
            else
                return default(T);
        }

        public void ReleaseOperation(string operationKey, T operationResult)
        {
            InstanceQM<T> throttler = null;
            var isSuccess = RedundantTaskThrottler.TryGetValue(operationKey, out throttler);
            if (isSuccess)
            {
                if (throttler != null)
                {
                    throttler.ReleaseInstance(operationResult);
                }
                RedundantTaskThrottler.TryRemove(operationKey, out throttler);
            }
        }
    }
}
