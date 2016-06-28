using System;
using System.Collections.Generic;

namespace Chroniton
{

    public interface ISchedule
    {
        /// <summary>
        /// A name for the schedule
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Is called with the previous execution time to determine the next
        /// </summary>
        /// <param name="afterThisTime">the time to be used to calculate th next time</param>
        /// <returns></returns>
        DateTime NextScheduledTime(DateTime afterThisTime);
    }

    /// <summary>
    /// no implementation yet
    /// </summary>
    public interface IRecordedSchedule
    {
        DateTime LastSuccessfullRun(DateTime beforeThisTime);
        IEnumerable<DateTime> AllSuccessfullRuns();
        IEnumerable<DateTime> LastSuccessfullRuns(int count);
        void RecordSuccessfullRun(DateTime successTime);
    }

}
