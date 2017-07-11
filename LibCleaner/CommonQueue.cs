using System;
using System.Collections.Generic;
using System.Threading;

namespace LibCleaner
{
    public class CommonQueue<T>
    {
        private readonly object _locker = new object();
        private readonly Queue<T> _tasks = new Queue<T>();
        private readonly EventWaitHandle _wh = new AutoResetEvent(false);
        private readonly Thread _worker;
        private bool _started;
        private DateTime _lastTaskTime;

        public event Action<T> OnExecuteTask;
        public event Action<T> OnTaskFinished;
        public event Action<T[]> OnExecuteTasks;

        public string Name { get; set; }
        public int ExecuteStep { get; set; }
        public bool UseExecuteStepAsMinValue { get; set; }
        public int StepTimeout { get; set; } //in seconds: timeout between 2 tasks

        public CommonQueue()
        {
            ExecuteStep = 1;
            UseExecuteStepAsMinValue = true;
            StepTimeout = 60;
            //Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(Environment.ProcessorCount > 2 ? (int)Math.Pow(2, (double)Environment.ProcessorCount / 4) - 1 : 1);
            _started = true;
            _worker = new Thread(Work) { IsBackground = true };
            _worker.Start();
        }

        public void Stop()
        {
            _started = false;
            _worker.Join();
            _wh.Close();
        }

        public void EnqueueTask(T task)
        {
            if (!_started) return;
            lock (_locker)
                _tasks.Enqueue(task);
            _lastTaskTime = DateTime.Now;
            _wh.Set();
        }

        private void Work()
        {
            while (_started)
            {
                var tasksCount = _tasks.Count;
                if (tasksCount >= ExecuteStep ||
                    tasksCount > 0 && _lastTaskTime.AddSeconds(StepTimeout) < DateTime.Now)
                {
                    var tp = UseExecuteStepAsMinValue ? tasksCount : (tasksCount > ExecuteStep ? ExecuteStep : tasksCount);
                    var tasks = new T[tp];
                    lock (_locker)
                        for (var i = 0; i < tp; i++)
                            tasks[i] = _tasks.Dequeue();
                    if (OnExecuteTasks != null)
                        OnExecuteTasks(tasks);
                    foreach (var task in tasks)
                    {
                        if (OnExecuteTask != null)
                            OnExecuteTask(task);
                        if (OnTaskFinished != null)
                            OnTaskFinished(task);
                    }
                }
                if (_tasks.Count == 0)
                    _wh.WaitOne(StepTimeout * 1000);
            }
        }
    }
}