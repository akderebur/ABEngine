using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
    public static class CoroutineManager
    {
        static List<TaskInfo> taskInfos = new List<TaskInfo>();
        internal static TaskInfo createLockTask = null;

        public static void StartCoroutine(string taskName, Func<Task> taskFunc)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var taskInfo = new TaskInfo()
            {
                taskName = taskName,
                cancelTokenSource = cts,
            };

            Func<CancellationToken, Task> cancellationAwareTaskFunc = async (token) =>
            {
                try
                {
                    await taskFunc();
                }
                catch (OperationCanceledException)
                {
                    if(createLockTask == taskInfo)
                        EntityManager.creationSemaphore.Release();
                }
            };


            var task = cancellationAwareTaskFunc(cts.Token).ContinueWith((t) =>
            {
                if (createLockTask == taskInfo)
                    EntityManager.creationSemaphore.Release();

                if (taskInfos.Contains(taskInfo))
                    taskInfos.Remove(taskInfo);
            });

            taskInfo.task = task;

            taskInfos.Add(taskInfo);
        }

        //public static void StartCoroutine(string taskName, Func<CancellationToken, Task> taskFunc)
        //{
        //    CancellationTokenSource cts = new CancellationTokenSource();
        //    var taskInfo = new TaskInfo()
        //    {
        //        taskName = taskName,
        //        cancelTokenSource = cts
        //    };

        //    var task = taskFunc(cts.Token).ContinueWith((t) =>
        //    {
        //        if (taskInfos.Contains(taskInfo))
        //            taskInfos.Remove(taskInfo);
        //    });

        //    taskInfo.task = task;

        //    taskInfos.Add(taskInfo);
        //}


        public static void StopCoroutine(string taskName)
        {
            for (int i = 0; i < taskInfos.Count; i++)
            {
                var taskInfo = taskInfos[i];

                if (taskInfo.taskName.Equals(taskName))
                {
                    taskInfo.cancelTokenSource.Cancel();
                    taskInfos.Remove(taskInfo);
                }

            }
        }

        public static void StopAllCoroutines()
        {
            for (int i = 0; i < taskInfos.Count; i++)
            {
                taskInfos[i].cancelTokenSource.Cancel();
            }
            taskInfos.Clear();
            EntityManager.creationSemaphore.Release();
            createLockTask = null;
        }

        public static async Task DelayCoroutine(this Task task, float waitSeconds)
        {
            //if (taskInfo.creationLock)
            //    EntityManager.creationSemaphore.Release();

            //await Task.Delay(waitSeconds.ToMilliseconds());
            //taskInfo.creationLock = await EntityManager.creationSemaphore.WaitAsync(-1, taskInfo.cancelTokenSource.Token);
        }

        public static async Task Delay(float waitSeconds, TaskInfo taskInfo)
        {
            if (taskInfo == createLockTask)
            {
                EntityManager.creationSemaphore.Release();
                createLockTask = null;
            }

            await Task.Delay(waitSeconds.ToMilliseconds());
        }
    }

    public class TaskInfo
    {
        public string taskName { internal get; set; }
        public Task task { internal get; set; }
        public CancellationTokenSource cancelTokenSource { internal get; set; }
    }
}

