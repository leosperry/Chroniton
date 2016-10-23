using System.Linq;
using System.Text.RegularExpressions;

namespace Chroniton.Schedules.Cron
{
	public class CronParser
	{
		const string secondsMinutesPattern = @"([0-5]?[0-9])";
		const string hoursPattern = @"([01]?[0-9]|2[0-3])";
		const string dayOfMonthPattern = @"(0?[1-9]|[12][0-9]|3[01])";
		const string monthPattern = @"(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|(0?[0-9]|1[0-2]))";
		const string dayOfWeekPattern = @"(SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6])";
		const string optDayOfWeek = @"((L|#[1-5])?)";
		const string yearPattern = @"([0-9]{4})";
		const string hyphenCommaPattern = @"((({0}{1}(\-{0})?)(,{0}(\-{0})?)*)|(\*{3}){2})";

		static readonly Regex _reg;

		static CronParser()
		{
			string[][] partsList = new string[][]
			{
				new string[] { secondsMinutesPattern, string.Empty, string.Empty, @"(/([2-6]|1[025]|[23]0))?" },
				new string[] { secondsMinutesPattern, string.Empty, string.Empty ,@"(/([2-6]|1[025]|[23]0))?"},
				new string[] { hoursPattern, string.Empty, string.Empty, @"(/([23468]|12))?"},
				new string[] { dayOfMonthPattern, string.Empty, $@"|({dayOfMonthPattern}W)|L|\?", string.Empty },
				new string[] { monthPattern, string.Empty, string.Empty, string.Empty },
				new string[] { dayOfWeekPattern, optDayOfWeek, string.Empty, @"|\?" },
				new string[] { yearPattern, string.Empty, string.Empty, string.Empty },
			};
			var inner =
				(from t in partsList
				 select string.Format(hyphenCommaPattern, t[0], t[1], t[2], t[3]))
				.Aggregate((s1, s2) => $"{s1} {s2}");

			_reg = new Regex($"^{inner}$");
		}

		public CronDateFinder Parse(string cronString)
		{
			if (string.IsNullOrEmpty(cronString))
			{
				throw new CronParsingException("invalid cron string");
			}
			Match m = _reg.Match(cronString);
			if (!m.Success)
			{
				throw new CronParsingException("invalid cron string");
			}

			return new CronDateFinder(
				m.Groups[1].Value,
				m.Groups[14].Value,
				m.Groups[27].Value,
				m.Groups[40].Value,
				m.Groups[53].Value,
				m.Groups[68].Value,
				m.Groups[81].Value
				);
		}
	}
}
