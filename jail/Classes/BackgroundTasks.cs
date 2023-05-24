using System;
using System.Threading.Tasks;

namespace jail.Classes {
    internal static class BackgroundTasks {
        private static readonly CommonQueue<Task> actionsQueue;

        static BackgroundTasks() {
            actionsQueue = new CommonQueue<Task>("BackgroundActionsQueue");
            actionsQueue.OnExecuteTask += task => {
                try {
                    task.Start();
                    task.Wait();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            };
            actionsQueue.Start();
        }

        public static void EnqueueAction(Task task) {
            actionsQueue?.EnqueueTask(task);
        }
    }
}