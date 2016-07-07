using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChronitonShared.Schedules
{
    public class CronSchedule
    {
        string _cron;

        static readonly Regex _minutes = new Regex(@"^((([0-5]?[0-9](\-[0-5]?[0-9])?)(,[0-5]?[0-9](\-[0-5]?[0-9])?)*)|\*)$");
        static readonly Regex _hours = new Regex(@"^(((([01]?[0-9]|2[0-3])(\-[01]?[0-9]|2[0-3])?)(,([01]?[0-9]|2[0-3])(\-[01]?[0-9]|2[0-3])?)*)|\*)$");
        static readonly Regex _date = new Regex(@"^((((0?[1-9]|[12][0-9]|3[01])(W$)?(\-(0?[1-9]|[12][0-9]|3[01]))?)(,((0?[1-9]|[12][0-9]|3[01])(\-(0?[1-9]|[12][0-9]|3[01]))?))*)|\*|\?|L)$");
        static readonly Regex _month = new Regex(@"^((((JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|[12]?[0-9])(\-((JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|[12]?[0-9])))?)(,((JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|[12]?[0-9])(\-((JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|[12]?[0-9])))?))*)|\*)$");
        static readonly Regex _dayOfWeek = new Regex(@"^((((SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6])((L|#[1-5])$)?(\-(SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6]))?)(,((SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6])((L|#[1-5])$)?(\-(SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6]))?))*)|\*|\?)$");
        static readonly Regex _year = new Regex(@"");

        public CronSchedule(string cron)
        {
            Cron cronObj;
            if (!validateCron(cron, out cronObj))
            {
                throw new ArgumentException("cron string is not valid", nameof(cron));
            }
            _cron = cron;
        }

        private bool validateCron(string cron, out Cron cronOjb)
        {
            cronOjb = null;
            var parts = cron.Split(' ');
            if (parts.Length < 6 || parts.Length > 7)
            {
                return false;
            }
            var success = 
                _minutes.IsMatch(parts[0]) &&
                _hours.IsMatch(parts[0]);

            if (success)
            {
                cronOjb = new Cron()
                {
                    Minute = parts[0],
                    Hour = parts[1]
                };
            }
            
            return success;
        }

        class Cron
        {
            public string Minute { get; set; }
            public string Hour { get; set; }
            public string Date { get; set; }
            public string Month { get; set; }
            public string DayOfWeek { get; set; }
            public string Year { get; set; }

        }
    }
}
