using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RemoteDesktopViewer.Utils
{

    public static class RemoveExtensions
    {
        public static void Remove<T>(this ConcurrentBag<T> data, T target)
        {
            var removeQueue = new Queue<T>();
            while (!data.IsEmpty)
            {
                if (data.TryTake(out var item) && item.Equals(target))
                    break;

                removeQueue.Enqueue(item);
            }

            foreach (var item in removeQueue)
            {
                data.Add(item);
            }
        }

        public static void Remove<T>(this Queue<T> data, T target)
        {
            var removeQueue = new Queue<T>();
            while (data.Count > 0)
            {
                var item = data.Dequeue();
                if (item.Equals(target))
                    break;

                removeQueue.Enqueue(item);
            }

            foreach (var item in removeQueue)
            {
                data.Enqueue(item);
            }
        }
    }
}