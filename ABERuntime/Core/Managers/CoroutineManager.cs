using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
    public static class CoroutineManager
    {
        static List<TaskInfo> taskInfos = new List<TaskInfo>();

        public static void StartCoroutine(string taskName, Func<CancellationToken, Task> taskFunc)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var taskInfo = new TaskInfo()
            {
                taskName = taskName,
                cancelTokenSource = cts
            };

            var task = taskFunc(cts.Token).ContinueWith((t) =>
            {
                if (taskInfos.Contains(taskInfo))
                    taskInfos.Remove(taskInfo);
            });

            taskInfo.task = task;

            taskInfos.Add(taskInfo);
        }


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
        }
    }

    class TaskInfo
    {
        public string taskName { get; set; }
        public Task task { get; set; }
        public CancellationTokenSource cancelTokenSource { get; set; }
    }
}

