using System;

namespace Chroniton.Schedules.Cron
{
    public class CronParsingException : Exception
    {
        public CronParsingException(string message): base(message)
        {

        }
    }
}
