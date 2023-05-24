namespace jail.Classes {
  public class CommonQueue<T> : QueueProcessor<T> {
    public CommonQueue(string name, int executeStep = 1, int executeStepTimeout = 1, bool executeStepIsMax = true)
      : base(new SafeQueue<T>(), name, executeStep, executeStepTimeout, executeStepIsMax) {
    }
  }
}