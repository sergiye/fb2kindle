using System;
using Simpl.Extensions;

namespace jail.Classes {
  
  internal static class BackgroundTasks {
    
    private static readonly CommonQueue<Action> actionsQueue;

    static BackgroundTasks() {
      actionsQueue = new CommonQueue<Action>("BackgroundActionsQueue");
      actionsQueue.OnExecuteTask += action => {
        try {
          action.Invoke();
        }
        catch (Exception e) {
          Console.WriteLine(e);
        }
      }; 
      actionsQueue.Start();
    }
    
    public static void EnqueueAction(Action executeMethod) {
      actionsQueue?.EnqueueTask(executeMethod);
    }
  }
}