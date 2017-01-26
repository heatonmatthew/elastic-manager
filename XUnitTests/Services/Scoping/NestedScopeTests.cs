using DataManager.Services.Scoping;
using FluentAssertions;
using Nest;
using Xunit;
using XUnitTests.Mocks;

namespace XUnitTests.Services.Scoping
{
    public class NestedScopeTests
    {
        class TestAspect : INestedAspect
        {
            public NestedScope Scope { get; set; }

            public INestedAspect Clone()
            {
                return new TestAspect {Scope = Scope};
            }

            public void RecordChanges(INestedAspect compareTo, BulkDescriptor bulk)
            {
                throw new System.NotImplementedException();
            }
            public void RemoveAll(BulkDescriptor bulk)
            {
                throw new System.NotImplementedException();
            }
        }

        public static NestedScope[] CreateNestedScopes(int depth = 3)
        {
            var result = new NestedScope[depth];

            NestedScope prev = null;
            for (int i = 0; i < depth; ++i)
            {
                var current = new NestedScope(prev);
                result[i] = current;
                prev = current;
            }

            return result;
        }

        [Fact]
        public void TestConstruction()
        {
            var topLevel = new NestedScope();

            topLevel.IsTopMost.Should().BeTrue();
            topLevel.Parent.Should().BeNull();
            topLevel.Children.Should().BeEmpty();

            var midLevel = new NestedScope(topLevel);

            topLevel.IsTopMost.Should().BeTrue();
            midLevel.IsTopMost.Should().BeFalse();

            topLevel.Parent.Should().BeNull();
            midLevel.Parent.Should().BeSameAs(topLevel);

            topLevel.Children.Should().Contain(midLevel);
            midLevel.Children.Should().BeEmpty();

            var lowLevel = new NestedScope(midLevel);

            topLevel.IsTopMost.Should().BeTrue();
            midLevel.IsTopMost.Should().BeFalse();
            lowLevel.IsTopMost.Should().BeFalse();

            topLevel.Parent.Should().BeNull();
            midLevel.Parent.Should().BeSameAs(topLevel);
            lowLevel.Parent.Should().BeSameAs(midLevel);

            topLevel.Children.Should().Contain(midLevel);
            midLevel.Children.Should().Contain(lowLevel);
            lowLevel.Children.Should().BeEmpty();
        }

        [Fact]
        public void TestAspects()
        {
            var stack = CreateNestedScopes(3);
            var topLevel = stack[0];
            var midLevel = stack[1];
            var lowLevel = stack[2];

            var midAspect = midLevel.GetAspect<TestAspect>();
            var lowAspect = lowLevel.GetAspect<TestAspect>();
            var topAspect = topLevel.GetAspect<TestAspect>();

            topAspect.Scope.Should().BeSameAs(topLevel);
            midAspect.Scope.Should().BeSameAs(midLevel);
            lowAspect.Scope.Should().BeSameAs(lowLevel);

            topAspect.Should().NotBeSameAs(midAspect);
            topAspect.Should().NotBeSameAs(lowAspect);
            lowAspect.Should().NotBeSameAs(midAspect);
        }

        [Fact]
        public void TestBuildNestedHierarchy()
        {
            var names = new[]
            {
                "a.b", "a.d.e", "a", "a.b.c", "a.c", "a.B.c", "a.d", "a"
            };

            var errorMock = new ErrorCacheMock();
            var scopes = NestedScope.BuildHeirarchy(names, errorMock);

            errorMock.Count.Should().Be(0);
            scopes.Count.Should().Be(6);

            foreach (var pair in scopes)
            {
                pair.Value.Name.Should().Be(pair.Key);
            }

            var a = scopes["a"];
            var ab = scopes["a.b"];
            var ac = scopes["a.c"];
            var ad = scopes["a.d"];
            var abc = scopes["a.b.c"];
            var ade = scopes["a.d.e"];

            a.IsTopMost.Should().BeTrue();
            a.Children.Should().Contain(new[] {ab, ac, ad});
            ab.Children.Should().Contain(abc);
            ad.Children.Should().Contain(ade);
        }

        [Fact]
        public void TestMergeWithExistingHierarchy()
        {
            var names = new[]
            {
                "a.b", "a.d.e", "a", "a.b.c", "a.c", "a.B.c", "a.d", "a"
            };

            var errorMock = new ErrorCacheMock();
            var scopes = NestedScope.BuildHeirarchy(names, errorMock);

            names = new[] {"a.b", "a.b.c", "a.d.f", "a.z"};
            scopes = NestedScope.BuildHeirarchy(names, errorMock, scopes);

            errorMock.Count.Should().Be(0);
            scopes.Count.Should().Be(8);

            foreach (var pair in scopes)
            {
                pair.Value.Name.Should().Be(pair.Key);
            }

            var a = scopes["a"];
            var ab = scopes["a.b"];
            var ac = scopes["a.c"];
            var ad = scopes["a.d"];
            var abc = scopes["a.b.c"];
            var ade = scopes["a.d.e"];
            var adf = scopes["a.d.f"];
            var az = scopes["a.z"];

            a.IsTopMost.Should().BeTrue();
            a.Children.Should().Contain(new[] {ab, ac, ad, az});
            ab.Children.Should().Contain(abc);
            ad.Children.Should().Contain(new[] {ade, adf});
            az.Children.Should().BeEmpty();
        }
    }
}
