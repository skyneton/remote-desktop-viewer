using System.Collections.Concurrent;
using System.Threading;

namespace RemoteDesktopViewer.Utils
{
    public class ThreadFactory
    {
        private static readonly ConcurrentQueue<ThreadFactory> _threadFactories = new ConcurrentQueue<ThreadFactory>();
        private readonly ConcurrentBag<Thread> Threads = new ConcurrentBag<Thread>();

        public ThreadFactory()
        {
            _threadFactories.Enqueue(this);
        }

        public Thread LaunchThread(Thread thread, bool setName = true)
        {
            thread.Start();
            
            if (Threads == null) return thread;
            
            if(setName)
                thread.Name = "Thread-" + (Threads.Count - 1);
            
            Threads.Add(thread);

            return thread;
        }

        public void KillAll()
        {
            if (Threads == null) return;
            
            foreach (var thread in Threads)
            {
                if(thread.IsAlive)
                    thread.Interrupt();
            }
        }

        public static void Close()
        {
            while (!_threadFactories.IsEmpty)
            {
                if(!_threadFactories.TryDequeue(out var factory)) continue;
                factory.KillAll();
            }
        }
    }
}