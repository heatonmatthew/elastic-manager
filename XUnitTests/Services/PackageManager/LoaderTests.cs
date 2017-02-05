using System.Linq;
using DataManager.Api.v1;
using DataManager.Services;
using DataManager.Services.PackageManager;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using FluentAssertions;
using Xunit;
using XUnitTests.Mocks;
using XUnitTests.Services.Scoping;

namespace XUnitTests.Services.PackageManager
{
    public class LoaderTests
    {
        public LoaderTests()
        {
            loader = new DataSetApiValidator
            {
                PackageId = TestPackageId,
                Scope = NestedScopeTests.CreateNestedScopes().Last(),
                ErrorCache = errorCache
            };
        }

        // Shared constants
        public const string TestPackageId = "Test.Package";

        private const int LocalDefault = 1;
        private const int AlternateDefault = -1;
        private const int GlobalDefault = 1000;

        // Shared context
        private readonly DataSetApiValidator loader;
        private readonly ErrorCacheMock errorCache = new ErrorCacheMock();

        [Theory]
        [InlineData("1d", 1, Units.Days)]
        [InlineData("7 years", 7, Units.Years)]
        [InlineData("1d", 1, Units.Days, true)]
        [InlineData("7 years", 7, Units.Years, true)]
        public void LoadConstantSettingWorks(string constValue, int expectedValue, Units expectedUnits, bool alreadyExists = false)
        {
            Variable existing = null;
            if (alreadyExists)
            {
                existing = new Variable {PackageId = TestPackageId, Name = "constant.MaxAge"};
                loader.Scope.Store(existing);
            }

            var constant = Constant.ParseEncoded(constValue);
            var variableInfo = loader.LoadConstantSetting(constant, "MaxAge");

            var variable = variableInfo.Variable;
            variable.Name.Should().Be("constant.MaxAge");
            variable.Value.Should().Be(expectedValue.ToString());
            variable.Default.Should().Be(expectedValue.ToString());
            variable.Units.Should().Be(expectedUnits.GetStringValue());
            variable.Type.Should().Be(SupportedTypes.Integer.GetStringValue());
            variable.IsGenerated.Should().BeTrue();

            variableInfo.Ref.PackageId.Should().Be(TestPackageId);

            if (existing != null) variable.Should().BeSameAs(existing);

            errorCache.ShouldHaveNoErrors();
        }

        [Fact]
        public void LookupReferencedAgeSettingSuccessfully()
        {
            var varRef = new VariableRef {PackageId = TestPackageId, VariableName = "var.name"};
            var existing = MakeSampleVariable(TestPackageId, varRef.VariableName);
            loader.Scope.Store(existing);
            
            var variableInfo = loader.LookupReferencedVariable(varRef);

            variableInfo.Variable.Should().BeSameAs(existing);
            variableInfo.Ref.PackageId.Should().Be(TestPackageId);
            errorCache.ShouldHaveNoErrors();
        }

        [Fact]
        public void LookupReferencedAgeSettingUnsuccessfully()
        {
            var varRef = new VariableRef {PackageId = TestPackageId, VariableName = "var.name"};
            var variableInfo = loader.LookupReferencedVariable(varRef);

            variableInfo.Should().BeNull();
            errorCache.ShouldContain($"Could not find variable: '{varRef.ToFullName()}'");
        }

        [Theory]
        [InlineData(null, null, null, "a.test.setting.name", GlobalDefault, false)]                 // default to global
        [InlineData("this.is.my.variable", null, null, "this.is.my.variable", LocalDefault, false)] // uses current package by default
        [InlineData("this.is.my.variable", TestPackageId, null, "this.is.my.variable", LocalDefault, false)]
        [InlineData("this.is.my.variable", "alternate.package.id", null, "this.is.my.variable", AlternateDefault, false)]
        [InlineData(null, null, "7days", "constant.a.test.setting.name", 7, true)]                  // generated variable for constant
        public void LocateSettingVariableTest(string variableName, string packageId, string constValue, string expectedName, int defaultValue, bool isGenerated)
        {
            const string settingName = "a.test.setting.name";

            // Setup the conditions
            if (variableName != null)
            {
                loader.Scope.Store(MakeSampleVariable(TestPackageId, variableName, defaultValue: LocalDefault));
                loader.Scope.Store(MakeSampleVariable("alternate.package.id", variableName, defaultValue: AlternateDefault));
            }
            loader.Scope.Store(MakeSampleVariable(Constants.PackageManagerIdentity, settingName, defaultValue: GlobalDefault));

            var setting = new SettingValue();
            if (!string.IsNullOrWhiteSpace(variableName))
            {
                setting.FromVariable = new VariableRef {VariableName = variableName, PackageId = packageId};
            }
            if (!string.IsNullOrWhiteSpace(constValue))
            {
                setting.ConstantValue = Constant.ParseEncoded(constValue);
            }

            // Act
            var variableInfo = loader.LocateSettingVariable(setting, settingName);
            var variable = variableInfo.Variable;
            var varRef = variableInfo.Ref;

            // Assert
            variable.Should().NotBeNull();
            variable.Name.Should().Be(expectedName);
            variable.Value.Should().Be("7");
            variable.Default.Should().Be(defaultValue.ToString());
            variable.Units.Should().Be(Units.Days.GetStringValue());
            variable.Type.Should().Be(SupportedTypes.Integer.GetStringValue());
            variable.IsGenerated.GetValueOrDefault().Should().Be(isGenerated);

            if (defaultValue == AlternateDefault)
            {
                varRef.PackageId.Should().Be("alternate.package.id");
            }
            else if (defaultValue == GlobalDefault)
            {
                varRef.PackageId.Should().Be(Constants.PackageManagerIdentity);
            }
            else
            {
                varRef.PackageId.Should().Be(TestPackageId);
            }

            errorCache.ShouldHaveNoErrors();
        }

        private static Variable MakeSampleVariable(
            string packageId,
            string name, 
            string value = "7", 
            string units = "days",
            string type = "integer",
            object defaultValue = null)
        {
            return new Variable
            {
                PackageId = packageId,
                Name = name,
                Value = value,
                Default = defaultValue?.ToString(),
                Units = units,
                Type = type
            };
        }
    }
}
