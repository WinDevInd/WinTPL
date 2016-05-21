using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    public class TPL
    {
        private int tempCounter, taskThreshold, currentExecCounter;
        private bool isOnHold;

        private InstanceQM<object> AccessObjGenerator = new InstanceQM<object>(1, true);

        private Queue<Action> HighPriorityTasks = new Queue<Action>();
        private Queue<Action> LowPriorityTasks = new Queue<Action>();
        private Queue<Action> MediumPriorityTasks = new Queue<Action>();
        private Queue<Action> IdlePriorityTasks = new Queue<Action>();

        private object obj = new object();

        private int TotalTaskCount
        {
            get
            {
                return HighPriorityTasks.Count + LowPriorityTasks.Count + MediumPriorityTasks.Count + IdlePriorityTasks.Count;
            }
        }

        public TPL(int ExecutionChunkSize = 7)
        {
            taskThreshold = ExecutionChunkSize;
        }
        #region benchmarkMethods

        public Task<BenchClass<T>> BenchmarkAndExecuteTaskAsync<T>(Func<T> TaskReference, TaskParams executionQuery)
        {
            return BenchmarkAndExecuteTaskAsync<T>(TaskConverters.ConvertToFuncToTask(TaskReference), executionQuery);
        }

        public async Task<BenchClass> BenchmarkAndExecuteTaskAsync(Func<Task> TaskReference, TaskParams executionQuery)
        {
            BenchClass returnBenchResult = new BenchClass();
            returnBenchResult.executionTimeInMilliseconds = (await BenchmarkAndExecuteTaskAsync<int>(TaskConverters.ConvertToFuncToTask<int>(TaskReference), executionQuery)).executionTimeInMilliseconds;
            return returnBenchResult;
        }

        public async Task<BenchClass> BenchmarkAndExecuteTaskAsync(Action TaskReference, TaskParams executionQuery)
        {
            BenchClass returnBenchResult = new BenchClass();
            returnBenchResult.executionTimeInMilliseconds = (await BenchmarkAndExecuteTaskAsync<int>(TaskConverters.ConvertToActionToTask<int>(TaskReference), executionQuery)).executionTimeInMilliseconds;
            return returnBenchResult;
        }

        public async Task<BenchClass<T>> BenchmarkAndExecuteTaskAsync<T>(Func<Task<T>> TaskReference, TaskParams executionQuery)
        {
            DateTime startTime = DateTime.Now;
            var OperationResult = await ExecuteTaskAsync<T>(TaskReference, executionQuery);
            DateTime endTime = DateTime.Now;
            BenchClass<T> BenchmarkResult = new BenchClass<T>();
            BenchmarkResult.executionTimeInMilliseconds = (endTime - startTime).TotalMilliseconds;
            BenchmarkResult.OperationResult = OperationResult;
            return BenchmarkResult;
        }

        #endregion

        #region ExecutionMethods
        public Task<T> ExecuteTaskAsync<T>(Func<T> TaskReference, TaskParams executionQuery)
        {
            return ExecuteTaskAsync<T>(TaskConverters.ConvertToFuncToTask(TaskReference), executionQuery);
        }

        public async Task ExecuteTaskAsync(Func<Task> TaskReference, TaskParams executionQuery)
        {
            await ExecuteTaskAsync<int>(TaskConverters.ConvertToFuncToTask<int>(TaskReference), executionQuery);
        }


        public async Task ExecuteTaskAsync(Action TaskReference, TaskParams executionQuery)
        {
            await ExecuteTaskAsync<int>(TaskConverters.ConvertToActionToTask<int>(TaskReference), executionQuery);
        }

        public Task<T> ExecuteTaskAsync<T>(Func<Task<T>> TaskReference, TaskParams executionQuery)
        {
            //var res = await Task.Run(()=>TaskReference());
            //return res;
            var tcs = new TaskCompletionSource<T>();
            TaskParams<T> executionParams = new TaskParams<T>(executionQuery.taskPriority)
            {
                shouldRunOnUIThread = executionQuery.shouldRunOnUIThread,
                forceCancelResponseIfExecuting = executionQuery.forceCancelResponseIfExecuting,
                suppressCancellationExceptions = executionQuery.suppressCancellationExceptions
            };
            executionQuery.AddCancelOperation(executionParams);
            executionParams.TaskParamsInitialize(TaskReference, tcs);
            executionParams.TaskCompleted += (o, e) =>
            {

                tcs.SetResult(e.OperationResult);
                lock (obj)
                {
                    if (executionParams.taskPriority != Priority.Immediate)
                    {
                        currentExecCounter--;
                        ProcessLoop();
                    }
                }
            };
            executionParams.TaskFailed += (o, e) =>
            {
                lock (obj)
                {
                    if (executionParams.taskPriority != Priority.Immediate)
                    {
                        currentExecCounter--;
                        ProcessLoop();
                    }
                }
            };

            lock (obj)
            {
                switch (executionParams.taskPriority)
                {
                    case Priority.High:
                        HighPriorityTasks.Enqueue(executionParams.ProcessExecutionQueryAsync);
                        break;

                    case Priority.Immediate:
                        executionParams.ProcessExecutionQueryAsync();
                        return tcs.Task; //RETURNED BEFORE

                    case Priority.Low:
                        LowPriorityTasks.Enqueue(executionParams.ProcessExecutionQueryAsync);
                        break;
                    case Priority.Idle:
                        IdlePriorityTasks.Enqueue(executionParams.ProcessExecutionQueryAsync);
                        break;
                    case Priority.Medium:
                        MediumPriorityTasks.Enqueue(executionParams.ProcessExecutionQueryAsync);
                        break;
                }
                ProcessLoop();
            }
            return tcs.Task;
        }
        #endregion

        async void ProcessLoop()
        {
            var accessObj = await AccessObjGenerator.GetFreeInstanceAsync();

            if (!isOnHold)
            {
                if (TotalTaskCount > 0)
                {
                    if (currentExecCounter < taskThreshold)
                    {
                        tempCounter = taskThreshold - currentExecCounter;
                        iterateAndExecute(HighPriorityTasks, Math.Min(tempCounter, HighPriorityTasks.Count));

                        tempCounter = taskThreshold - currentExecCounter;
                        iterateAndExecute(MediumPriorityTasks, Math.Min(tempCounter, MediumPriorityTasks.Count));

                        tempCounter = taskThreshold - currentExecCounter;
                        iterateAndExecute(LowPriorityTasks, Math.Min(tempCounter, LowPriorityTasks.Count));
                    }
                    if(currentExecCounter <= 0)
                    {
                        tempCounter = taskThreshold - currentExecCounter;
                        iterateAndExecute(IdlePriorityTasks, Math.Min(tempCounter, IdlePriorityTasks.Count));
                    }
                }
            }

            AccessObjGenerator.ReleaseInstance(accessObj);
        }

        void iterateAndExecute(Queue<Action> TaskList, int count)
        {
            currentExecCounter += count;
            for (int i = 0; i < count; i++)
            {
                TaskList.Dequeue().Invoke();
            }
        }

        public bool HoldExecution()
        {
            lock(obj)
            {
                isOnHold = true;
                return true;
            }
        }

        public void UnHoldExecution()
        {
            lock (obj)
            {
                if (isOnHold)
                {
                    isOnHold = false;
                    ProcessLoop();
                }
            }
        }

    }
}
