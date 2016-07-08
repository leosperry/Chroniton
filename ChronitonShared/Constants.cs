using System;

namespace Chroniton
{
    public class Constants
    {
        /// <summary>
        /// When a schedule returns this value, the job will effectively be expired and will not run again
        /// </summary>
        public static readonly DateTime Never = new DateTime(1919, 5, 29, 14, 30, 0);

        // easter egg: someone should find a more exact time to the above historical date
        // I believe it's currently accurate +/- 20 minutes
    }
}