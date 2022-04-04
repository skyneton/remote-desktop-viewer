using System.Collections.Concurrent;
using System.Threading;

namespace RemoteDesktopViewer.Utils
{
    public class ThreadFactory
    {
        private static readonly ConcurrentQueue<ThreadFactory> ThreadFactories = new ConcurrentQueue<ThreadFactory>();
        private readonly ConcurrentBag<Thread> _threads = new ConcurrentBag<Thread>();

        public ThreadFactory()
        {
            ThreadFactories.Enqueue(this);
        }

        public Thread LaunchThread(Thread thread, bool setName = true)
        {
            thread.Start();
            
            if (_threads == null) return thread;
            
            if(setName)
                thread.Name = "Thread-" + (_threads.Count - 1);
            
            _threads.Add(thread);

            return thread;
        }

        public void KillAll()
        {
            if (_threads == null) return;
            
            foreach (var thread in _threads)
            {
                if(thread.IsAlive)
                    thread.Interrupt();
            }
        }

        public static void Close()
        {
            while (!ThreadFactories.IsEmpty)
            {
                if(!ThreadFactories.TryDequeue(out var factory)) continue;
                factory.KillAll();
            }
        }
    }
}