using System.Data;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue927Tests
	{
		[Test]
		public void ExternalConnectionDisposing([Values] bool dispose)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var connection = new TestNoopConnection("");
#pragma warning restore CA2000 // Dispose objects before losing scope
			Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));

			using (var db = new DataConnection(new DataOptions().UseConnection(new TestNoopProvider(), connection, dispose)))
			{
				var c = db.OpenDbConnection();
				Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
			}

			Assert.Multiple(() =>
			{
				Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));
				Assert.That(connection.IsDisposed, Is.EqualTo(dispose));
			});
		}

		[Test]
		public void InternalConnectionDisposed()
		{
			TestNoopConnection? connection;

			using (var db = new DataConnection(new DataOptions().UseConnectionString(new TestNoopProvider(), "")))
			{
				connection = db.OpenDbConnection() as TestNoopConnection;
				Assert.That(connection!.State, Is.EqualTo(ConnectionState.Open));
			}

			Assert.Multiple(() =>
			{
				Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));
				Assert.That(connection.IsDisposed, Is.True);
			});
		}

		[Test]
		public void ExternalConnectionNotClosed()
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var connection = new TestNoopConnection("");
#pragma warning restore CA2000 // Dispose objects before losing scope
			connection.Open();

			Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));

			using (var db = new DataConnection(new DataOptions().UseConnection(new TestNoopProvider(), connection, false)))
			{
				var c = db.TryGetDbConnection();
				Assert.That(c, Is.Not.Null);
				Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
			}

			Assert.Multiple(() =>
			{
				Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));
				Assert.That(connection.IsDisposed, Is.EqualTo(false));
			});
		}
	}
}
