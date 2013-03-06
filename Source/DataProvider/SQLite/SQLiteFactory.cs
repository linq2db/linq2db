using System;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using IOPath = System.IO.Path;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	public class SQLiteFactory: IDataProviderFactory
	{
		static readonly SQLiteDataProvider _SQLiteDataProvider = new SQLiteDataProvider();

		static SQLiteFactory()
		{
			DataConnection.AddDataProvider(_SQLiteDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _SQLiteDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _SQLiteDataProvider;
		}

		#region AssemblyResolver

		class AssemblyResolver
		{
			public string Path;

			public Assembly Resolver(object sender, ResolveEventArgs args)
			{
				if (args.Name == "System.Data.SQLite")
					return Assembly.LoadFile(File.Exists(Path) ? Path : IOPath.Combine(Path, args.Name, ".dll"));
				return null;
			}
		}

		public static void ResolveSQLitePath([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			if (path.StartsWith("file:///"))
				path = path.Substring("file:///".Length);

			ResolveEventHandler resolver = new AssemblyResolver { Path = path }.Resolver;

#if FW4

			var l = Expression.Lambda<Action>(Expression.Call(
				Expression.Constant(AppDomain.CurrentDomain),
				typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod(),
				Expression.Constant(resolver)));

			l.Compile()();
#else
			AppDomain.CurrentDomain.AssemblyResolve += resolver;
#endif
		}

		#endregion

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_SQLiteDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_SQLiteDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_SQLiteDataProvider, transaction);
		}

		#endregion
	}
}
