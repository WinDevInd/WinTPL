using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    public class InstanceQM<T>
    {
        private Queue<T> ClientStack = new Queue<T>();
        private Queue<TaskCompletionSource<T>> requestQueue = new Queue<TaskCompletionSource<T>>();
        private object lockVal = new object();
        bool reuseInstances;

        public InstanceQM(int instanceCount, bool reuseInstances)
        {
            this.reuseInstances = reuseInstances;
            for (int i = 0; i < instanceCount; i++)
                ClientStack.Enqueue(Activator.CreateInstance<T>());
        }

        public int GetQueueSize()
        {
            lock(lockVal)
            {
                var count = requestQueue.Count;
                return count;
            }
        }

        public void ReleaseInstance(T client)
        {
            lock (lockVal)
            {
                if (requestQueue.Count > 0)
                {
                    if (reuseInstances)
                        requestQueue.Dequeue().TrySetResult(client);
                    else
                        requestQueue.Dequeue().TrySetResult(Activator.CreateInstance<T>());
                }
                else
                {
                    if (!reuseInstances)
                        ClientStack.Enqueue(Activator.CreateInstance<T>());
                    else
                        ClientStack.Enqueue(client);
                }
            }
        }

        public Task<T> GetFreeInstanceAsync()
        {
            lock (lockVal)
            {
                var tcs = new TaskCompletionSource<T>();
                requestQueue.Enqueue(tcs);
                ProcessQueue();
                return tcs.Task;
            }
        }

        private void ProcessQueue()
        {
            if (ClientStack.Count > 0)
            {
                requestQueue.Dequeue().TrySetResult(ClientStack.Dequeue());
            }
        }
    }
}
