using System;

namespace Chroniton.Schedules.Cron.Fields
{
	class YearField : SimpleField
	{
		protected override DatePart DatePart
		{
			get
			{
				return DatePart.Year;
			}
		}

		internal override int SmallestValueForPart => 1970;

		public YearField(string field) : base(field)
		{

		}

		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddYears(value - date.Year);
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.AddYears(1);
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

		internal override int SmallestValueForPart => 1;

		public MonthField(string field) : base(convertMonths(field))
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
		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddMonths(value - date.Month);
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.Month == 12 ? date : date.AddMonths(1);
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

		internal override int SmallestValueForPart => 0;

		public HoursField(string field) : base(field, 24)
		{

		}

		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddHours(value - date.Hour);
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.Hour == 23 ? date : date.AddHours(1);
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

		internal override int SmallestValueForPart => 0;

		public MinutesField(string field) : base(field, 60)
		{

		}

		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddMinutes(value - date.Minute);
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.Minute == 59 ? date : date.AddMinutes(1);
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

		internal override int SmallestValueForPart => 0;

		public SecondsField(string field) : base(field, 60)
		{

		}

		internal override DateTime SetTimePart(DateTime date, int value)
		{
			return date.AddSeconds(value - date.Second);
		}

		protected override DateTime IncrementTime(DateTime date)
		{
			return date.Second == 59 ? date : date.AddSeconds(1);
		}

	}
}
