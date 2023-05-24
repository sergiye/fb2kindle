using System;
using System.Threading;

namespace jail.Classes {
  public class QueueProcessor<T> {
    private readonly IBaseQueue<T> tasks;
    private readonly EventWaitHandle wh = new AutoResetEvent(false);
    private Thread worker;
    private bool started;
    private DateTime lastTaskTime;

    public event Action<T> OnExecuteTask;
    public event Action<T[]> OnExecuteTasks;
    public event Action<T> OnTaskFinished;

    public readonly string Name;
    public int ExecuteStep { get; set; }
    public bool ExecuteStepIsMax { get; set; }
    public int ExecuteStepTimeout { get; set; } //in seconds: timeout between 2 tasks

    public QueueProcessor(IBaseQueue<T> queue, string name, int executeStep = 1, int executeStepTimeout = 1,
      bool executeStepIsMax = true) {
      tasks = queue ?? throw new ArgumentException("queue must be specified");
      Name = name;
      if (executeStep < 1)
        throw new ArgumentException("ExecuteStep should be greater than 1");
      ExecuteStep = executeStep;
      if (executeStepTimeout < 1)
        throw new ArgumentException("ExecuteStepTimeout should be greater than 1");

      ExecuteStepTimeout = executeStepTimeout; //Timeout.Infinite;
      ExecuteStepIsMax = executeStepIsMax;
      //Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(Environment.ProcessorCount > 2 ? (int)Math.Pow(2, (double)Environment.ProcessorCount / 4) - 1 : 1);
    }

    public bool Started {
      get { return started; }
    }

    public void Start() {
      if (Started)
        return;
      started = true;
      worker = new Thread(Work) {
        IsBackground = true,
        Name = string.Format("{0}_worker", Name),
        Priority = ThreadPriority.BelowNormal
      };
      worker.Start();
    }

    public void Stop() {
      if (!Started)
        return;
      if (worker != null) {
        worker.Abort();
        worker = null;
      }

      wh.Close();
      started = false;
    }

    public int Count {
      get { return tasks.Count; }
    }

    public virtual void EnqueueTask(T task) {
      if (!Started) return;
      tasks.Enqueue(task);
      lastTaskTime = DateTime.Now;
      wh.Set();
    }

    protected virtual T DequeueTask() {
      return tasks.Dequeue();
    }

    private void Work() {
      try {
        while (Started) {
          var tasksCount = Count;
          if (tasksCount >= ExecuteStep ||
              tasksCount > 0 && lastTaskTime.AddSeconds(ExecuteStepTimeout) < DateTime.Now) {
            var tp = ExecuteStepIsMax ? (tasksCount > ExecuteStep ? ExecuteStep : tasksCount) : tasksCount;
            var tasks = new T[tp];
            for (var i = 0; i < tp; i++)
              tasks[i] = DequeueTask();
            OnExecuteTasks?.Invoke(tasks);
            foreach (var task in tasks) {
              if (!Started) return;
              OnExecuteTask?.Invoke(task);
              OnTaskFinished?.Invoke(task);
            }
          }

          if (Count == 0)
            wh.WaitOne(ExecuteStepTimeout * 1000);
        }
      }
      catch (ThreadAbortException ex) {
        Console.WriteLine("{0} aborted: {1}", Name, ex.Message);
      }
      catch (Exception e) {
        Console.WriteLine("{0} stopped: {1}", Name, e.Message);
      }
    }
  }
}