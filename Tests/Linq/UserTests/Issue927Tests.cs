using System;
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
			var connection = new TestNoopConnection("");
			Assert.AreEqual(ConnectionState.Closed, connection.State);

			using (var db = new DataConnection(new TestNoopProvider(), connection, dispose))
			{
				var c = db.Connection;
				Assert.AreEqual(ConnectionState.Open, c.State);
			}

			Assert.AreEqual(ConnectionState.Closed, connection.State);
			Assert.AreEqual(dispose,                connection.Disposed);
		}

		[Test]
		public void InternalConnectionDisposed()
		{
			TestNoopConnection connection;

			using (var db = new DataConnection(new TestNoopProvider(), ""))
			{
				connection = db.Connection as TestNoopConnection;
				Assert.NotNull (connection);
				Assert.AreEqual(ConnectionState.Open, connection.State);
			}

			Assert.AreEqual(ConnectionState.Closed, connection.State);
			Assert.True    (connection.Disposed);
		}

		[Test]
		public void ExternalConnectionNotClosed()
		{
			var connection = new TestNoopConnection("");
			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State);

			using (var db = new DataConnection(new TestNoopProvider(), connection, false))
			{
				var c = db.Connection;
				Assert.AreEqual(ConnectionState.Open, c.State);
			}

			Assert.AreEqual(ConnectionState.Open, connection.State);
			Assert.AreEqual(false,                connection.Disposed);
		}

		[Test]
		public void CloneConnectionDisposed()
		{
			var connection = new TestNoopConnection("");
			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State);

			TestNoopConnection cloneConnection;

			using (var db = new DataConnection(new TestNoopProvider(), connection, false))
			{
				var c = db.Connection;
				Assert.AreEqual(ConnectionState.Open, c.State);

				using (var db2 = (DataConnection)db.Clone())
				{
					cloneConnection = db2.Connection as TestNoopConnection;
					Assert.NotNull    (cloneConnection);
					Assert.AreNotEqual(cloneConnection,      connection);
					Assert.AreEqual   (ConnectionState.Open, cloneConnection.State);
				}
			}

			Assert.AreEqual(ConnectionState.Open, connection.State);
			Assert.AreEqual(false,                connection.Disposed);

			Assert.IsNotNull(cloneConnection);
			Assert.AreEqual (ConnectionState.Closed, cloneConnection.State);
			Assert.AreEqual (true,                   cloneConnection.Disposed);

		}
	}
}
