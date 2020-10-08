using System;
using System.Collections.Generic;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Identity;

namespace Tests.Identity
{
	public class TestConnectionFactory : IConnectionFactory
	{
		private static readonly Dictionary<string, HashSet<string>> _tables = new Dictionary<string, HashSet<string>>();
		private readonly string _configuration;
		private readonly string _connectionString;
		private readonly string _key;
		private readonly IDataProvider _provider;

		public TestConnectionFactory(IDataProvider provider, string configuration, string connectionString)
		{
			_provider = provider;
			Configuration.Linq.AllowMultipleQuery = true;
			_configuration = configuration;
			_connectionString = connectionString;
			_key = _configuration + "$$" + _connectionString;
		}

		public IDataContext GetContext()
		{
			return new DataContext(_provider, _connectionString);
		}

		public DataConnection GetConnection()
		{
			return new DataConnection(_provider, _connectionString);
		}

		public void CreateTable<T>()
		{
			var dc = GetContext();
			var e = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			HashSet<string> set;

			lock (_tables)
			{
				if (!_tables.TryGetValue(_key, out set))
				{
					set = new HashSet<string>();
					_tables.Add(_key, set);
				}

				if (set.Contains(e.TableName))
					return;

				set.Add(e.TableName);
				dc.CreateTable<T>();
			}
		}

		public void CreateTables<TUser, TRole, TKey>()
			where TUser : class, IIdentityUser<TKey>
			where TRole : class, IIdentityRole<TKey>
			where TKey : IEquatable<TKey>
		{
			lock (_tables)
			{
				CreateTable<TUser>();
				CreateTable<TRole>();
				CreateTable<IdentityUserLogin<TKey>>();
				CreateTable<IdentityUserRole<TKey>>();
				CreateTable<IdentityRoleClaim<TKey>>();
				CreateTable<IdentityUserClaim<TKey>>();
				CreateTable<IdentityUserToken<TKey>>();
			}
		}

		public void DropTable<T>()
		{
			var dc = GetContext();
			var e = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			HashSet<string> set;

			lock (_tables)
			{
				if (!_tables.TryGetValue(_key, out set))
					return;

				if (!set.Contains(e.TableName))
					return;

				set.Remove(e.TableName);

				dc.DropTable<T>();
			}
		}
	}

}
