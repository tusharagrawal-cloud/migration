using System;
using One77.Core.Data;
using Xunit;

namespace One77.Core.Tests
{
    /// <summary>Pure unit tests — no database required.</summary>
    public class SqlConnectionFactoryTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullConnectionString()
        {
            Assert.Throws<ArgumentException>(() => new SqlConnectionFactory(null!));
        }

        [Fact]
        public void Constructor_ThrowsOnEmptyConnectionString()
        {
            Assert.Throws<ArgumentException>(() => new SqlConnectionFactory(string.Empty));
        }

        [Fact]
        public void Constructor_ThrowsOnWhitespaceConnectionString()
        {
            Assert.Throws<ArgumentException>(() => new SqlConnectionFactory("   "));
        }

        [Fact]
        public void Constructor_AcceptsNonEmptyConnectionString()
        {
            var factory = new SqlConnectionFactory("Server=localhost;Database=One77;");
            Assert.NotNull(factory);
        }
    }
}
