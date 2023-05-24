using System.Collections.Generic;

namespace jail.Classes {
  public class SafeQueue<T> : Queue<T>, IBaseQueue<T> {
    public object SyncRoot { get; set; }

    public SafeQueue() {
      SyncRoot = new object();
    }

    public new void Enqueue(T msg) {
      lock (SyncRoot)
        base.Enqueue(msg);
    }

    public new int Count {
      get {
        int count;
        lock (SyncRoot)
          count = base.Count;
        return count;
      }
    }

    public new T Dequeue() {
      T msg = default(T);
      lock (SyncRoot)
        if (Count > 0)
          msg = base.Dequeue();
      return msg;
    }

    public new T Peek() {
      T msg;
      lock (SyncRoot)
        msg = base.Peek();
      return msg;
    }

    public new void Clear() {
      lock (SyncRoot)
        base.Clear();
    }
  }
}