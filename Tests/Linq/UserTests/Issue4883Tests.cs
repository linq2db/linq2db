using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests;

[TestFixture]
public class Issue4883Tests : TestBase
{
	[Test]
	public async Task TestDataConnection()
	{
		var dbConn = new TestDbConnection();
		var db     = GetDataConnection(new DataOptions().UseConnection(
			new TestNoopProvider(), dbConn));

		_ = await db.GetTable<TestEntity>().SingleOrDefaultAsync();
		
		Assert.That(dbConn.OpenedAsync, Is.True);
	}
	
	[Test]
	public async Task TestDataContext()
	{
		var dbConn = new TestDbConnection();
		var db     = GetDataContext("", dbOptions => dbOptions.UseConnection(
			new TestNoopProvider(), dbConn));

		_ = await db.GetTable<TestEntity>().SingleOrDefaultAsync();
		
		Assert.That(dbConn.OpenedAsync, Is.True);
	}

	class TestEntity;

	class TestDbConnection() : TestNoopConnection(string.Empty)
	{
		public bool OpenedAsync { get; private set; }

		public override void Open()
		{
			OpenedAsync = false;
			base.Open();
		}

		public override Task OpenAsync(CancellationToken cancellationToken)
		{
			OpenedAsync = true;
			base.Open();
			return Task.CompletedTask;
		}
	}
}
