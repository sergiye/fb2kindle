namespace jail.Classes {
  public interface IBaseQueue<T> {
    int Count { get; }
    T Dequeue();
    void Enqueue(T msg);
  }
}