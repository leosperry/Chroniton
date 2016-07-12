using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chroniton.Schedules.Cron
{
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
            //field order
            // 5 Year
            // 4 Month 
            // 3 Date (calculated from 2 fields)
            // 2 Hour
            // 1 Minute
            // 0 Second

            var fields = new DateField[] {
                new YearField(this.Year),
                new MonthField(this.Month),
                new DayOfMonthField(this.DayOfWeek, this.DayOfMonth),
                new HoursField(this.Hours),
                new MinutesField(this.Minutes),
                new SecondsField(this.Seconds)
            };

            //Foreach column
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

        enum DatePart
        {
            Year,
            Month,
            Date,
            Hour,
            Minute,
            Second
        }

        /// <summary>
        /// internal class which implements the needs of the CronDateFinder
        /// and some utility methods for child classes
        /// </summary>
        abstract class DateField
        {
            protected abstract DatePart DatePart { get; }

            public abstract DateTime GetNext(DateTime input);
            public abstract DateTime GetNearestToCurrent(DateTime date);
            
            protected static IEnumerable<int> parseCommaHyphenedInts(string input)
            {
                foreach (var item in input.Split(','))
                {
                    if (item.Contains('-'))
                    {
                        var range = item.Split('-');
                        var start = int.Parse(range[0]);
                        var end = int.Parse(range[1]);
                        if (start > end)
                        {
                            throw new Exception();
                        }
                        for (int i = start; i <= end; i++)
                        {
                            yield return i;
                        }
                    }
                    else
                    {
                        yield return int.Parse(item);
                    }
                }
            }

            protected int getNearestInt(int target, IEnumerable<int> ints)
            {
                int nextBiggest = int.MaxValue, nextSmallest = int.MinValue;
                foreach (int i in ints)
                {
                    if (i == target)
                    {
                        return i;
                    }
                    else
                    {
                        if (i > nextSmallest && i < target)
                        {
                            nextSmallest = i;
                        }
                        else if (i < nextBiggest && i > target)
                        {
                            nextBiggest = i;
                        }
                    }
                }
                if (nextBiggest != int.MaxValue)
                {
                    return nextBiggest;
                }
                else
                {
                    return nextSmallest;
                }
            }

            protected int? getNextInt(int target, IEnumerable<int> ints)
            {
                int? retVal = null;
                foreach (var i in ints)
                {
                    if (i > target && (retVal == null || i < retVal))
                    {
                        retVal = i;
                    }
                }
                return retVal;
            }

            protected DateTime addTime(DateTime date, int amount)
            {
                switch (this.DatePart)
                {
                    case DatePart.Year:
                        return date.AddYears(amount);
                    case DatePart.Month:
                        return date.AddMonths(amount);
                    case DatePart.Date:
                        return date.AddDays(amount);
                    case DatePart.Hour:
                        return date.AddHours(amount);
                    case DatePart.Minute:
                        return date.AddMinutes(amount);
                    default:
                    case DatePart.Second:
                        return date.AddSeconds(amount);
                }
            }

            protected int getPartFromDate(DateTime date)
            {
                switch (this.DatePart)
                {
                    default:
                    case DatePart.Year:
                        return date.Year;
                    case DatePart.Month:
                        return date.Month;
                    case DatePart.Date:
                        return date.Day;
                    case DatePart.Hour:
                        return date.Hour;
                    case DatePart.Minute:
                        return date.Minute;
                    case DatePart.Second:
                        return date.Second;
                }
            }
        }

        /// <summary>
        /// class which hadles most of the needs
        /// of the CronDateFinder
        /// </summary>
        abstract class SimpleField : DateField
        {
            IEnumerable<int> availableValues = null;

            public SimpleField(string field)
            {
                if (field != "*")
                {
                    availableValues = parseCommaHyphenedInts(field);
                }
            }

            public override DateTime GetNearestToCurrent(DateTime date)
            {
                if (availableValues == null)
                {
                    return date;
                }
                else
                {
                    var partValue = getPartFromDate(date);
                    var year = getNearestInt(partValue, availableValues);
                    return addTime(date, year - partValue);
                }
            }

            public override DateTime GetNext(DateTime input)
            {
                if (availableValues == null)
                {
                    return input.AddYears(1);
                }
                else
                {
                    var next = base.getNextInt(input.Year, availableValues);
                    if (next.HasValue)
                    {
                        var partValue = base.getPartFromDate(input);
                        return base.addTime(input, next.Value - partValue);
                    }
                    else
                    {
                        return input;
                    }
                }
            }
        }

        class YearField: SimpleField
        {
            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Year;
                }
            }

            public YearField(string field): base(field)
            {

            }
        }

        /// <summary>
        /// adds functionality for simple fields which 
        /// have allow the slash feature
        /// </summary>
        abstract class SimpleFieldWithSlash : SimpleField
        {
            public SimpleFieldWithSlash(string field, int total): base(convertSlashToCSV(field, total))
            {

            }

            static string convertSlashToCSV(string field, int total)
            {
                // this method is not the cleanest
                // however, the / is a shortcut for CSV
                // so, it is accurate
                // feel free to refactor
                if (field.Contains("/"))
                {
                    var divisionAmountStr = field.Substring(field.IndexOf('/') + 1);
                    var multiplier = int.Parse(divisionAmountStr);
                    if (total % multiplier != 0)
                    {
                        // by this point, after the regex, it had better be divisible
                        // we should ay be able to delete this.
                        // thourough unit tests are needed
                        throw new CronParsingException("slash parameter is invalid");
                    }
                    
                    return
                        (from i in Enumerable.Range(0, total / multiplier) 
                         select (i * multiplier).ToString())
                        .Aggregate((s1, s2) => $"{s1},{s2}");
                }
                else // it should only be an *
                {
                    return field;
                }
            }
        }

        class HoursField : SimpleFieldWithSlash
        {
            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Hour;
                }
            }

            public HoursField(string field) : base(field, 24)
            {

            }
        }

        class MinutesField : SimpleFieldWithSlash
        {
            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Minute;
                }
            }

            public MinutesField(string field) : base(field, 60)
            {

            }
        }

        class SecondsField : SimpleFieldWithSlash
        {
            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Second;
                }
            }

            public SecondsField(string field) : base(field, 60)
            {

            }
        }

        /// <summary>
        /// allows the text features of the month field
        /// to work with the simple field
        /// </summary>
        class MonthField : SimpleField
        {
            static readonly string[][] conversions = new string[][]
            {
                new string[] { "JAN", "1" },
                new string[] { "FEB", "2" },
                new string[] { "MAR", "3" },
                new string[] { "APR", "4" },
                new string[] { "MAY", "5" },
                new string[] { "JUN", "6" },
                new string[] { "JUL", "7" },
                new string[] { "AUG", "8" },
                new string[] { "SEP", "9" },
                new string[] { "OCT", "10" },
                new string[] { "NOV", "11" },
                new string[] { "DEC", "12" },
            };

            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Month;
                }
            }

            public MonthField(string field): base(convertMonths(field))
            {
                
            }

            static string convertMonths(string field)
            {
                foreach (var item in conversions)
                {
                    field = field.Replace(item[0], item[1]);
                }
                return field;
            }
        }

        /// <summary>
        /// the most complex field, it needs to take into account 
        /// both the Day Of Week and Day Of Month Field columns
        /// which have the #, L, and W characters
        /// it also needs to take into account the number of days in a month
        /// </summary>
        class DayOfMonthField : DateField 
        {
            string _dayOfWeek, _dayOfMonth;
            static readonly string[][] conversions = new string[][]
            {
                new string[] { "SUN", "0" },
                new string[] { "MON", "1" },
                new string[] { "TUE", "2" },
                new string[] { "WED", "3" },
                new string[] { "THU", "4" },
                new string[] { "THUR", "4" },
                new string[] { "FRI", "5" },
                new string[] { "SAT", "6" }
            };

            protected override DatePart DatePart
            {
                get
                {
                    return DatePart.Date;
                }
            }

            public DayOfMonthField(string dayOfWeek, string dayOfMonth)
            {
                _dayOfWeek = dayOfWeek.Replace('?', '*');
                foreach (var item in conversions)
                {
                    _dayOfWeek.Replace(item[0], item[1]);
                }
                _dayOfMonth = dayOfMonth.Replace('?', '*');
                if (_dayOfWeek != "*" && _dayOfMonth != "*")
                {
                    throw new CronParsingException("setting day of month and day of week not supported");
                }
            }

            public override DateTime GetNearestToCurrent(DateTime date)
            {
                var retval = date;
                if (_dayOfMonth == "*" && _dayOfWeek == "*")
                {
                    return date;
                }


                return retval;
            }


            public override DateTime GetNext(DateTime input)
            {
                if (_dayOfMonth == "*" && _dayOfWeek == "*")
                {
                    return input.AddDays(1);
                }

                throw new NotImplementedException();
            }

            IEnumerable<int> getAvailable(DateTime date)
            {
                if (_dayOfMonth == "L")
                {
                    return Enumerable.Repeat(getLastDayOfMonth(date).Day, 1);
                }
                else if (_dayOfMonth != "*")
                {
                    return getAvailableFromDayOfMonth(date);
                }
                else
                {
                    return getAvailableFromDayOfWeek(date);
                }

            }

            private static DateTime getLastDayOfMonth(DateTime date)
            {
                return date.AddMonths(1).AddDays(-date.Day - 1);
            }

            private IEnumerable<int> getAvailableFromDayOfWeek(DateTime date)
            {
                foreach (var item in _dayOfWeek.Split(','))
                {
                    if (item.EndsWith("L"))
                    {
                        var day = (DayOfWeek)int.Parse(item.Substring(0, 1));
                        yield return getLastDayOfWeekOfMonth(day, date);
                    }
                    else if (item.Contains('#'))
                    {
                        var values = item.Split('#');
                        int dayOfWeek = int.Parse(values[0]), number = int.Parse(values[1]);
                        yield return getNDayOfWeekFromMonth((DayOfWeek)dayOfWeek, number, date);
                    }
                    else if (item.Contains('-'))
                    {
                        var range = item.Split('-');
                        int start = int.Parse(range[0]), end = int.Parse(range[1]);
                        for (int i = start; i <= end; i++)
                        {
                            yield return i;
                        }
                    }
                    else
                    {
                        yield return int.Parse(item);
                    }
                }
            }

            private int getNDayOfWeekFromMonth(DayOfWeek dayOfWeek, int number, DateTime date)
            {
                var currentDate = date.AddDays(- date.Day + 1);
                while (currentDate.DayOfWeek != dayOfWeek)
                {
                    currentDate = currentDate.AddDays(1);
                }
                return 7 * (number - 1) + currentDate.Day;
            }

            private int getLastDayOfWeekOfMonth(DayOfWeek day, DateTime date)
            {
                var lastDay = getLastDayOfMonth(date);
                while (lastDay.DayOfWeek != day)
                {
                    date = date.AddDays(-1);
                }
                return date.Day;
            }

            private IEnumerable<int> getAvailableFromDayOfMonth(DateTime date)
            {
                throw new NotImplementedException();
            }
        }
    }
}
