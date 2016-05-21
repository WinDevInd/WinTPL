using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPL
{
    internal interface ICancelMethod
    {
        void CancelTask();
    }
    public enum Priority
    {
        Immediate,
        High,
        Medium,
        Low,
        Idle
    };

    internal class TaskCompletionEvent<T> : EventArgs
    {
        public TaskCompletionEvent(T result)
        {
            OperationResult = result;
        }
        internal T OperationResult { get; set; }
    }

    public class TaskParams
    {
        public TaskParams(Priority TaskPriority)
        {
            taskPriority = TaskPriority;
        }

        internal Queue<WeakReference> CancelOperations = new Queue<WeakReference>();

        internal void AddCancelOperation(ICancelMethod CancelMethod)
        {
            WeakReference actionRef = new WeakReference(CancelMethod);
            CancelOperations.Enqueue(actionRef);
        }
        public void CancelAllOperations()
        {
            while (CancelOperations.Count > 0)
            {
                WeakReference weakRef = CancelOperations.Dequeue();
                if(weakRef.IsAlive)
                {
                    (weakRef.Target as ICancelMethod).CancelTask();
                }
            }
        }
        public bool shouldRunOnUIThread { get; set; }

        /// <summary>
        /// Will return default value instead of throwing TaskCancelledException
        /// </summary>
        public bool suppressCancellationExceptions { get; set; }

        public bool forceCancelResponseIfExecuting { get; set; }

        internal Priority taskPriority { get; set; }
    }

    internal class TaskParams<T> : ICancelMethod
    {
        internal TaskParams(Priority TaskPriority)
        {
            taskPriority = TaskPriority;
        }
        internal bool shouldRunOnUIThread { get; set; }
        internal bool suppressCancellationExceptions { get; set; }
        internal event EventHandler<TaskCompletionEvent<T>> TaskCompleted;
        internal event EventHandler TaskFailed;
        internal Priority taskPriority { get; set; }
        internal Func<Task<T>> TaskRef { get; set; }
        internal TaskCompletionSource<T> TCS { get; set; }
        internal bool TaskCancelled { get; set; }
        public bool forceCancelResponseIfExecuting { get; set; }

        void ICancelMethod.CancelTask()
        {
            TaskCancelled = true;
        }

        internal async void ProcessExecutionQueryAsync()
        {
            T res = default(T);
            try
            {
                if (TaskCancelled)
                {
                    if (suppressCancellationExceptions)
                        TCS.SetResult(res);
                    else
                        TCS.SetException(new TaskCanceledException());
                    if (TaskFailed != null)
                        TaskFailed(this, null);
                    return;
                }
                if (shouldRunOnUIThread)
                {
                    res = await TaskRef.Invoke();
                }
                else
                    res = await Task.Run(() => TaskRef());
            }
            catch (Exception e)
            {
                TCS.SetException(e);
                if (TaskFailed != null)
                    TaskFailed(this, null);
                return;
            }

            if (TaskCancelled && forceCancelResponseIfExecuting)
            {
                if (suppressCancellationExceptions)
                    TCS.SetResult(res);
                else
                    TCS.SetException(new TaskCanceledException());

                if (TaskFailed != null)
                    TaskFailed(this, null);
                return;
            }
            if (TaskCompleted != null)
                TaskCompleted(this, new TaskCompletionEvent<T>(res));
        }

        internal void TaskParamsInitialize(Func<Task<T>> taskRef, TaskCompletionSource<T> tcs)
        {
            TCS = tcs;
            TaskRef = taskRef;
        }

    }
}
