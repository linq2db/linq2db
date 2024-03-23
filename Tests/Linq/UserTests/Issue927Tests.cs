using System.Data;

using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue927Tests
	{
		[Test, Theory]
		public void ExternalConnectionDisposing(bool dispose)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var connection = new TestNoopConnection("");
#pragma warning restore CA2000 // Dispose objects before losing scope
			Assert.AreEqual(ConnectionState.Closed, connection.State);

			using (var db = new DataConnection(new TestNoopProvider(), connection, dispose))
			{
				var c = db.Connection;
				Assert.AreEqual(ConnectionState.Open, c.State);
			}

			Assert.AreEqual(ConnectionState.Closed, connection.State);
			Assert.AreEqual(dispose,                connection.IsDisposed);
		}

		[Test]
		public void InternalConnectionDisposed()
		{
			TestNoopConnection? connection;

			using (var db = new DataConnection(new TestNoopProvider(), ""))
			{
				connection = db.Connection as TestNoopConnection;
				Assert.NotNull (connection);
				Assert.AreEqual(ConnectionState.Open, connection!.State);
			}

			Assert.AreEqual(ConnectionState.Closed, connection.State);
			Assert.True    (connection.IsDisposed);
		}

		[Test]
		public void ExternalConnectionNotClosed()
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var connection = new TestNoopConnection("");
#pragma warning restore CA2000 // Dispose objects before losing scope
			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State);

			using (var db = new DataConnection(new TestNoopProvider(), connection, false))
			{
				var c = db.Connection;
				Assert.AreEqual(ConnectionState.Open, c.State);
			}

			Assert.AreEqual(ConnectionState.Open, connection.State);
			Assert.AreEqual(false,                connection.IsDisposed);
		}
	}
}
