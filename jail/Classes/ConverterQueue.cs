using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Simpl.Extensions;

namespace jail.Classes {
  
  public static class ConvertQueue {
    
    public class ConvertTask {
           
      public Guid TaskId { get; }
      public string FileName { get; }
      public DateTime CreateTime { get; }
      public DateTime FinishedTime { get; set; }
      public string Result { get; set; }
      public List<string> Output { get; }

      public ConvertTask(string fileName) {
        TaskId = Guid.NewGuid();
        CreateTime = DateTime.Now;
        FileName = fileName;
        Output = new List<string>();
      }
    }

    private static readonly CommonQueue<ConvertTask> converterQueue;
    private static readonly List<ConvertTask> tasksLog = new List<ConvertTask>();

    static ConvertQueue() {
      converterQueue = new CommonQueue<ConvertTask>("Book converter");
      //_converterQueue.OnExecuteTasks += ConverterQueueOnExecuteTasks;
      converterQueue.OnExecuteTask += ConverterQueueOnExecuteTask;
      converterQueue.Start();
    }
    
    public static event Action<ConvertTask> OnConvertTaskFinished;

    public static ConvertTask GetTaskStatus(Guid taskId) {
      return tasksLog.FirstOrDefault(t => t.TaskId == taskId);
    }

    private static void ConverterQueueOnExecuteTask(ConvertTask task) {
      try {
        
        while (tasksLog.Count > 1000) tasksLog.RemoveAt(0); //todo: optimize tasks log
        tasksLog.Add(task);
        
        if (!File.Exists(task.FileName))
          throw new FileNotFoundException("Book source file not found");

        var resultFile = Path.ChangeExtension(task.FileName, ".mobi");
        if (!File.Exists(resultFile)) {
          // var res = StartProcess(SettingsHelper.ConverterPath, $"{task.FileName} -preview", false);
          var res = LaunchProcess(SettingsHelper.ConverterPath, $"{task.FileName} -preview", s => { task.Output.Add(s); });
          if (res == 2) {
            Logger.WriteWarning("Error converting to mobi");
            throw new ArgumentException("Error converting book for kindle");
          }
        }
        task.Result = $"Book converted: {task.FileName}";
        Logger.WriteDebug(task.Result);
      }
      catch (Exception ex) {
        task.Result = $"Error converting book for kindle: {task.FileName}";
        Logger.WriteError(ex, task.Result);
      }

      task.FinishedTime = DateTime.Now;
      OnConvertTaskFinished?.Invoke(task);
    }

    internal static Guid ConvertBookNoWait(string inputFile) {
      var task = new ConvertTask(inputFile);
      converterQueue.EnqueueTask(task);
      return task.TaskId;
    }

    internal static bool ConvertBook(string inputFile) {
      var resultFile = Path.ChangeExtension(inputFile, ".mobi");
      var id = ConvertBookNoWait(inputFile);
      // wait
      var startTime = Environment.TickCount;
      while (true) {
        Thread.Sleep(500);
        if (File.Exists(resultFile))
          return true;
        if (Environment.TickCount > startTime + 120 * 1000)
          return false;
      }
    }

    internal static int LaunchProcess(string fileName, string arguments, Action<string> onOutput = null) {

      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = {
          FileName = fileName,
          Arguments = arguments,
          UseShellExecute = false,
          RedirectStandardError = true,
          RedirectStandardOutput = true
        }
      };

      process.OutputDataReceived += (sender, e) => { onOutput?.Invoke(e.Data); };
      process.ErrorDataReceived += (sender, e) => { onOutput?.Invoke(e.Data); };
      // process.Exited += (sender, e) => { onOutput?.Invoke($"Process exited with code {process.ExitCode}"); };

      process.Start();
      process.BeginErrorReadLine();
      process.BeginOutputReadLine();
      process.WaitForExit();
      return process.ExitCode;
    }

    internal static int StartProcess(string fileName, string args, bool addToConsole) {
      var startInfo = new ProcessStartInfo {
        FileName = fileName,
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = addToConsole,
        CreateNoWindow = addToConsole,
      };
      var process = Process.Start(startInfo);
      if (process == null)
        throw new Exception("Error starting process.");
      
      if (addToConsole)
        while (!process.StandardOutput.EndOfStream)
          Logger.WriteDebug(process.StandardOutput.ReadLine());
      process.WaitForExit();
      return process.ExitCode;
    }
    
  }
}