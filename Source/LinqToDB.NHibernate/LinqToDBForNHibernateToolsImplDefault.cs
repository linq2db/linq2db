using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using JetBrains.Annotations;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;
using NHibernate;
using NHibernate.Engine;

namespace LinqToDB.NHibernate
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	/// <summary>
	/// Default NHibernate to LINQ To DB integration bridge implementation.
	/// </summary>
	[PublicAPI]
	public class LinqToDBForNHibernateToolsImplDefault : ILinqToDBForNHibernateTools
	{
		sealed record ProviderKey(string? ProviderName, string? ConnectionString);

		readonly ConcurrentDictionary<ProviderKey, IDataProvider> _knownProviders = new ConcurrentDictionary<ProviderKey, IDataProvider>();

		private readonly MemoryCache _schemaCache = new MemoryCache(
			new MemoryCacheOptions
			{
				ExpirationScanFrequency = TimeSpan.FromHours(1.0),
			});

		/// <summary>
		/// Force clear of internal caches.
		/// </summary>
		public virtual void ClearCaches()
		{
			_knownProviders.Clear();
			_schemaCache.Compact(1.0);
		}

		/// <summary>
		/// Returns LINQ To DB provider, based on provider data from NHibernate.
		/// Could be overriden if you have issues with default detection mechanisms.
		/// </summary>
		/// <param name="providerInfo">Provider information, extracted from NHibernate.</param>
		/// <param name="connectionInfo"></param>
		/// <returns>LINQ TO DB provider instance.</returns>
		public virtual IDataProvider GetDataProvider(NHProviderInfo providerInfo, NHConnectionInfo connectionInfo)
		{
			var info = GetLinqToDbProviderInfo(providerInfo);

			return _knownProviders.GetOrAdd(new ProviderKey(info.ProviderName, connectionInfo.ConnectionString), k =>
			{
				return CreateLinqToDbDataProvider(providerInfo, info, connectionInfo);
			});
		}

		/// <summary>
		/// Converts NHibernate provider settings to linq2db provider settings.
		/// </summary>
		/// <param name="providerInfo">NHibernate provider settings.</param>
		/// <returns>linq2db provider settings.</returns>
		protected virtual LinqToDBProviderInfo GetLinqToDbProviderInfo(NHProviderInfo providerInfo)
		{
			var provInfo = new LinqToDBProviderInfo();

			// Primary, most robust signal: the runtime type of the live ADO connection.
			// This covers both the SQLite and SQL Server test paths without driver-name guessing.
			if (providerInfo.Connection is DbConnection dbConnection)
			{
				provInfo.Merge(GetLinqToDbProviderInfo(dbConnection));
			}

			// Secondary: NHibernate driver detected from the session factory.
			if (providerInfo.Options != null)
			{
				provInfo.Merge(GetLinqToDbProviderInfo(providerInfo.Options));
			}

			return provInfo;
		}

		/// <summary>
		/// Creates instance of linq2db database provider.
		/// </summary>
		/// <param name="providerInfo">NHibernate provider settings.</param>
		/// <param name="provInfo">linq2db provider settings.</param>
		/// <param name="connectionInfo">NHibernate connection settings.</param>
		/// <returns>linq2db database provider.</returns>
		protected virtual IDataProvider CreateLinqToDbDataProvider(NHProviderInfo providerInfo, LinqToDBProviderInfo provInfo,
			NHConnectionInfo connectionInfo)
		{
			if (provInfo.ProviderName == null)
			{
				throw new LinqToDBForNHibernateToolsException("Can not detect data provider.");
			}

			return provInfo.ProviderName switch
			{
				ProviderName.SqlServer     => CreateSqlServerProvider(SqlServerDefaultVersion, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.MySql         => MySqlTools.GetDataProvider(MySqlVersion.AutoDetect, MySqlProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.PostgreSQL    => CreatePostgreSqlProvider(PostgreSqlDefaultVersion, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.SQLite        => SQLiteTools.GetDataProvider(SQLiteProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.SQLiteClassic => SQLiteTools.GetDataProvider(SQLiteProvider.System, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.SQLiteMS      => SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.Firebird      => FirebirdTools.GetDataProvider(FirebirdVersion.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.DB2           => DB2Tools.GetDataProvider(DB2Version.LUW, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.DB2LUW        => DB2Tools.GetDataProvider(DB2Version.LUW, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.DB2zOS        => DB2Tools.GetDataProvider(DB2Version.zOS, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.Oracle        => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection as DbConnection, connectionInfo.Transaction),
				ProviderName.SqlCe         => SqlCeTools.GetDataProvider(),
				//ProviderName.Access      => new AccessDataProvider(),
				_                          => throw new LinqToDBForNHibernateToolsException($"Can not instantiate data provider '{provInfo.ProviderName}'."),
			};
		}

		protected virtual LinqToDBProviderInfo? GetLinqToDbProviderInfo(ISessionFactory sessionFactory) 
		{
			if (sessionFactory is ISessionFactoryImplementor implementor)
			{
				var driverName = implementor.ConnectionProvider.Driver.GetType().Name;

				switch (driverName)
				{
					case "SqlClientDriver":
					case "MicrosoftDataSqlClientDriver":
						return new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer };
					case "SQLite20Driver":
						return new LinqToDBProviderInfo { ProviderName = ProviderName.SQLiteClassic };
				}
			}

			return null;
		}

		/// <summary>
		/// Creates linq2db provider settings object from <see cref="DbConnection"/> instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <returns>linq2db provider settings.</returns>
		protected virtual LinqToDBProviderInfo? GetLinqToDbProviderInfo(DbConnection connection)
		{
			return connection.GetType().Name switch
			{
				"SqlConnection"                         => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer },
				"MySqlConnection"                       => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql },
				"NpgsqlConnection" or "PgSqlConnection" => new LinqToDBProviderInfo { ProviderName = ProviderName.PostgreSQL },
				"FbConnection"                          => new LinqToDBProviderInfo { ProviderName = ProviderName.Firebird },
				"DB2Connection"                         => new LinqToDBProviderInfo { ProviderName = ProviderName.DB2LUW },
				"OracleConnection"                      => new LinqToDBProviderInfo { ProviderName = ProviderName.Oracle },
				// System.Data.SQLite (NHibernate SQLite20Driver).
				"SQLiteConnection"                      => new LinqToDBProviderInfo { ProviderName = ProviderName.SQLiteClassic },
				// Microsoft.Data.Sqlite.
				"SqliteConnection"                      => new LinqToDBProviderInfo { ProviderName = ProviderName.SQLiteMS },
				"JetConnection"                         => new LinqToDBProviderInfo { ProviderName = ProviderName.Access },
				_                                       => null,
			};
		}

		/// <summary>
		/// Creates linq2db SQL Server database provider instance.
		/// </summary>
		/// <param name="version">SQL Server dialect.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>linq2db SQL Server provider instance.</returns>
		protected virtual IDataProvider CreateSqlServerProvider(SqlServerVersion version, string? connectionString, DbConnection? connection, DbTransaction? transaction)
		{
			return SqlServerTools.GetDataProvider(version, SqlServerProvider.MicrosoftDataSqlClient, connectionString, connection, transaction);
		}

		/// <summary>
		/// Creates linq2db PostgreSQL database provider instance.
		/// </summary>
		/// <param name="version">PostgreSQL dialect.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>linq2db PostgreSQL provider instance.</returns>
		protected virtual IDataProvider CreatePostgreSqlProvider(PostgreSQLVersion version, string? connectionString, DbConnection? connection, DbTransaction? transaction)
		{
			return PostgreSQLTools.GetDataProvider(version, connectionString, connection, transaction);
		}

		/// <summary>
		/// Creates metadata provider for specified NHibernate data model. Default implementation uses
		/// <see cref="NHMetadataReader"/> metadata provider.
		/// </summary>
		/// <returns>LINQ To DB metadata provider for specified NHibernate model.</returns>
		public virtual IMetadataReader CreateMetadataReader(ISessionFactory? sessionFactory)
		{
			return new NHMetadataReader(sessionFactory);
		}

		/// <summary>
		/// Creates mapping schema using provided NHibernate data model and metadata provider.
		/// </summary>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <returns>Mapping schema for provided NHibernate model.</returns>
		public virtual MappingSchema CreateMappingSchema(
			ISessionFactory? sessionFactory,
			MappingSchema[]? mappingSchemas,
			IMetadataReader? metadataReader)
		{
			var schema = mappingSchemas != null ? new MappingSchema(mappingSchemas) : new MappingSchema();
			if (metadataReader != null)
				schema.AddMetadataReader(metadataReader);

			return schema;
		}

		/// <summary>
		/// Returns mapping schema using provided NHibernate data model and metadata provider.
		/// </summary>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <returns>Mapping schema for provided NHibernate model.</returns>
		public virtual MappingSchema GetMappingSchema(
			ISessionFactory? sessionFactory,
			IMetadataReader? metadataReader)
		{
			var schemaId = string.Empty;
			MappingSchema[]? schemas = null;
			Tuple<string, MappingSchema[]>? value;
			if (_mappingSchemas.TryGetValue(Tuple.Create<ISessionFactory?>(null), out value))
			{
				schemaId = value.Item1;
				schemas = value.Item2;
			}

			if (sessionFactory != null)
			{
				if (_mappingSchemas.TryGetValue(Tuple.Create<ISessionFactory?>(sessionFactory), out value))
				{
					if (string.IsNullOrEmpty(schemaId))
						schemaId = value.Item1;
					else
						schemaId = schemaId + "-" + value.Item1;

					if (schemas == null)
						schemas = value.Item2;
					else
						schemas = schemas.Concat(value.Item2).ToArray();
				}
			}

			var result = _schemaCache.GetOrCreate(
				Tuple.Create(
					sessionFactory,
					schemaId,
					metadataReader,
					EnableChangeTracker
				),
				e =>
				{
					e.SlidingExpiration = TimeSpan.FromHours(1);
					return CreateMappingSchema(sessionFactory, schemas, metadataReader);
				});

			return result!;
		}

		/// <summary>
		/// Returns the NHibernate <see cref="ISessionFactory"/> for a specific <see cref="ISession"/> instance.
		/// </summary>
		/// <param name="session"></param>
		/// <returns><see cref="ISessionFactory"/> instance.</returns>
		public virtual ISessionFactory? GetSessionOptions(ISession? session)
		{
			return session?.SessionFactory;
		}

		/// <summary>
		/// Removes conversions from expression.
		/// </summary>
		/// <param name="ex">Expression.</param>
		/// <returns>Unwrapped expression.</returns>
		public static Expression? Unwrap(Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote          : return Unwrap(((UnaryExpression)ex).Operand);
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
					{
						var ue = (UnaryExpression)ex;

						if (!ue.Operand.Type.IsEnum)
							return Unwrap(ue.Operand);

						break;
					}
			}

			return ex;
		}

		/// <summary>
		/// Tests that method is <see cref="IQueryable{T}"/> extension.
		/// </summary>
		/// <param name="method">Method to test.</param>
		/// <param name="enumerable">Allow <see cref="IEnumerable{T}"/> extensions.</param>
		/// <returns><see langword="true"/> if method is <see cref="IQueryable{T}"/> extension.</returns>
		public static bool IsQueryable(MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(Queryable) || (enumerable && type == typeof(Enumerable)) || type == typeof(LinqExtensions)/* ||
				   type == typeof()*/;
		}

		/// <summary>
		/// Evaluates value of expression.
		/// </summary>
		/// <param name="expr">Expression to evaluate.</param>
		/// <returns>Expression value.</returns>
		public static object? EvaluateExpression(Expression? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.MemberAccess:
					{
						var member = (MemberExpression) expr;

						if (member.Member is FieldInfo)
							return ((FieldInfo)member.Member).GetValue(EvaluateExpression(member.Expression));

						if (member.Member is PropertyInfo)
							return ((PropertyInfo)member.Member).GetValue(EvaluateExpression(member.Expression), null);

						break;
					}
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvokeExt();
			return value;
		}

		/// <summary>
		/// Compacts expression to handle big filters.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Compacted expression.</returns>
		public static Expression CompactExpression(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Or:
				case ExpressionType.And:
				case ExpressionType.OrElse:
				case ExpressionType.AndAlso:
				{
					var stack = new Stack<Expression>();
					var items = new List<Expression>();
					var binary = (BinaryExpression) expression;

					stack.Push(binary.Right);
					stack.Push(binary.Left);
					while (stack.Count > 0)
					{
						var item = stack.Pop();
						if (item.NodeType == expression.NodeType)
						{
							binary = (BinaryExpression) item;
							stack.Push(binary.Right);
							stack.Push(binary.Left);
						}
						else
							items.Add(item);
					}

					if (items.Count > 3)
					{
						// having N items will lead to NxM recursive calls in expression visitors and
						// will result in stack overflow on relatively small numbers (~1000 items).
						// To fix it we will rebalance condition tree here which will result in
						// LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
						//
						// E.g. we have condition A OR B OR C OR D OR E
						// as an expression tree it represented as tree with depth 5
						//   OR
						// A    OR
						//    B    OR
						//       C    OR
						//          D    E
						// for rebalanced tree it will have depth 4
						//                  OR
						//        OR
						//   OR        OR        OR
						// A    B    C    D    E    F
						// Not much on small numbers, but huge improvement on bigger numbers
						while (items.Count != 1)
						{
							items = CompactTree(items, expression.NodeType);
						}

						return items[0];
					}

					break;
				}
			}

			return expression;
		}

		static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
		{
			var result = new List<Expression>();

			// traverse list from left to right to preserve calculation order
			for (var i = 0; i < items.Count; i += 2)
			{
				if (i + 1 == items.Count)
				{
					// last non-paired item
					result.Add(items[i]);
				}
				else
				{
					result.Add(Expression.MakeBinary(nodeType, items[i], items[i + 1]));
				}
			}

			return result;
		}

		/// <summary>
		/// Transforms NHibernate expression tree to LINQ To DB expression.
		/// Method replaces native NHibernate <c>NhQueryable&lt;T&gt;</c> instances with LINQ To DB
		/// <see cref="DataExtensions.GetTable{T}(IDataContext)"/> calls.
		/// </summary>
		/// <param name="expression">NHibernate expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <returns>Transformed expression.</returns>
		public virtual Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory)
		{
			// Tracking is on by default; an AsReadOnly() marker anywhere in the query turns it off, mirroring
			// how the EF Core integration treats AsNoTracking().
			var tracking = true;

			TransformInfo LocalTransform(Expression e)
			{
				e = CompactExpression(e);

				switch (e.NodeType)
				{
					case ExpressionType.Constant:
					{
						// Replace a native NHibernate queryable root (NhQueryable<T>) with a linq2db
						// GetTable<T> call on the current context. Keeping the whole query on a single
						// data context means no second implicit context/connection is created for the
						// source, which is what breaks the connection/transaction lifecycle otherwise.
						if (e is ConstantExpression { Value: IQueryable queryable } && queryable.Provider is not IQueryProviderAsync)
						{
							var entityType = queryable.ElementType;
							var newExpr    = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType), SqlQueryRootExpression.Create(dc));
							return new TransformInfo(newExpr);
						}

							break;
					}

					case ExpressionType.Call:
					{
						// Detect the AsReadOnly() marker (the NHibernate analogue of EF Core's AsNoTracking()):
						// strip it from the query and leave the entities it materialises detached (untracked).
						if (e is MethodCallExpression { Method.IsGenericMethod: true } call
							&& call.Method.GetGenericMethodDefinition() == LinqToDBForNHibernateTools.AsReadOnlyMethodInfo)
						{
							tracking = false;
							return new TransformInfo(call.Arguments[0], false, true);
						}

						break;
					}

					case ExpressionType.MemberAccess:
					{
						if (typeof(IQueryable<>).IsSameOrParentOf(e.Type))
						{
							var ma    = (MemberExpression)e;
							var query = (IQueryable)EvaluateExpression(ma)!;

							return new TransformInfo(query.Expression, false, true);
						}

						break;
					}

				}

				return new TransformInfo(e);
			}

			var newExpression = expression.Transform(LocalTransform);

			if (dc is LinqToDBForNHibernateToolsDataConnection dataConnection)
				dataConnection.Tracking = tracking;

			return newExpression;
		}

		/// <summary>
		/// Extracts the <see cref="ISession"/> from a native NHibernate query.
		/// Due to unavailability of a public integration API this method uses reflection and could break across NHibernate versions.
		/// </summary>
		/// <param name="query">NHibernate query.</param>
		/// <returns>Current <see cref="ISession"/> instance.</returns>
		public virtual ISession? GetCurrentContext(IQueryable query)
		{
			var provider = query.Provider;

			// A native NHibernate query (NhQueryable<T>) carries its session on the query provider
			// (DefaultQueryProvider.Session — an ISessionImplementor that the concrete session also
			// implements as ISession). This member is not part of NHibernate's public integration
			// surface, so it is read by reflection and may need revisiting across NHibernate versions.
			var sessionProperty = provider.GetType().GetProperty(
				"Session",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			return sessionProperty?.GetValue(provider) as ISession;
		}

		/// <summary>
		/// Gets or sets default provider version for SQL Server. Set to <see cref="SqlServerVersion.v2008"/> dialect.
		/// </summary>
		public static SqlServerVersion SqlServerDefaultVersion { get; set; } = SqlServerVersion.v2008;

		/// <summary>
		/// Gets or sets default provider version for PostgreSQL Server. Set to <see cref="PostgreSQLVersion.v93"/> dialect.
		/// </summary>
		public static PostgreSQLVersion PostgreSqlDefaultVersion { get; set; } = PostgreSQLVersion.v93;

		/// <summary>
		/// Enables attaching entities materialised by a linq2db query to the NHibernate session's change tracker,
		/// so that subsequent modifications are persisted when the session is flushed.
		/// </summary>
		public virtual bool EnableChangeTracker { get; set; } = true;

		#region MappingSchemaSupport

		readonly ConcurrentDictionary<Tuple<ISessionFactory?>, Tuple<string, MappingSchema[]>> _mappingSchemas = new();

		// 6.x: MappingSchema.ConfigurationID is an explicit IConfigurationID member returning int
		// (not the reflectable non-public property the 2021 code assumed).
		static readonly Func<MappingSchema, string> _configurationIdGetter =
			ms => ((IConfigurationID)ms).ConfigurationID.ToString(System.Globalization.CultureInfo.InvariantCulture);

		public void AddMappingSchema(ISessionFactory? sessionFactory, MappingSchema mappingSchema)
		{
			_mappingSchemas.AddOrUpdate(Tuple.Create(sessionFactory), sf =>
				{
					return Tuple.Create(_configurationIdGetter(mappingSchema), new[] {mappingSchema});
				},
				(sf, tuple) =>
				{
					return Tuple.Create(tuple.Item1 + ";" + _configurationIdGetter(mappingSchema),
						tuple.Item2.Concat(new[] {mappingSchema}).ToArray());
				});
		}

		#endregion

	}
}
