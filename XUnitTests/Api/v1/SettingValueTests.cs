using System;
using System.Collections.Generic;
using DataManager.Api.v1;
using DataManager.Services.Variables;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace XUnitTests.Api.v1
{
    public class SettingValueTests
    {
        class Container
        {
            public SettingValue Value { get; set; }
        }

        [Theory, MemberData(nameof(FromVariableData))]
        public void TestDeserializeFromVariable(string json, string variableName, string packageId = null, object defaultValue = null)
        {
            var settings = JsonConvert.DeserializeObject<Container>(json).Value;

            settings.IsNull.Should().BeFalse();
            settings.ValueType.Should().Be(SettingValueType.Variable);
            settings.ConstantValue.Should().BeNull();

            settings.FromVariable.PackageId.Should().Be(packageId);
            settings.FromVariable.VariableName.Should().Be(variableName);
            settings.FromVariable.DefaultValue.Should().Be(defaultValue);
        }

        public static IEnumerable<object[]> FromVariableData()
        {
            return new[]
            {
                new object[] {"{\"Value\":{\"variable-name\":\"freddy\"}}", "freddy"},
                new object[] {"{\"Value\":{\"variable-name\":\"spanky\",\"package-id\":\"PKG\"}}", "spanky", "PKG"},
                new object[]
                {
                    "{\"Value\":{\"variable-name\":\"donkey\",\"package-id\":\"Kong\",\"default\":\"lsjv;ls\"}}",
                    "donkey", "Kong", "lsjv;ls"
                },
                new object[]
                {
                    "{\"Value\":{\"variable-name\":\"donkey\",\"default\":\"lsjv;ls\"}}",
                    "donkey", null, "lsjv;ls"
                },
                new object[] {"{\"Value\":\"PKG::freddy\"}", "freddy", "PKG"}
            };
        }

        [Theory, MemberData(nameof(ConstantData))]
        public void TestDeserializeConstant(string json, object value, Units units, SupportedTypes supportedType)
        {
            var settings = JsonConvert.DeserializeObject<Container>(json).Value;

            settings.IsNull.Should().BeFalse();
            settings.ValueType.Should().Be(SettingValueType.Constant);
            settings.FromVariable.Should().BeNull();

            settings.ConstantValue.Value.Should().Be(value);
            settings.ConstantValue.Units.Should().Be(units);
            settings.ConstantValue.SupportedType.Should().Be(supportedType);
        }

        public static IEnumerable<object[]> ConstantData()
        {
            return new[]
            {
                new object[] {"{\"Value\":\"88w\"}", 88, Units.Weeks, SupportedTypes.Integer},
                new object[] {"{\"Value\":\" 7 years   \"}", 7, Units.Years, SupportedTypes.Integer}
            };
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"Value\":null}")]
        [InlineData("{\"Value\":{}}")]
        public void TestDeserializeNull(string json)
        {
            var settings = JsonConvert.DeserializeObject<Container>(json).Value;
            settings.Should().BeNull();
        }
    }
}
