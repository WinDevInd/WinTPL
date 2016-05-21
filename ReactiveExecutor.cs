using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    public class ReactiveExecutor
    {
        int initialDelayInMilliseconds, maxFreeTimeBeforeExecution;
        bool alwaysWaitBeforeNextExecution;
        bool isInPauseState = true;
        bool ignoreInitialWait;
        bool returnDefaultOnCancel;

        DateTime lastExecTime;

        Guid lastOperationId;

        InstanceQM<object> throttler = new InstanceQM<object>(1, true);

        public ReactiveExecutor(int initialDelayInMilliseconds, int maxFreeTimeBeforeExecutionInMilliseconds = 1000, bool ignoreInitialWait = false, bool alwaysWaitBeforeNextExecution = false, bool returnDefaultOnCancel = true)
        {
            this.initialDelayInMilliseconds = Math.Max(0, initialDelayInMilliseconds);
            this.alwaysWaitBeforeNextExecution = alwaysWaitBeforeNextExecution;
            this.ignoreInitialWait = ignoreInitialWait;
            this.returnDefaultOnCancel = returnDefaultOnCancel;
            this.maxFreeTimeBeforeExecution = maxFreeTimeBeforeExecutionInMilliseconds;
            lastExecTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(maxFreeTimeBeforeExecution);
        }

        public void CancelMost()
        {
            lastOperationId = Guid.NewGuid();
        }

        public Task<T> ExecuteTaskAsync<T>(Func<T> TaskReference)
        {
            return ExecuteTaskAsync<T>(TaskConverters.ConvertToFuncToTask(TaskReference));
        }

        public async Task ExecuteTaskAsync(Func<Task> TaskReference)
        {
            await ExecuteTaskAsync<int>(TaskConverters.ConvertToFuncToTask<int>(TaskReference));
        }


        public async Task ExecuteTaskAsync(Action TaskReference)
        {
            await ExecuteTaskAsync<int>(TaskConverters.ConvertToActionToTask<int>(TaskReference));
        }


        async Task<T> ExecuteTaskAsyncInternal<T>(Func<Task<T>> TaskReference, Guid operationId)
        {
            if (!ValidateOperation(operationId))
                return RespondPostValidation<T>();

            if (!ignoreInitialWait && (alwaysWaitBeforeNextExecution || isInPauseState) && initialDelayInMilliseconds > 0)
            {
                isInPauseState = false;
                await Task.Delay(initialDelayInMilliseconds);
                 if (!ValidateOperation(operationId))
                return RespondPostValidation<T>();
            }
            else
            {
                ignoreInitialWait = false;
            }

           
            isInPauseState = false;
            lastExecTime = DateTime.UtcNow;
            var response = await TaskReference.Invoke();
            return response;
        }

        bool ValidateOperation(Guid operationId)
        {
            if (lastOperationId != operationId)
                if ((DateTime.UtcNow - lastExecTime).TotalMilliseconds <= maxFreeTimeBeforeExecution)
                    return false;
            return true;
        }

        T RespondPostValidation<T>()
        {
            if (returnDefaultOnCancel)
                return default(T);
            else
                throw new TaskCanceledException();
        }


        public async Task<T> ExecuteTaskAsync<T>(Func<Task<T>> TaskReference)
        {
            Guid newOperationId = Guid.NewGuid();
            lastOperationId = newOperationId;

            var accessObj = await throttler.GetFreeInstanceAsync();
            try
            {
                var response = await ExecuteTaskAsyncInternal<T>(TaskReference, newOperationId);
                return response;
            }
            finally
            {
                throttler.ReleaseInstance(accessObj);
                if (!isInPauseState && throttler.GetQueueSize() == 0)
                {
                    if (lastOperationId == newOperationId)
                    {
                        await Task.Delay(initialDelayInMilliseconds);
                        if (lastOperationId == newOperationId)
                        {
                            isInPauseState = true;
                        }
                    }
                }
            }

        }
    }
}
