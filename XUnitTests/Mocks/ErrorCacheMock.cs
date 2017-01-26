using System;
using System.Collections.Generic;
using DataManager.Services;
using FluentAssertions;

namespace XUnitTests.Mocks
{
    class ErrorCacheMock : List<string>, IErrorCache
    {
        public void ShouldHaveNoErrors()
        {
            this.Should().BeEmpty(ToString());
        }

        public void ShouldContain(string errorMessage)
        {
            this.Should().Contain(errorMessage);
        }

        public void AddError(string message)
        {
            Add(message);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this);
        }
    }
}
