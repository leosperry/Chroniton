using System;
using System.Collections.Generic;
using System.Text;

namespace Chroniton.Schedules.Cron
{
    public class CronParsingException : Exception
    {
        public CronParsingException(string message): base(message)
        {

        }
    }
}
