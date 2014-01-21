using System;
using System.Threading;

namespace TKCode123
{
    public static class Utils
    {
        public static bool WaitSeconds(this TimeSpan ts, int secs)
        {
            if (secs <= 0 || ts >= new TimeSpan(TimeSpan.TicksPerSecond*secs))
                return false;
            Thread.Sleep(50);
            return true;
        }

        public static string ToRFCString(this DateTime ts)
        {
            return ts.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }
    }
}
