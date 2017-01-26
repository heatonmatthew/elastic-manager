using System.Linq;
using DataManager.Api.v1;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using FluentAssertions;
using Xunit;
using XUnitTests.Mocks;

namespace XUnitTests.Services.Scoping
{
    public class VariableSetTests
    {
        public VariableSetTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            scopeStack = NestedScopeTests.CreateNestedScopes(3);
            scopeTop = scopeStack[0];
            scopeBottom = scopeStack[scopeStack.Length - 1];

            this.output = output;
        }

        // Common Context
        private readonly NestedScope[] scopeStack;
        private readonly NestedScope scopeTop;
        private readonly NestedScope scopeBottom;

        private readonly Xunit.Abstractions.ITestOutputHelper output;

        [Fact]
        public void TestEmptyVariableSet()
        {
            scopeBottom
                .TryFindVariable("packageid::variablename", out var result)
                .Should().BeFalse();

            result.Should().BeNull();

            scopeTop
                .TryFindVariable("packageid::variablename", out result)
                .Should().BeFalse();

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestFindVariable(int existsAtLevel)
        {
            var variable = new Variable {PackageId = "packageid", Name = "variablename"};
            scopeStack[existsAtLevel].Store(variable);

            var lookup = scopeBottom.FindVariable("packageid::variablename");
            lookup.Should().BeSameAs(variable);
        }

        [Theory]
        [InlineData(0, new[] {"top::var1"})]
        [InlineData(1, new[] {"middle::var1"})]
        [InlineData(2, new[] {"middle::var1", "bottom::var2"})]
        public void TestFirstVarOfName(int lookupLevel, string[] shouldFind)
        {
            // Store three variables at the top and bottom of the stack
            var var1 = new Variable
            {
                PackageId = "top",
                Name = "var1",
                Units = Units.None.GetStringValue(),
                Type = SupportedTypes.Integer.GetStringValue()
            };
            scopeStack[0].Store(var1);
            var var1A = new Variable
            {
                PackageId = "middle",
                Name = "var1",
                Units = Units.None.GetStringValue(),
                Type = SupportedTypes.Integer.GetStringValue()
            };
            scopeStack[1].Store(var1A);
            var var2 = new Variable
            {
                PackageId = "bottom",
                Name = "var2",
                Units = Units.None.GetStringValue(),
                Type = SupportedTypes.Integer.GetStringValue()
            };
            scopeStack[2].Store(var2);

            // Lookup names var1 and var2 at the specified level in the scope nesting
            var lookupScope = scopeStack[lookupLevel];
            var errorCache = new ErrorCacheMock();

            var found = new[]
                {
                    lookupScope.FirstVarOfName("var1", errorCache),
                    lookupScope.FirstVarOfName("var2", errorCache)
                }
                .Where(v => v != null)
                .Select(v => v.Ref.ToFullName())
                .ToArray();

            foreach (var message in errorCache)
            {
                output.WriteLine(message);
            }

            found.Should().Equal(shouldFind);
            errorCache.Count.Should().Be(2 - shouldFind.Length);
        }
    }
}
