using System;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Version = DataManager.Api.v1.Version;

namespace XUnitTests.Api.v1
{
    public class VersionTests
    {
        class Container
        {
            public Version At { get; set; }
        }

        [Theory]
        [InlineData("1.2.3", 1, 2, 3)]
        [InlineData("1.22.300", 1, 22, 300)]
        [InlineData(" 1.22.300  ", 1, 22, 300)]
        public void TestSerializeDeserialize(string value, int major, int minor, int revision)
        {
            var version = new Version
            {
                Major = major,
                Minor = minor,
                Revision = revision
            };
            var asString = version.ToVersionString();
            asString.Should().Be(value.Trim());

            var v1 = Version.Parse(value);
            var v2 = Version.Parse(asString);

            v1.Should().Be(version);
            v2.Should().Be(version);
        }

        [Theory]
        [InlineData("asdfakll")]
        [InlineData("a 1.2.3")]
        [InlineData("1.2.3a")]
        public void TestDeserializationFailures(string value)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Version.Parse(value);
            });
        }

        [Fact]
        public void TestJsonConverter()
        {
            var container = new Container
            {
                At = new Version
                {
                    Major = 35,
                    Minor = 29,
                    Revision = 2999
                }
            };

            var json = JsonConvert.SerializeObject(container, Formatting.None);
            json.Should().Be("{\"At\":\"35.29.2999\"}");

            var container2 = JsonConvert.DeserializeObject<Container>("{\"At\":\"4.5.6\"}");

            container2.At.Major.Should().Be(4);
            container2.At.Minor.Should().Be(5);
            container2.At.Revision.Should().Be(6);

            container2.Should().NotBe(container);
        }
    }
}
