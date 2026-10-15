using SecureFileManager.Domain.Entities;
using Xunit;

namespace SecureFileManager.Tests
{
    public class ExampleTests
    {
        [Fact]
        public void AdminUser_CanBeCreated()
        {
            var u = new AdminUser { Id = System.Guid.NewGuid(), Username = "admin", PasswordHash = "hash", CreatedAt = System.DateTime.UtcNow, UpdatedAt = System.DateTime.UtcNow };
            Assert.Equal("admin", u.Username);
        }
    }
}
