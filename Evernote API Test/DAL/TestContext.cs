using Evernote_API_Test.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Evernote_API_Test.DAL
{
    public class TestContext : DbContext
    {
        public TestContext() : base("TestContext")
        {
        }

        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}