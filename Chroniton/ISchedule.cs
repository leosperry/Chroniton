using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chroniton
{
    public delegate void Work();

    public interface ISchedule
    {
        string Name { get; set; }
        DateTime NextScheduledTime(DateTime afterThisTime);
    }

    public interface IRecordedSchedule
    {
        DateTime LastSuccessfullRun(DateTime beforeThisTime);
        IEnumerable<DateTime> AllSuccessfullRuns();
        IEnumerable<DateTime> LastSuccessfullRuns(int count);
        void RecordSuccessfullRun(DateTime successTime);
    }

}
