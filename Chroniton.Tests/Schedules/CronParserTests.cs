using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Chroniton.Schedules.Cron;

namespace Chroniton.Tests.Schedules
{   
    public class CronParserTests
    {
        [Fact]
        public void CtorShouldNotFail()
        {
            Assert.NotNull(new CronParser());
        }
    }

    public class ParseTests
    {
        CronParser parserUnderTest;

        void initParser()
        {
            parserUnderTest = new CronParser();
        }

        public class WhenNotValidCronString : ParseTests
        {
            [Fact]
            public void ShouldReturnNull()
            {
                initParser();
                Assert.Null(parserUnderTest.Parse(string.Empty));
            }
        }

        public class WhenMatching : ParseTests
        {
            [Fact]
            public void ShouldReturnCron()
            {
                CronParser parser = new CronParser();
                var result = parser.Parse("0 1 2 3 JAN SUN 2000");
                Assert.NotNull(result);
                Assert.Equal("0", result.Seconds);
                Assert.Equal("1", result.Minutes);
                Assert.Equal("2", result.Hours);
                Assert.Equal("3", result.DayOfMonth);
                Assert.Equal("JAN", result.Month);
                Assert.Equal("SUN", result.DayOfWeek);
                Assert.Equal("2000", result.Year);
            }

            public class GetNextTests : ParseTests
            {
                [Theory
                    //day of Month
                    , InlineData("0 0 0 1 * ? *", "1/1/2017 00:00:00", "2/1/2017 00:00:00")
                    , InlineData("0 0 0 2 * ? *", "1/1/2017 00:00:00", "1/2/2017 00:00:00")
                    , InlineData("0 0 0 2,3 * ? *", "1/1/2017 00:00:00", "1/2/2017 00:00:00")
                    , InlineData("0 0 0 2,15 * ? *", "1/2/2017 00:00:00", "1/15/2017 00:00:00")
                    , InlineData("0 0 0 2,15 * ? *", "1/16/2017 00:00:00", "2/2/2017 00:00:00")
                    , InlineData("0 0 0 2-15 * ? *", "1/16/2017 00:00:00", "2/2/2017 00:00:00")
                    , InlineData("0 0 0 2-15 * ? *", "1/1/2017 00:00:00", "1/2/2017 00:00:00")
                    , InlineData("0 0 0 2-15 * ? *", "1/10/2017 00:00:00", "1/11/2017 00:00:00")
                    , InlineData("0 0 0 2-15 * ? *", "1/15/2017 00:00:00", "2/2/2017 00:00:00")
                    //day of month special characters
                    //LEONARD DO THIS NEXT

                    //day of week
                    , InlineData("0 0 0 ? * MON-FRI *", "1/1/2017 00:00:00", "1/2/2017 00:00:00")//hyphen day of week
                    , InlineData("0 0 0 ? * MON-FRI *", "1/2/2017 00:00:00", "1/3/2017 00:00:00")
                    , InlineData("0 0 0 ? * MON,FRI *", "1/2/2017 00:00:00", "1/6/2017 00:00:00")// comma day of week
                    , InlineData("0 0 0 ? * 1,5 *", "1/2/2017 00:00:00", "1/6/2017 00:00:00")
                    , InlineData("0 0 0 ? * 1,4-5 *", "1/2/2017 00:00:00", "1/5/2017 00:00:00")// commma hyphen day of week
                    , InlineData("0 0 0 ? * SUN#2 *", "1/31/1998 00:00:00", "2/8/1998 00:00:00")// specific day of week
                    , InlineData("0 0 0 ? * 0L *", "1/31/1998 00:00:00", "2/22/1998 00:00:00")//last day of week
                    , InlineData("0 0 0 ? * SUN#5 *", "1/31/1998 00:00:00", "2/22/1998 00:00:00")
                    // worst cases
                    , InlineData("0 0 0 ? * SUN *", "1/31/1998 00:00:00", "2/1/1998 00:00:00")
                    , InlineData("0 0 0 1 JAN ? *", "1/1/2000 00:00:00", "1/1/2001 00:00:00")
                    , InlineData("0 0 0 1 JAN ? 2000", "1/1/2000 00:00:00", null)
                    ]
                public void ShouldReturnCorrectly(string input, string startDate, string expectedDate)
                {
                    initParser();
                    var finder = parserUnderTest.Parse(input);
                    var next = finder.GetNext(DateTime.Parse(startDate));
                    if (expectedDate == null)
                    {
                        Assert.Null(next);
                    }
                    else
                    {
                        Assert.Equal(DateTime.Parse(expectedDate), next);
                    }
                }
            }
        }
    }
}
