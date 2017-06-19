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
		private class TestDataConnection : DataConnection
		{
			public TestDataConnection(string providerName, string connectionString) : base(providerName, connectionString)
			{
			}

			protected override Exception ExceptionIntercept(Exception original)
			{
				return Intercept(original) ;
			}

			static Func<Exception, Exception> defaultExceptionIntercept = (original) => original;
			private Func<Exception, Exception> _exceptionIntercept = defaultExceptionIntercept;
			public Func<Exception, Exception> Intercept
			{
				get { return _exceptionIntercept; }
				set { _exceptionIntercept = value ?? defaultExceptionIntercept; }
			}
		}

		public class TestTable
		{
			[Column(IsIdentity = true)]
			public int ID { get; set; }
		}

		private TestDataConnection _connection;



		[OneTimeSetUp]
		public void SetUp()
		{
			_connection = new TestDataConnection(ProviderName.SQLite, "Data Source=:memory:;");


		}

		[NUnit.Framework.TearDown]
		public void ResetIntercept()
		{
			_connection.Intercept = null;
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
			_connection.Intercept = (ex) => { return new DivideByZeroException("Intercepted exception", ex); };

			var table = _connection.GetTable<TestTable>();
			Assert.Catch<DivideByZeroException>(() => table.ToList());
		}

		[Test]
		public void InterceptedResetExecuteReader()
		{
			_connection.Intercept = (ex) => { return new DivideByZeroException("Intercepted exception", ex); };

			var table = _connection.GetTable<TestTable>();
			Assert.Catch<DivideByZeroException>(() => table.ToList());

			_connection.Intercept = null;

			Assert.Catch<SQLiteException>(() => table.ToList());
		}

        [Test]
        public void InterceptedRetryExecuteReader()
        {
            int count = 0;
            _connection.Intercept = (ex) => 
            {
                if (count < 1)
                {
                    count++;
                    return null;
                }

                return new DivideByZeroException("Intercepted exception", ex); 
            };

            var table = _connection.GetTable<TestTable>();
            Assert.Catch<DivideByZeroException>(() => table.ToList());
            Assert.AreEqual(1, count);
        }


        [Test]
		public void InterceptedExceptionExecuteNonQuery()
		{
			var table = _connection.CreateTable<TestTable>();
			_connection.Close();

			_connection.Intercept = (ex) => { return new DivideByZeroException("Intercepted exception", ex); };

			Assert.Catch<DivideByZeroException>(() => table.Drop());
		}

        [Test]
        public void InterceptedRetryExecuteNonQuery()
        {
            int count = 0;
            _connection.Intercept = (ex) =>
            {
                if (count < 1)
                {
                    count++;
                    return null;
                }

                return new DivideByZeroException("Intercepted exception", ex);
            };

            var table = _connection.CreateTable<TestTable>();
            _connection.Close();

            Assert.Catch<DivideByZeroException>(() => table.Drop());
            Assert.AreEqual(1, count);
        }

        [Test]
		public void InterceptedExceptionExecuteScalar()
		{

			// TODO: find a query that will excercise ExecuteScalar on its own so we can test the call.

		}
#endif
	}
}