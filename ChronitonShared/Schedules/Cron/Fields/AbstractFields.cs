using System;
using System.Collections.Generic;
using System.Linq;

namespace Chroniton.Schedules.Cron.Fields
{
    /// <summary>
    /// internal class which implements the needs of the CronDateFinder
    /// and some utility methods for child classes
    /// </summary>
    abstract class DateField
    {
        protected abstract DatePart DatePart { get; }

        internal abstract int SmallestValueForPart { get; }

        public abstract DateTime GetNext(DateTime input);

        public abstract DateTime GetNearestToCurrent(DateTime date);

        internal abstract DateTime SetTimePart(DateTime date, int value);

        protected static IEnumerable<int> parseCommaHyphenedInts(string input)
        {
            foreach (var item in input.Split(','))
            {
                if (item.Contains("-"))
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
            int nextBiggest = int.MaxValue;
            foreach (int i in ints)
            {
                if (i == target)
                {
                    return i;
                }
                else if (i < nextBiggest && i > target)
                {
                    nextBiggest = i;
                }
            }

            if (nextBiggest != int.MaxValue)
            {
                return nextBiggest;
            }
            else
            {
                return ints.Aggregate((i1, i2) => i1 < i2 ? i1 : i2);
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

        protected abstract DateTime IncrementTime(DateTime date);

        protected int getPartFromDate(DateTime date)
        {
            switch (this.DatePart)
            {
                default:
                case DatePart.Year:
                    return date.Year;
                case DatePart.Month:
                    return date.Month;
                case DatePart.Day:
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
                var newValue = getNearestInt(partValue, availableValues);
                return SetTimePart(date, newValue);
            }
        }

        public override DateTime GetNext(DateTime input)
        {
            if (availableValues == null)
            {
                return IncrementTime(input);
            }
            else
            {
                var partValue = getPartFromDate(input);
                var next = base.getNextInt(partValue, availableValues);
                if (next.HasValue)
                {
                    return SetTimePart(input, next.Value);
                }
                else
                {
                    return SetTimePart(input, SmallestValueForPart);
                }
            }
        }
    }


    /// <summary>
    /// adds functionality for simple fields which 
    /// have allow the slash feature
    /// </summary>
    abstract class SimpleFieldWithSlash : SimpleField
    {
        public SimpleFieldWithSlash(string field, int total) : base(convertSlashToCSV(field, total))
        {

        }

        static string convertSlashToCSV(string field, int total)
        {
            // this method is not the cleanest
            // however, the / is a shortcut for CSV
            // so, it is accurate
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
            else 
            {
                return field;
            }
        }
    }
}
