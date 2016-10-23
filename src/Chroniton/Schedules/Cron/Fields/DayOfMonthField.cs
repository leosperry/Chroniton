using System;
using System.Collections.Generic;

namespace Chroniton.Schedules.Cron.Fields
{
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
				new string[] { "SAT", "6" },
				new string[] { "#5", "L" }
		};

		protected override DatePart DatePart
		{
			get
			{
				return DatePart.Day;
			}
		}

		internal override int SmallestValueForPart => 1;

		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddDays(value - date.Day);
		}

		public DayOfMonthField(string dayOfWeek, string dayOfMonth)
		{
			_dayOfWeek = dayOfWeek.Replace('?', '*').ToUpper();
			foreach (var item in conversions)
			{
				_dayOfWeek = _dayOfWeek.Replace(item[0], item[1]);
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
			else if (_dayOfMonth == "L")
			{
				return getLastDayOfMonth(date);
			}
			else if (_dayOfMonth.EndsWith("W"))
			{
				var d = int.Parse(_dayOfMonth.Substring(0, _dayOfMonth.Length - 1));
				return getNearestWeekday(d, date);
			}

			IEnumerable<int> availableValues;
			if (_dayOfMonth != "*")
			{
				availableValues = parseCommaHyphenedInts(_dayOfMonth);
				availableValues = cleanLastDaysOfMonth(date, availableValues);
			}
			else
			{
				availableValues = getAvailableFromDayOfWeek(date);
			}

			var newday = getNearestInt(date.Day, availableValues);
			return SetTimePart(date, newday);
		}

		public override DateTime GetNext(DateTime input)
		{
			if (_dayOfMonth == "*" && _dayOfWeek == "*")
			{
				var lastDay = getLastDayOfMonth(input);
				if (lastDay.Day == input.Day)
				{
					return SetTimePart(input, SmallestValueForPart);
				}
				else
				{
					return IncrementTime(input);
				}
			}
			else if (_dayOfMonth == "L")
			{
				return getLastDayOfMonth(input);
			}
			else if (_dayOfMonth.EndsWith("W"))
			{
				var d = int.Parse(_dayOfMonth.Substring(0, _dayOfMonth.Length - 1));
				return getNearestWeekday(d, input);
			}

			IEnumerable<int> availableValues;
			if (_dayOfMonth != "*")
			{
				availableValues = parseCommaHyphenedInts(_dayOfMonth);
				availableValues = cleanLastDaysOfMonth(input, availableValues);
			}
			else
			{
				availableValues = getAvailableFromDayOfWeek(input);
			}

			var newday = getNextInt(input.Day, availableValues);
			if (newday == null)
			{
				return SetTimePart(input, SmallestValueForPart);
			}
			else
			{
				return SetTimePart(input, newday.Value);
			}
		}

		private IEnumerable<int> cleanLastDaysOfMonth(DateTime input, IEnumerable<int> availableValues)
		{
			bool lastDayReturned = false;
			var lastDay = getLastDayOfMonth(input).Day;
			foreach (var item in availableValues)
			{
				if (item < lastDay)
				{
					yield return item;
				}
				else
				{
					if (!lastDayReturned)
					{
						lastDayReturned = true;
						yield return lastDay;
					}
				}
			}
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.Date == getLastDayOfMonth(date) ? date : date.AddDays(1);
		}

		private DateTime getNearestWeekday(int day, DateTime date)
		{
			var newdate = SetTimePart(date, day);
			if (newdate.DayOfWeek < System.DayOfWeek.Saturday && newdate.DayOfWeek > System.DayOfWeek.Sunday)
			{
				return newdate;
			}
			else if (newdate.Day == 1 && newdate.DayOfWeek == System.DayOfWeek.Saturday)
			{
				//must grab next Monday
				return newdate.AddDays(2);
			}
			else if (newdate.Day == getLastDayOfMonth(date).Day && newdate.DayOfWeek == System.DayOfWeek.Sunday)
			{
				//must grab previous Friday
				return newdate.AddDays(-2);
			}
			else if (newdate.DayOfWeek == System.DayOfWeek.Saturday)
			{
				return newdate.AddDays(-1);
			}
			else
			{
				return newdate.AddDays(1);
			}
		}

		private static DateTime getLastDayOfMonth(DateTime date)
		{
			var retVal = date.AddMonths(1);
			return retVal.AddDays(-retVal.Day);
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
				else if (item.Contains("#"))
				{
					var values = item.Split('#');
					int dayOfWeek = int.Parse(values[0]), number = int.Parse(values[1]);
					yield return getNDayOfWeekFromMonth((DayOfWeek)dayOfWeek, number, date);
				}
				else if (item.Contains("-"))
				{
					var range = item.Split('-');
					int start = int.Parse(range[0]), end = int.Parse(range[1]);
					for (int i = start; i <= end; i++)
					{
						foreach (var day in getDaysFromWeekDay((DayOfWeek)i, date))
						{
							yield return day;
						}
					}
				}
				else
				{
					foreach (var day in getDaysFromWeekDay((DayOfWeek)int.Parse(item), date))
					{
						yield return day;
					}
				}
			}
		}

		private IEnumerable<int> getDaysFromWeekDay(DayOfWeek dayOfWeek, DateTime date)
		{
			int first = getNDayOfWeekFromMonth(dayOfWeek, 1, date);
			var trackingDate = date.AddDays(first - date.Day);
			do
			{
				yield return trackingDate.Day;
				trackingDate = trackingDate.AddDays(7);
			} while (trackingDate.Month == date.Month);
		}

		private int getNDayOfWeekFromMonth(DayOfWeek dayOfWeek, int number, DateTime date)
		{
			var currentDate = date.AddDays(-date.Day + 1);
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
				lastDay = lastDay.AddDays(-1);
			}
			return lastDay.Day;
		}
	}
}
