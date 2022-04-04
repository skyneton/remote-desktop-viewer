using System;

namespace RemoteDesktopViewer.Utils
{
    public class TimeManager
    {
        private static readonly DateTime StartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis => (long) (DateTime.UtcNow - StartTime).TotalMilliseconds;
    }
}