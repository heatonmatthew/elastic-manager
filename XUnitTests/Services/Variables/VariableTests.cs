using System;
using DataManager.Api.v1;
using DataManager.Services.Variables;
using EnumStringValues;
using FluentAssertions;
using Xunit;
using XUnitTests.Mocks;

namespace XUnitTests.Services.Variables
{
    public class VariableTests
    {
        [Theory]
        [InlineData("TestPackage::NameTest", "TestPackage", "NameTest", null)]
        [InlineData("Alternate.Package::Average.Age", "Alternate.Package", "Average.Age", 1)]
        public void VariableRefParsingWorks(string fullName, string packageId, string variableName, object defaultValue)
        {
            // Test the conversion to full name
            var varRef = new VariableRef
            {
                PackageId = packageId,
                VariableName = variableName,
                DefaultValue = defaultValue
            };
            varRef.ToFullName().Should().Be(fullName);

            // Test Parsing back out
            varRef = VariableRef.Parse(fullName, defaultValue);
            varRef.PackageId.Should().Be(packageId);
            varRef.VariableName.Should().Be(variableName);
            varRef.DefaultValue.ShouldBeEquivalentTo(defaultValue);
            
            // Test successful TryParse
            VariableRef.TryParse(fullName, out var another, defaultValue).Should().BeTrue();
            another.PackageId.Should().Be(packageId);
            another.VariableName.Should().Be(variableName);
            another.DefaultValue.ShouldBeEquivalentTo(defaultValue);
        }

        [Theory]
        [InlineData("alskdjf;lasdjf")]
        [InlineData("package:variable")]
        [InlineData("package::variable::something")]
        public void VariableRefParsingFailsCorrectly(string fullName)
        {
            VariableRef.TryParse(fullName, out var result).Should().BeFalse();

            Assert.Throws<ArgumentOutOfRangeException>(() => VariableRef.Parse(fullName));
        }

        [Theory]
        [InlineData(1, 1, SupportedTypes.Integer, Units.Weeks)]
        [InlineData(1, null, SupportedTypes.Integer, Units.Weeks, 0, 55)]
        [InlineData(7, 300, SupportedTypes.Integer, Units.Days, null, 300)]
        [InlineData(7, 300, SupportedTypes.Integer, Units.None, 72, null)]
        [InlineData("value", "default", SupportedTypes.String, Units.None)]
        public void ExtractVariableWorks(object value, object defaultValue, SupportedTypes supportedType, Units unit, object max = null, object min = null)
        {
            var variable = new Variable
            {
                Name = "Test",
                Value = value?.ToString(),
                Default = defaultValue?.ToString(),
                Max = max?.ToString(),
                Min = min?.ToString(),
                Type = supportedType.GetStringValue(),
                Units = unit.GetStringValue()
            };

            var errorCache = new ErrorCacheMock();
            var info = VariableInfo.Extract(variable, "myPackage", errorCache);

            errorCache.ShouldHaveNoErrors();

            info.Ref.VariableName.Should().Be("Test");
            info.Ref.PackageId.Should().Be("myPackage");

            info.HasErrors.Should().BeFalse();
            info.Value.ShouldBeEquivalentTo(value);
            info.Default.ShouldBeEquivalentTo(defaultValue);
            info.Min.ShouldBeEquivalentTo(min);
            info.Max.ShouldBeEquivalentTo(max);
            info.SupportedType.Should().Be(supportedType);
            info.Units.Should().Be(unit);
        }

        [Theory]
        [InlineData(1, 1, "blob", "weeks", "Error parsing Type for variable")]
        [InlineData(1, 1, null, "weeks", "Error parsing Type for variable")]
        [InlineData(1, 1, "integer", "lightyear", "Error parsing Units for variable")]
        [InlineData(1, 1, "int", null, "Error parsing Units for variable")]
        [InlineData("ASDf", 1, "integer", "weeks", "Could not parse 'ASDf' as an integer")]
        [InlineData(5, "dlkjlsk", "integer", "weeks", "Could not parse 'dlkjlsk' as an integer")]
        public void ExtractVariableWithErrors(object value, object defaultValue, string supportedType, string unit, string errorMessage)
        {
            var variable = new Variable
            {
                Name = "Test",
                Value = value?.ToString(),
                Default = defaultValue?.ToString(),
                Type = supportedType,
                Units = unit
            };

            var errorCache = new ErrorCacheMock();
            var info = VariableInfo.Extract(variable, "myPackage", errorCache);

            errorCache.ShouldContain(errorMessage);
            info.HasErrors.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 2, 1)]
        [InlineData(null, 2, 2)]
        [InlineData(-5, 2, -5)]
        [InlineData(null, -27, -27)]
        public void IntegerValueTypeConversionWorks(int? value, int? defaultValue, int expected)
        {
            var variable = new Variable
            {
                Name = "sample",
                Value = value?.ToString(),
                Default = defaultValue?.ToString(),
                Type = SupportedTypes.Integer.GetStringValue(),
                Units = Units.None.GetStringValue()
            };

            var errorCache = new ErrorCacheMock();
            var info = VariableInfo.Extract(variable, "myPackage", errorCache);

            info.GetValueOrDefault<int>(errorCache).Should().Be(expected);
            errorCache.Count.Should().Be(0);
        }

        [Fact]
        public void IntegerValueTypeConversionFailsCorrectly()
        {
            var variable = new Variable
            {
                Name = "sample",
                Value = "abc",
                Type = SupportedTypes.Integer.GetStringValue(),
                Units = Units.None.GetStringValue()
            };

            var errorCache = new ErrorCacheMock();
            var info = VariableInfo.Extract(variable, "myPackage", errorCache);
            errorCache.Count.Should().Be(1);

            errorCache.Clear();
            Assert.Throws<InvalidOperationException>(() => info.GetValueOrDefault<int>(errorCache));
            errorCache.Count.Should().Be(1);
        }

        [Theory]
        [InlineData("Hello", "World", "Hello")]
        [InlineData(null, "World", "World")]
        public void StringValueTypeConversionWorks(string value, string defaultValue, string expected)
        {
            var variable = new Variable
            {
                Name = "sample",
                Value = value,
                Default = defaultValue,
                Type = SupportedTypes.String.GetStringValue(),
                Units = Units.None.GetStringValue()
            };

            var errorCache = new ErrorCacheMock();
            var info = VariableInfo.Extract(variable, "myPackage", errorCache);

            info.GetValueOrDefault<string>(errorCache).Should().Be(expected);
            errorCache.Count.Should().Be(0);
        }
    }
}
