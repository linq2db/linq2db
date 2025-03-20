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
		var dbConn = new MockDbConnection();
		var db     = GetDataConnection(new DataOptions().UseConnection(
			new TestNoopProvider(), dbConn));

		var _ = await db.GetTable<TestEntity>().SingleOrDefaultAsync();
		
		Assert.That(dbConn.OpenedAsync, Is.True);
	}
	
	[Test]
	public async Task TestDataContext()
	{
		var dbConn = new MockDbConnection();
		var db     = GetDataContext("", dbOptions => dbOptions.UseConnection(
			new TestNoopProvider(), dbConn));

		var _ = await db.GetTable<TestEntity>().SingleOrDefaultAsync();
		
		Assert.That(dbConn.OpenedAsync, Is.True);
	}

	private class TestEntity
	{
		
	}

	private class MockDbConnection() : TestNoopConnection(string.Empty)
	{
		public bool OpenedAsync { get; private set; }

		public override    void          Open()
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
