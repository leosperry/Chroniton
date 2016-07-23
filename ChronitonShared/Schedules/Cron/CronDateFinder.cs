using Chroniton.Schedules.Cron.Fields;
using System;

namespace Chroniton.Schedules.Cron
{
    enum DatePart
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    public class CronDateFinder
    {
        public string Seconds { get; set; }
        public string Minutes { get; set; }
        public string Hours { get; set; }
        public string DayOfMonth { get; set; }
        public string Month { get; set; }
        public string DayOfWeek { get; set; }
        public string Year { get; set; }

        public DateTime? GetNext(DateTime input)
        {
            var fields = new DateField[] {
                new SecondsField(Seconds),
                new MinutesField(Minutes),
                new HoursField(Hours),
                new DayOfMonthField(DayOfWeek, DayOfMonth),
                new MonthField(Month),
                new YearField(Year)
            };

            int currentColumn = 5;
            DateTime retVal = new DateTime(
                input.Year, input.Month, input.Day, 
                input.Hour, input.Minute, input.Second + 1);

            while (currentColumn >= 0)
            {
                if (currentColumn > 5)
                {
                    return null;
                }
                if (retVal > input)
                {
                    retVal = fields[currentColumn].GetNearestToCurrent(retVal);
                }
                else
                {
                    retVal = fields[currentColumn].GetNext(retVal);
                    //also set remaining to minimum;
                    for (int i = currentColumn - 1; i >= 0; i--)
                    {
                        var field = fields[i];
                        retVal = field.SetTimePart(retVal, field.SmallestValueForPart);
                    }
                }

                if (retVal > input)
                {
                    currentColumn--;
                }
                else
                {
                    currentColumn++;
                }
            }            
            return retVal;
        }
    }
}
