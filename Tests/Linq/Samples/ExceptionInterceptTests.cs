using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;
using System.Data.SQLite;

namespace Tests.Samples
{
	[TestFixture]
	public class ExceptionInterceptTests : TestBase
	{
#if !MONO
		public class TestTable
		{
			[Column(IsIdentity = true)]
			public int ID { get; set; }
		}

		private DataConnection _connection;

		[OneTimeSetUp]
		public void SetUp()
		{
			_connection = new DataConnection(ProviderName.SQLite, "Data Source=:memory:;");


		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_connection.Dispose();
		}

		[Test]
		public void StandardExceptionExecuteReader()
		{
			var table = _connection.GetTable<TestTable>();
			Assert.Catch<SQLiteException>(() => table.ToList());
		}

		[Test]
		public void InterceptedExceptionExecuteReader()
		{
			_connection.ExceptionIntercept = (ex) => throw new DivideByZeroException("Intercepted exception", ex);

			var table = _connection.GetTable<TestTable>();
			Assert.Catch<DivideByZeroException>(() => table.ToList());
		}

		[Test]
		public void InterceptedResetExecuteReader()
		{
			_connection.ExceptionIntercept = (ex) => throw new DivideByZeroException("Intercepted exception", ex);

			var table = _connection.GetTable<TestTable>();
			Assert.Catch<DivideByZeroException>(() => table.ToList());

			_connection.ExceptionIntercept = null;

			Assert.Catch<SQLiteException>(() => table.ToList());
		}

		[Test]
		public void InterceptedExceptionExecuteNonQuery()
		{
			var table = _connection.CreateTable<TestTable>();
			_connection.Close();

			_connection.ExceptionIntercept = (ex) => throw new DivideByZeroException("Intercepted exception", ex);
			
			Assert.Catch<DivideByZeroException>(() => table.Drop());
		}

		[Test]
		public void InterceptedExceptionExecuteScalar()
		{

			// TODO: find a query that will excercise ExecuteScalar on its own so we can test the call.

		}


#endif
	}
}