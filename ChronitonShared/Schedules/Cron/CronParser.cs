using System.Linq;
using System.Text.RegularExpressions;

namespace Chroniton.Schedules.Cron
{
    public class CronParser
    {
        const string secMinValues = @"([0-5]?[0-9])";
        const string hoursValues = @"([01]?[0-9]|2[0-3])";
        const string dayOfMonthValues = @"(0?[1-9]|[12][0-9]|3[01])";
        const string monthValues = @"(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|(0?[0-9]|1[0-2]))";
        const string dayOfWeekValues = @"(SUN|MON|TUE|WED|THUR?|FRI|SAT|[0-6])";
        const string optDayOfWeek = @"(L|#[1-5]|\?)";
        const string yearValues = @"([0-9]{4})";

        const string mainPattern = @"((({0}(\-{0})?)(,{0}(\-{0})?)*)|\*|{1})";
        const string slashPattern = @"(({0}|\*)/({1}))?";

        const string secMinSlashValues = "([2-6]|1[025]|[23]0)";
        const string hoursSlashValues = "([23468]|12)";
        const string monthSlashValues = "[2346]";

        static readonly Regex _reg;

        static CronParser()
        {
            string[][] partsList = new string[][]
            {
                new string[] { secMinValues,        string.Format(slashPattern, secMinValues, secMinSlashValues) },
                new string[] { secMinValues,        string.Format(slashPattern, secMinValues, secMinSlashValues) },
                new string[] { hoursValues,         string.Format(slashPattern, hoursValues, hoursSlashValues)},
                new string[] { dayOfMonthValues,    $@"(({dayOfMonthValues}W)|L|\?)" },
                new string[] { monthValues,         string.Format(slashPattern, monthValues, monthSlashValues) },
                new string[] { dayOfWeekValues,     $@"({dayOfWeekValues}(L|#[1-5])|\?)", },
                new string[] { yearValues,          string.Empty }
            };
            var inner =
                (from t in partsList
                 select string.Format(mainPattern, t[0], t[1]))
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
                m.Groups[16].Value,
                m.Groups[31].Value,
                m.Groups[46].Value,
                m.Groups[59].Value,
                m.Groups[78].Value,
                m.Groups[91].Value
                );
        }
    }
}
