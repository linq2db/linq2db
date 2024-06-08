﻿using System.Data;

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
			Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));

			using (var db = new DataConnection(new TestNoopProvider(), connection, dispose))
			{
				var c = db.Connection;
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

			using (var db = new DataConnection(new TestNoopProvider(), ""))
			{
				connection = db.Connection as TestNoopConnection;
				Assert.That(connection, Is.Not.Null);
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

			using (var db = new DataConnection(new TestNoopProvider(), connection, false))
			{
				var c = db.Connection;
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
