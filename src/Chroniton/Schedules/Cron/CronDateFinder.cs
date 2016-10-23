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
		DateField[] _fields;

		public CronDateFinder(string seconds, string minutes, string hours, string dayOfMonth,
			string month, string dayOfWeek, string year)
		{
			_fields = new DateField[] {
				new SecondsField(seconds),
				new MinutesField(minutes),
				new HoursField(hours),
				new DayOfMonthField(dayOfWeek, dayOfMonth),
				new MonthField(month),
				new YearField(year)
			};
		}

		public DateTime? GetNext(DateTime input)
		{
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
					retVal = _fields[currentColumn].GetNearestToCurrent(retVal);
				}
				else
				{
					retVal = _fields[currentColumn].GetNext(retVal);
					//also set remaining to minimum;
					for (int i = currentColumn - 1; i >= 0; i--)
					{
						var field = _fields[i];
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
