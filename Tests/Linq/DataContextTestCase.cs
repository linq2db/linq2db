using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Tests
{
	public abstract class DatabaseTestCase
	{
		public static Type GetDataContextType(
			bool includeLinqService, string[] except, string[] include)
		{
			var testCaseType = typeof(DataContextTestCase<,,,,,,,,,,,,,,,,,,>);
			var argTypes     = new Type[testCaseType.GetGenericArguments().Length];

			for (var i = 0; i < argTypes.Length; i++)
				argTypes[i] = typeof(None);

			var n = 0;

#if !MONO
			if (includeLinqService)
			{
				n++;
				argTypes[0] = typeof(LinqService);
			}
#endif

			if (include != null)
			{
				var list = include
					.Intersect(TestBase.UserProviders.Select(p => p.Name))
					.Select (GetContextType)
					.ToArray();

				for (var i = 0; i < list.Length; i++)
					argTypes[i + n] = list[i];
			}
			else
			{
				var list = TestBase.Providers
					.Where  (providerName => except == null || !except.Contains(providerName))
					.Where  (providerName => TestBase.UserProviders.Select(p => p.Name).Contains(providerName))
					.Select (GetContextType)
					.ToArray();

				for (var i = 0; i < list.Length; i++)
					argTypes[i + n] = list[i];
			}

			return  testCaseType.MakeGenericType(argTypes);
		}

		static Type GetContextType(string contextName)
		{
			switch (contextName)
			{
				case LinqToDB.ProviderName.SqlServer     : return typeof(SqlServer);
				case LinqToDB.ProviderName.SqlServer2008 : return typeof(SqlServer2008);
				case LinqToDB.ProviderName.SqlServer2012 : return typeof(SqlServer2012);
				case LinqToDB.ProviderName.SqlServer2014 : return typeof(SqlServer2014);
				case LinqToDB.ProviderName.SqlCe         : return typeof(SqlCe);
				case LinqToDB.ProviderName.SQLite        : return typeof(SQLite);
				case LinqToDB.ProviderName.Access        : return typeof(Access);
				case LinqToDB.ProviderName.SqlServer2000 : return typeof(SqlServer2000);
				case LinqToDB.ProviderName.SqlServer2005 : return typeof(SqlServer2005);
				case LinqToDB.ProviderName.DB2           : return typeof(DB2);
				case LinqToDB.ProviderName.Informix      : return typeof(Informix);
				case LinqToDB.ProviderName.Firebird      : return typeof(Firebird);
				case LinqToDB.ProviderName.Oracle        : return typeof(Oracle);
				case LinqToDB.ProviderName.PostgreSQL    : return typeof(PostgreSQL);
				case LinqToDB.ProviderName.MySql         : return typeof(MySql);
				case LinqToDB.ProviderName.Sybase        : return typeof(Sybase);
				case LinqToDB.ProviderName.SapHana       : return typeof(SapHana);
				case "Northwind"                         : return typeof(Northwind);
				case "SqlAzure.2012"                     : return typeof(SqlAzure2012);
			}

			throw new InvalidOperationException();
		}

		public virtual string ProviderName
		{
			get
			{
				return GetType().Name;
			}
		}

		internal class None : DatabaseTestCase
		{
			public override string ProviderName { get { return null; } }
		}

		internal class LinqService : None
		{
		}

		class Access        : DatabaseTestCase { }
		class SqlCe         : DatabaseTestCase { }
		class SQLite        : DatabaseTestCase { }
		class DB2           : DatabaseTestCase { }
		class Firebird      : DatabaseTestCase { }
		class Informix      : DatabaseTestCase { }
		class SqlServer     : DatabaseTestCase { }
		class SqlServer2000 : DatabaseTestCase { public override string ProviderName { get { return "SqlServer.2000"; } } }
		class SqlServer2005 : DatabaseTestCase { public override string ProviderName { get { return "SqlServer.2005"; } } }
		class SqlServer2008 : DatabaseTestCase { public override string ProviderName { get { return "SqlServer.2008"; } } }
		class SqlServer2012 : DatabaseTestCase { public override string ProviderName { get { return "SqlServer.2012"; } } }
		class SqlServer2014 : DatabaseTestCase { public override string ProviderName { get { return "SqlServer.2014"; } } }
		class SqlAzure2012  : DatabaseTestCase { public override string ProviderName { get { return "SqlAzure.2012";  } } }
		class MySql         : DatabaseTestCase { }
		class Oracle        : DatabaseTestCase { }
		class PostgreSQL    : DatabaseTestCase { }
		class Sybase        : DatabaseTestCase { }
		class Northwind     : DatabaseTestCase { }
		class SapHana       : DatabaseTestCase { }
	}

	public class DataContextTestCase<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,T17,T18,T19>
		where T1  : DatabaseTestCase, new()
		where T2  : DatabaseTestCase, new()
		where T3  : DatabaseTestCase, new()
		where T4  : DatabaseTestCase, new()
		where T5  : DatabaseTestCase, new()
		where T6  : DatabaseTestCase, new()
		where T7  : DatabaseTestCase, new()
		where T8  : DatabaseTestCase, new()
		where T9  : DatabaseTestCase, new()
		where T10 : DatabaseTestCase, new()
		where T11 : DatabaseTestCase, new()
		where T12 : DatabaseTestCase, new()
		where T13 : DatabaseTestCase, new()
		where T14 : DatabaseTestCase, new()
		where T15 : DatabaseTestCase, new()
		where T16 : DatabaseTestCase, new()
		where T17 : DatabaseTestCase, new()
		where T18 : DatabaseTestCase, new()
		where T19 : DatabaseTestCase, new()
	{
		static readonly TestCaseData[] _cases = GetCases().ToArray();

		private static IEnumerable<TestCaseData> GetCases()
		{
			var providerNames =
			(
				from t in new DatabaseTestCase[]
				{
					new T1(),  new T2(),  new T3(),  new T4(),  new T5(),  new T6(),  new T7(),  new T8(),  new T9(), new T10(),
					new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17(), new T18(), new T19()
				}
				where t.ProviderName != null
				select t.ProviderName
			).ToArray();

			var cases = providerNames.Select(name => new { name, isLinqService = false }).ToArray();

			if (typeof(T1) == typeof(DatabaseTestCase.LinqService))
				cases = cases.Concat(providerNames.Select(name => new { name, isLinqService = true })).ToArray();

			foreach (var c in cases)
			{
				var data  = new TestCaseData(c.isLinqService ? c.name + ".LinqService" : c.name).SetCategory(c.name);

				if (c.isLinqService)
				{
					data = data
						//.SetName   (c.name + " Linq Service")
						.SetCategory("Linq Service");
				}
				else
				{
					//data = data.SetName(c.name);
				}
				
				yield return data;
			}
		}

		public static IEnumerable TestCases
		{
			get { return _cases; }
		}
	}
}
