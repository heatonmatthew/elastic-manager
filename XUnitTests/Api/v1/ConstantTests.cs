using DataManager.Api.v1;
using DataManager.Services.Variables;
using FluentAssertions;
using Xunit;

namespace XUnitTests.Api.v1
{
    public class ConstantTests
    {
        [Theory]
        [InlineData("1d", true, 1, Units.Days)]
        [InlineData("27 days", true, 27, Units.Days)]
        [InlineData(" 9 weeks ", true, 9, Units.Weeks)]
        [InlineData(" 9 weeks ", true, 9, Units.Weeks)]
        [InlineData("1M", true, 1, Units.Months)]
        [InlineData("7years", true, 7, Units.Years)]
        [InlineData("", false)]
        [InlineData("1da", false)]
        [InlineData("eighty weeks", false)]
        public void ParsingEncodedIntegersWorks(
            string encoded, bool shouldSucceed,
            int? expected = null, Units? expectedUnits = null)
        {
            Constant.TryParseEncoded(encoded, out var value)
                .Should().Be(shouldSucceed);

            if (shouldSucceed)
            {
                value.Value.Should().Be(expected.Value);
                value.Units.Should().Be(expectedUnits.Value);
                value.SupportedType.Should().Be(SupportedTypes.Integer);
            }
        }

        [Theory]
        [InlineData(1, Units.Days, "1days")]
        [InlineData(7, Units.Years, "7years")]
        public void EncodingIntegersWorks(int value, Units units, string expected)
        {
            var constant = new Constant {Value = value, Units = units, SupportedType = SupportedTypes.Integer};
            constant.ToEncoded().Should().Be(expected);
        }

        [Theory]
        [InlineData("lskdj .asdf;;")]
        [InlineData("seven days")]
        public void ParsingInvalidValuesFails(string constValue)
        {
            Constant.TryParseEncoded(constValue, out var constant).Should().BeFalse();
        }
    }
}
