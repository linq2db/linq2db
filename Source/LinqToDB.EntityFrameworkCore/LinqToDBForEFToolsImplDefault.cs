using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LinqToDB.EntityFrameworkCore
{
	using Common.Internal.Cache;
	using Data;
	using DataProvider;
	using DataProvider.DB2;
	using DataProvider.Firebird;
	using DataProvider.MySql;
	using DataProvider.Oracle;
	using DataProvider.PostgreSQL;
	using DataProvider.SqlCe;
	using DataProvider.SQLite;
	using DataProvider.SqlServer;
	using Expressions;
	using Extensions;
	using Mapping;
	using Metadata;
	using Reflection;
	using SqlQuery;

	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	/// <summary>
	/// Default EF Core - LINQ To DB integration bridge implementation.
	/// </summary>
	[PublicAPI]
	public class LinqToDBForEFToolsImplDefault : ILinqToDBForEFTools
	{
		private static readonly char[] _nameSeparator = ['.'];

		sealed class ProviderKey
		{
			public ProviderKey(string? providerName, string? connectionString)
			{
				ProviderName     = providerName;
				ConnectionString = connectionString;
			}

			string? ProviderName     { get; }
			string? ConnectionString { get; }

			#region Equality members

			private bool Equals(ProviderKey other)
			{
				return string.Equals(ProviderName, other.ProviderName) && string.Equals(ConnectionString, other.ConnectionString);
			}

			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((ProviderKey) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((ProviderName != null ? ProviderName.GetHashCode() : 0) * 397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
				}
			}
			
			#endregion
		}

		readonly ConcurrentDictionary<ProviderKey, IDataProvider> _knownProviders = new();

		private readonly MemoryCache _schemaCache = new(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()
			{
				ExpirationScanFrequency = TimeSpan.FromHours(1.0)
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
		/// Returns LINQ To DB provider, based on provider data from EF Core.
		/// Could be overridden if you have issues with default detection mechanisms.
		/// </summary>
		/// <param name="options">Linq To DB context options.</param>
		/// <param name="providerInfo">Provider information, extracted from EF Core.</param>
		/// <param name="connectionInfo"></param>
		/// <returns>LINQ TO DB provider instance.</returns>
		public virtual IDataProvider GetDataProvider(DataOptions options, EFProviderInfo providerInfo, EFConnectionInfo connectionInfo)
		{
			if (options.ConnectionOptions.DataProvider != null)
				return options.ConnectionOptions.DataProvider;

			LinqToDBProviderInfo info;
			if (options.ConnectionOptions.ProviderName != null)
				info = new LinqToDBProviderInfo() { ProviderName = options.ConnectionOptions.ProviderName };
			else
				info = GetLinqToDBProviderInfo(providerInfo);

			return _knownProviders.GetOrAdd(new ProviderKey(info.ProviderName, connectionInfo.ConnectionString), k =>
			{
				return CreateLinqToDBDataProvider(providerInfo, info, connectionInfo);
			});
		}

		/// <summary>
		/// Converts EF Core provider settings to Linq To DB provider settings.
		/// </summary>
		/// <param name="providerInfo">EF Core provider settings.</param>
		/// <returns>Linq To DB provider settings.</returns>
		protected virtual LinqToDBProviderInfo GetLinqToDBProviderInfo(EFProviderInfo providerInfo)
		{
			var provInfo = new LinqToDBProviderInfo();

			var relational = providerInfo.Options?.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();
			if (relational != null)
			{
				provInfo.Merge(GetLinqToDBProviderInfo(relational));
			}

			if (providerInfo.Connection != null)
			{
				provInfo.Merge(GetLinqToDBProviderInfo(providerInfo.Connection));
			}

			if (providerInfo.Context != null)
			{
				provInfo.Merge(GetLinqToDBProviderInfo(providerInfo.Context.Database));
			}

			return provInfo;
		}

		/// <summary>
		/// Creates instance of Linq To DB database provider.
		/// </summary>
		/// <param name="providerInfo">EF Core provider settings.</param>
		/// <param name="provInfo">Linq To DB provider settings.</param>
		/// <param name="connectionInfo">EF Core connection settings.</param>
		/// <returns>Linq To DB database provider.</returns>
		protected virtual IDataProvider CreateLinqToDBDataProvider(EFProviderInfo providerInfo, LinqToDBProviderInfo provInfo,
			EFConnectionInfo connectionInfo)
		{
			if (provInfo.ProviderName == null)
			{
				throw new LinqToDBForEFToolsException("Can not detect data provider.");
			}

			return provInfo.ProviderName switch
			{
				ProviderName.SqlServer                                                    => CreateSqlServerProvider(SqlServerDefaultVersion, connectionInfo.ConnectionString),
				ProviderName.SqlServer2005                                                => CreateSqlServerProvider(SqlServerVersion.v2005, connectionInfo.ConnectionString),
				ProviderName.SqlServer2008                                                => CreateSqlServerProvider(SqlServerVersion.v2008, connectionInfo.ConnectionString),
				ProviderName.SqlServer2012                                                => CreateSqlServerProvider(SqlServerVersion.v2012, connectionInfo.ConnectionString),
				ProviderName.SqlServer2014                                                => CreateSqlServerProvider(SqlServerVersion.v2014, connectionInfo.ConnectionString),
				ProviderName.SqlServer2016                                                => CreateSqlServerProvider(SqlServerVersion.v2016, connectionInfo.ConnectionString),
				ProviderName.SqlServer2017                                                => CreateSqlServerProvider(SqlServerVersion.v2017, connectionInfo.ConnectionString),
				ProviderName.SqlServer2019                                                => CreateSqlServerProvider(SqlServerVersion.v2019, connectionInfo.ConnectionString),
				ProviderName.SqlServer2022                                                => CreateSqlServerProvider(SqlServerVersion.v2022, connectionInfo.ConnectionString),

				ProviderName.MySql                                                        => MySqlTools.GetDataProvider(MySqlVersion.AutoDetect, MySqlProvider.AutoDetect, connectionInfo.ConnectionString),
				ProviderName.MySql57                                                      => MySqlTools.GetDataProvider(MySqlVersion.MySql57, MySqlProvider.AutoDetect, connectionInfo.ConnectionString),
				ProviderName.MySql80                                                      => MySqlTools.GetDataProvider(MySqlVersion.MySql80, MySqlProvider.AutoDetect, connectionInfo.ConnectionString),
				ProviderName.MariaDB10                                                    => MySqlTools.GetDataProvider(MySqlVersion.MariaDB10, MySqlProvider.MySqlConnector, connectionInfo.ConnectionString),

				ProviderName.PostgreSQL                                                   => CreatePostgreSqlProvider(PostgreSqlDefaultVersion, connectionInfo.ConnectionString),
				ProviderName.PostgreSQL92                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v92, connectionInfo.ConnectionString),
				ProviderName.PostgreSQL93                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v93, connectionInfo.ConnectionString),
				ProviderName.PostgreSQL95                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v95, connectionInfo.ConnectionString),
				ProviderName.PostgreSQL15                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v15, connectionInfo.ConnectionString),

				ProviderName.SQLite or ProviderName.SQLiteMS                              => SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft, connectionInfo.ConnectionString),

				ProviderName.Firebird                                                     => FirebirdTools.GetDataProvider(FirebirdVersion.AutoDetect, connectionInfo.ConnectionString),
				ProviderName.Firebird25                                                   => FirebirdTools.GetDataProvider(FirebirdVersion.v25, connectionInfo.ConnectionString),
				ProviderName.Firebird3                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v3, connectionInfo.ConnectionString),
				ProviderName.Firebird4                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v4, connectionInfo.ConnectionString),
				ProviderName.Firebird5                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v5, connectionInfo.ConnectionString),

				ProviderName.DB2 or ProviderName.DB2LUW                                   => DB2Tools.GetDataProvider(DB2Version.LUW, connectionInfo.ConnectionString),
				ProviderName.DB2zOS                                                       => DB2Tools.GetDataProvider(DB2Version.zOS, connectionInfo.ConnectionString),

				ProviderName.Oracle                                                       => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.AutoDetect, connectionInfo.ConnectionString),
				ProviderName.Oracle11Native                                               => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Native    , connectionInfo.ConnectionString),
				ProviderName.OracleNative                                                 => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Native    , connectionInfo.ConnectionString),
				ProviderName.Oracle11Managed                                              => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Managed   , connectionInfo.ConnectionString),
				ProviderName.OracleManaged                                                => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Managed   , connectionInfo.ConnectionString),
				ProviderName.Oracle11Devart                                               => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Devart    , connectionInfo.ConnectionString),
				ProviderName.OracleDevart                                                 => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Devart    , connectionInfo.ConnectionString),

				ProviderName.SqlCe                                                        => SqlCeTools.GetDataProvider(),

				// TODO: missing: informix, access, sap hana, sybase, clickhouse
				_                                                                         => throw new LinqToDBForEFToolsException($"Can not instantiate data provider '{provInfo.ProviderName}'."),
			};
		}

		/// <summary>
		/// Creates Linq To DB provider settings object from <see cref="DatabaseFacade"/> instance.
		/// </summary>
		/// <param name="database">EF Core database information object.</param>
		/// <returns>Linq To DB provider settings.</returns>
		protected virtual LinqToDBProviderInfo? GetLinqToDBProviderInfo(DatabaseFacade database)
		{
			return database.ProviderName switch
			{
				"Microsoft.EntityFrameworkCore.SqlServer"                                                   => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer      },
				"Pomelo.EntityFrameworkCore.MySql" or "Devart.Data.MySql.EFCore"                            => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql          },
				"MySql.Data.EntityFrameworkCore"                                                            => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql          },
				"Npgsql.EntityFrameworkCore.PostgreSQL" or "Devart.Data.PostgreSql.EFCore"                  => new LinqToDBProviderInfo { ProviderName = ProviderName.PostgreSQL     },
				"Microsoft.EntityFrameworkCore.Sqlite" or "Devart.Data.SQLite.EFCore"                       => new LinqToDBProviderInfo { ProviderName = ProviderName.SQLite         },
				"FirebirdSql.EntityFrameworkCore.Firebird" or "EntityFrameworkCore.FirebirdSql"             => new LinqToDBProviderInfo { ProviderName = ProviderName.Firebird       },
				"IBM.EntityFrameworkCore" or "IBM.EntityFrameworkCore-lnx" or "IBM.EntityFrameworkCore-osx" => new LinqToDBProviderInfo { ProviderName = ProviderName.DB2LUW         },
				"Devart.Data.Oracle.EFCore"                                                                 => new LinqToDBProviderInfo { ProviderName = ProviderName.Oracle         },
				"EntityFrameworkCore.Jet"                                                                   => new LinqToDBProviderInfo { ProviderName = ProviderName.Access         },
				"EntityFrameworkCore.SqlServerCompact40" or "EntityFrameworkCore.SqlServerCompact35"        => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlCe          },
				_                                                                                           => null,
			};
		}

		/// <summary>
		/// Creates Linq To DB provider settings object from <see cref="DbConnection"/> instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <returns>Linq To DB provider settings.</returns>
		protected virtual LinqToDBProviderInfo? GetLinqToDBProviderInfo(DbConnection connection)
		{
			return connection.GetType().Name switch
			{
				"SqlConnection"                          => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer  },
				"MySqlConnection"                        => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql      },
				"NpgsqlConnection" or "PgSqlConnection"  => new LinqToDBProviderInfo { ProviderName = ProviderName.PostgreSQL },
				"FbConnection"                           => new LinqToDBProviderInfo { ProviderName = ProviderName.Firebird   },
				"DB2Connection"                          => new LinqToDBProviderInfo { ProviderName = ProviderName.DB2LUW     },
				"OracleConnection"                       => new LinqToDBProviderInfo { ProviderName = ProviderName.Oracle     },
				"SqliteConnection" or "SQLiteConnection" => new LinqToDBProviderInfo { ProviderName = ProviderName.SQLite     },
				"JetConnection"                          => new LinqToDBProviderInfo { ProviderName = ProviderName.Access     },
				_                                        => null,
			};
		}

		/// <summary>
		/// Creates Linq To DB provider settings object from <see cref="RelationalOptionsExtension"/> instance.
		/// </summary>
		/// <param name="extensions">EF Core provider options.</param>
		/// <returns>Linq To DB provider settings.</returns>
		protected virtual LinqToDBProviderInfo? GetLinqToDBProviderInfo(RelationalOptionsExtension extensions)
		{
			return extensions.GetType().Name switch
			{
				"MySqlOptionsExtension"                              => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql          },
				"MySQLOptionsExtension"                              => new LinqToDBProviderInfo { ProviderName = ProviderName.MySql          },
				"NpgsqlOptionsExtension" or "PgSqlOptionsExtension"  => new LinqToDBProviderInfo { ProviderName = ProviderName.PostgreSQL     },
				"SqlServerOptionsExtension"                          => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlServer      },
				"SqliteOptionsExtension" or "SQLiteOptionsExtension" => new LinqToDBProviderInfo { ProviderName = ProviderName.SQLite         },
				"SqlCeOptionsExtension"                              => new LinqToDBProviderInfo { ProviderName = ProviderName.SqlCe          },
				"FbOptionsExtension"                                 => new LinqToDBProviderInfo { ProviderName = ProviderName.Firebird       },
				"Db2OptionsExtension"                                => new LinqToDBProviderInfo { ProviderName = ProviderName.DB2LUW         },
				"OracleOptionsExtension"                             => new LinqToDBProviderInfo { ProviderName = ProviderName.Oracle         },
				"JetOptionsExtension"                                => new LinqToDBProviderInfo { ProviderName = ProviderName.Access         },
				_                                                    => null,
			};
		}

		/// <summary>
		/// Creates Linq To DB SQL Server database provider instance.
		/// </summary>
		/// <param name="version">SQL Server dialect.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Linq To DB SQL Server provider instance.</returns>
		protected virtual IDataProvider CreateSqlServerProvider(SqlServerVersion version, string? connectionString)
		{
			return DataProvider.SqlServer.SqlServerTools.GetDataProvider(version, SqlServerProvider.MicrosoftDataSqlClient, connectionString);
		}

		/// <summary>
		/// Creates Linq To DB PostgreSQL database provider instance.
		/// </summary>
		/// <param name="version">PostgreSQL dialect.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Linq To DB PostgreSQL provider instance.</returns>
		protected virtual IDataProvider CreatePostgreSqlProvider(PostgreSQLVersion version, string? connectionString)
		{
			return PostgreSQLTools.GetDataProvider(version, connectionString);
		}

		/// <summary>
		/// Creates metadata provider for specified EF Core data model. Default implementation uses
		/// <see cref="EFCoreMetadataReader"/> metadata provider.
		/// </summary>
		/// <param name="model">EF Core data model.</param>
		/// <param name="accessor">EF Core service provider.</param>
		/// <returns>LINQ To DB metadata provider for specified EF Core model.</returns>
		public virtual IMetadataReader CreateMetadataReader(IModel? model, IInfrastructure<IServiceProvider>? accessor)
		{
			return new EFCoreMetadataReader(model, accessor);
		}

		/// <summary>
		/// Creates mapping schema using provided EF Core data model and metadata provider.
		/// </summary>
		/// <param name="model">EF Core data model.</param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <param name="convertorSelector"></param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		public virtual MappingSchema CreateMappingSchema(
			IModel model,
			IMetadataReader? metadataReader,
			IValueConverterSelector? convertorSelector,
			DataOptions dataOptions)
		{
			var schema = new MappingSchema();
			if (metadataReader != null)
				schema.AddMetadataReader(metadataReader);

			DefineConvertors(schema, model, convertorSelector, dataOptions);

			return schema;
		}

		/// <summary>
		/// Import type conversions from EF Core model into Linq To DB mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Linq To DB mapping schema.</param>
		/// <param name="model">EF Core data mode.</param>
		/// <param name="convertorSelector">Type filter.</param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		public virtual void DefineConvertors(
			MappingSchema mappingSchema,
			IModel model,
			IValueConverterSelector? convertorSelector,
			DataOptions dataOptions)
		{
			ArgumentNullException.ThrowIfNull(mappingSchema);
			ArgumentNullException.ThrowIfNull(model);

			if (convertorSelector == null)
				return;

			var entities = model.GetEntityTypes().ToArray();

			var types = entities.SelectMany(e => e.GetProperties().Select(p => p.ClrType))
				.Distinct()
				.ToArray();

			var sqlConverter = mappingSchema.ValueToSqlConverter;
			
			foreach (var modelType in types)
			{
				// skipping enums
				if (modelType.IsEnum)
					continue;

				// skipping arrays
				if (modelType.IsArray)
					continue;

				MapEFCoreType(modelType);
				if (modelType.IsValueType && !typeof(Nullable<>).IsSameOrParentOf(modelType))
					MapEFCoreType(typeof(Nullable<>).MakeGenericType(modelType));
			}

			void MapEFCoreType(Type modelType)
			{
				var currentType = mappingSchema.GetDataType(modelType);
				if (currentType != SqlDataType.Undefined)
					return;

				var infos = convertorSelector.Select(modelType).ToArray();
				if (infos.Length <= 0)
					return;

				var info = infos[0];
				var providerType = info.ProviderClrType;
				var dataType = mappingSchema.GetDataType(providerType);
				var fromParam = Expression.Parameter(modelType, "t");
				var toParam = Expression.Parameter(providerType, "t");
				var converter = info.Create();

				var valueExpression =
					Expression.Invoke(Expression.Constant(converter.ConvertToProvider), WithConvertToObject(fromParam));
				var convertLambda = WithToDataParameter(valueExpression, dataType, fromParam);

				mappingSchema.SetConvertExpression(modelType, typeof(DataParameter), convertLambda, false);
				mappingSchema.SetConvertExpression(modelType, providerType,
					Expression.Lambda(Expression.Convert(valueExpression, providerType), fromParam));
				mappingSchema.SetConvertExpression(providerType, modelType,
					Expression.Lambda(
						Expression.Convert(
							Expression.Invoke(Expression.Constant(converter.ConvertFromProvider), WithConvertToObject(toParam)),
							modelType), toParam));

				mappingSchema.SetValueToSqlConverter(modelType, (sb, dt, v)
					=> sqlConverter.Convert(sb, mappingSchema, dt.Type, dataOptions, converter.ConvertToProvider(v)));
			}
		}

		private static LambdaExpression WithToDataParameter(Expression valueExpression, SqlDataType dataType, ParameterExpression fromParam) 
			=> Expression.Lambda
			(
				Expression.New
				(
					DataParameterConstructor,
					Expression.Constant("Conv", typeof(string)),
					valueExpression,
					Expression.Constant(dataType.Type.DataType, typeof(DataType)),
					Expression.Constant(dataType.Type.DbType, typeof(string))
				), 
				fromParam
			);

		private static Expression WithConvertToObject(Expression valueExpression) 
			=> valueExpression.Type != typeof(object) 
				? Expression.Convert(valueExpression, typeof(object)) 
				: valueExpression;

		/// <summary>
		/// Returns mapping schema using provided EF Core data model and metadata provider.
		/// </summary>
		/// <param name="model">EF Core data model.</param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <param name="convertorSelector"></param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		public virtual MappingSchema GetMappingSchema(
			IModel model,
			IMetadataReader? metadataReader,
			IValueConverterSelector? convertorSelector,
			DataOptions? dataOptions)
		{
			dataOptions ??= new();

			var result = _schemaCache.GetOrCreate(
				(
					dataOptions,
					model,
					metadataReader,
					convertorSelector,
					EnableChangeTracker
				),
				e =>
				{
					e.SlidingExpiration = TimeSpan.FromHours(1);
					return CreateMappingSchema(model, metadataReader, convertorSelector, dataOptions);
				})!;

			return result;
		}

		/// <summary>
		/// Returns EF Core <see cref="IDbContextOptions"/> for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="context">EF Core <see cref="DbContext"/> instance.</param>
		/// <returns><see cref="IDbContextOptions"/> instance.</returns>
		public virtual IDbContextOptions? GetContextOptions(DbContext? context)
		{
			return context?.GetService<IDbContextOptions>();
		}

#if EF31
		static readonly MethodInfo FromSqlOnQueryableMethodInfo = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Single(x => x.Name == "FromSqlOnQueryable").GetGenericMethodDefinition();
#endif

		static readonly MethodInfo IgnoreQueryFiltersMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.IgnoreQueryFilters());
		static readonly MethodInfo IncludeMethodInfo            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.Include(o => o));
		static readonly MethodInfo IncludeMethodInfoString      = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.Include(string.Empty));
		static readonly MethodInfo ThenIncludeMethodInfo        = MemberHelper.MethodOfGeneric<IIncludableQueryable<object, object>>(q => q.ThenInclude<object, object, object>(null!));
		static readonly MethodInfo TagWithMethodInfo            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.TagWith(string.Empty));

#if !EF31
		static readonly MethodInfo AsSplitQueryMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsSplitQuery());

		static readonly MethodInfo AsSingleQueryMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsSingleQuery());
#endif

		static readonly MethodInfo ThenIncludeEnumerableMethodInfo              = MemberHelper.MethodOfGeneric<IIncludableQueryable<object, IEnumerable<object>>>(q => q.ThenInclude<object, object, object>(null!));
		static readonly MethodInfo AsNoTrackingMethodInfo                       = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsNoTracking());
#if !EF31
		static readonly MethodInfo AsNoTrackingWithIdentityResolutionMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsNoTrackingWithIdentityResolution());
#endif

		static readonly MethodInfo EFProperty = MemberHelper.MethodOfGeneric(() => EF.Property<object>(1, ""));

		static readonly MethodInfo L2DBFromSqlMethodInfo = MemberHelper.MethodOfGeneric<IDataContext>(dc => dc.FromSql<object>(new Common.RawSqlString()));

		static readonly ConstructorInfo RawSqlStringConstructor = MemberHelper.ConstructorOf(() => new Common.RawSqlString(""));

		static readonly ConstructorInfo DataParameterConstructor = MemberHelper.ConstructorOf(() => new DataParameter("", "", DataType.Undefined, ""));

		static readonly MethodInfo ToSql = MemberHelper.MethodOfGeneric(() => Sql.ToSql(1));

#if !EF31
		private static readonly MethodInfo AsSqlServerTable    = MemberHelper.MethodOfGeneric<ITable<object>>(q => DataProvider.SqlServer.SqlServerTools.AsSqlServer(q));
		private static readonly MethodInfo TemporalAsOfTable   = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableAsOf(t, default));
		private static readonly MethodInfo TemporalFromTo      = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableFromTo(t, default, default));
		private static readonly MethodInfo TemporalBetween     = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableBetween(t, default, default));
		private static readonly MethodInfo TemporalContainedIn = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableContainedIn(t, default, default));
		private static readonly MethodInfo TemporalAll         = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableAll(t));
#endif

		private static readonly Func<object?, object?> ContextDependenciesGetValueMethod = (typeof(RelationalQueryContextFactory)
#if !EF31
			.GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new LinqToDBForEFToolsException($"Can not find protected property '{nameof(RelationalQueryContextFactory)}.Dependencies' in current EFCore Version.")
#else
			.GetField("_dependencies", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new LinqToDBForEFToolsException($"Can not find private property '{nameof(RelationalQueryContextFactory)}._dependencies' in current EFCore Version.")
#endif
			).GetValue;

		/// <summary>
		/// Removes conversions from expression.
		/// </summary>
		/// <param name="ex">Expression.</param>
		/// <returns>Unwrapped expression.</returns>
		[return: NotNullIfNotNull(nameof(ex))]
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
		/// <returns><c>true</c> if method is <see cref="IQueryable{T}"/> extension.</returns>
		public static bool IsQueryable(MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(Queryable) || (enumerable && type == typeof(Enumerable)) || type == typeof(LinqExtensions) ||
				   type == typeof(DataExtensions) || type == typeof(TableExtensions) ||
				   type == typeof(EntityFrameworkQueryableExtensions);
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

						if (member.Member.IsFieldEx())
							return ((FieldInfo)member.Member).GetValue(EvaluateExpression(member.Expression));

						if (member.Member.IsPropertyEx())
							return ((PropertyInfo)member.Member).GetValue(EvaluateExpression(member.Expression), null);

						break;
					}
			}

			var value = Expression.Lambda(expr).Compile().DynamicInvoke();
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

#if !EF31
		/// <summary>
		/// Gets current property value via reflection.
		/// </summary>
		/// <typeparam name="TValue">Property value type.</typeparam>
		/// <param name="obj">Object instance</param>
		/// <param name="propName">Property name</param>
		/// <returns>Property value.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected static TValue GetPropValue<TValue>(object obj, string propName)
		{
			var prop = obj.GetType().GetProperty(propName)
				?? throw new InvalidOperationException($"Property {obj.GetType().Name}.{propName} not found.");
			var propValue = prop.GetValue(obj);
			if (propValue == default)
				return default!;
			return (TValue)propValue;
		}
#endif

		/// <summary>
		/// Transforms EF Core expression tree to LINQ To DB expression.
		/// Method replaces EF Core <see cref="EntityQueryable{TResult}"/> instances with LINQ To DB
		/// <see cref="DataExtensions.GetTable{T}(IDataContext)"/> calls.
		/// </summary>
		/// <param name="expression">EF Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <param name="ctx">Optional DbContext instance.</param>
		/// <param name="model">EF Core data model instance.</param>
		/// <returns>Transformed expression.</returns>
		public virtual Expression TransformExpression(Expression expression, IDataContext? dc, DbContext? ctx, IModel? model)
		{
			var tracking           = true;
			var ignoreTracking     = false;

			var nonEvaluableParameters = new HashSet<ParameterExpression>();

			TransformInfo LocalTransform(Expression e)
			{
				e = CompactExpression(e);

				switch (e.NodeType)
				{
					case ExpressionType.Lambda:
					{
						foreach (var parameter in ((LambdaExpression)e).Parameters)
						{
							nonEvaluableParameters.Add(parameter);
						}

						break;
					}

					case ExpressionType.Constant:
					{
						if (dc != null && typeof(EntityQueryable<>).IsSameOrParentOf(e.Type) || typeof(DbSet<>).IsSameOrParentOf(e.Type))
						{
							var entityType = e.Type.GenericTypeArguments[0];
							var newExpr = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType), Expression.Constant(dc));
							return new TransformInfo(newExpr);
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

					case ExpressionType.Call:
					{
						var methodCall = (MethodCallExpression) e;

						var generic = methodCall.Method.IsGenericMethod ? methodCall.Method.GetGenericMethodDefinition() : methodCall.Method;

						if (IsQueryable(methodCall))
						{
							if (methodCall.Method.IsGenericMethod)
							{
								var isTunnel = false;

								if (generic == IgnoreQueryFiltersMethodInfo)
								{
									var newMethod = Expression.Call(
										Methods.LinqToDB.IgnoreFilters.MakeGenericMethod(methodCall.Method.GetGenericArguments()),
										methodCall.Arguments[0], Expression.NewArrayInit(typeof(Type)));
									return new TransformInfo(newMethod, false, true);
								}
								else if (generic == AsNoTrackingMethodInfo
#if !EF31
									|| generic == AsNoTrackingWithIdentityResolutionMethodInfo
#endif
									)
								{
									isTunnel = true;
									tracking = false;
								}
								else if (generic == IncludeMethodInfo)
								{
									var method =
										Methods.LinqToDB.LoadWith.MakeGenericMethod(methodCall.Method
											.GetGenericArguments());

									return new TransformInfo(Expression.Call(method, methodCall.Arguments), false, true);
								}
								else if (generic == IncludeMethodInfoString)
								{
									var arguments = new List<Expression>(2)
									{
										methodCall.Arguments[0]
									};

									var propName = (string)EvaluateExpression(methodCall.Arguments[1])!;
									var param    = Expression.Parameter(methodCall.Method.GetGenericArguments()[0], "e");
									var propPath = propName.Split(_nameSeparator, StringSplitOptions.RemoveEmptyEntries);
									var prop     = (Expression)param;
									for (var i = 0; i < propPath.Length; i++)
									{
										prop = Expression.PropertyOrField(prop, propPath[i]);
									}
									
									arguments.Add(Expression.Lambda(prop, param));

									var method =
										Methods.LinqToDB.LoadWith.MakeGenericMethod(param.Type, prop.Type);

									return new TransformInfo(Expression.Call(method, arguments.ToArray()), false, true);
								}
								else if (generic == ThenIncludeMethodInfo)
								{
									var method =
										Methods.LinqToDB.ThenLoadFromSingle.MakeGenericMethod(methodCall.Method
											.GetGenericArguments());

									return new TransformInfo(Expression.Call(method, methodCall.Arguments.Select(a => a.Transform(l => LocalTransform(l)))
										.ToArray()), false, true);
								}
								else if (generic == ThenIncludeEnumerableMethodInfo)
								{
									var method =
										Methods.LinqToDB.ThenLoadFromMany.MakeGenericMethod(methodCall.Method
											.GetGenericArguments());

									return new TransformInfo(Expression.Call(method, methodCall.Arguments.Select(a => a.Transform(l => LocalTransform(l)))
										.ToArray()), false, true);
								}
								else if (generic == Methods.LinqToDB.RemoveOrderBy)
								{
									// This is workaround. EagerLoading runs query again with RemoveOrderBy method.
									// it is only one possible way now how to detect nested query. 
									ignoreTracking = true;
								}
								else if (generic == TagWithMethodInfo)
								{
									var method = Methods.LinqToDB.TagQuery.MakeGenericMethod(methodCall.Method.GetGenericArguments());

									return new TransformInfo(Expression.Call(method, methodCall.Arguments.Select(a => a.Transform(l => LocalTransform(l)))
										.ToArray()), false, true);
								}

								if (isTunnel)
									return new TransformInfo(methodCall.Arguments[0], false, true);
							}

							break;
						}

#if !EF31
						if (generic == AsSplitQueryMethodInfo || generic == AsSingleQueryMethodInfo)
							return new TransformInfo(methodCall.Arguments[0], false, true);
#endif

						if (typeof(ITable<>).IsSameOrParentOf(methodCall.Type))
						{
							if (generic.Name == "ToLinqToDBTable")
							{
								return new TransformInfo(methodCall.Arguments[0], false, true);
							}

							break;
						}

#if EF31
						if (generic == FromSqlOnQueryableMethodInfo)
						{
							//convert the arguments from the FromSqlOnQueryable method from EF, to a L2DB FromSql call
							return new TransformInfo(Expression.Call(null, L2DBFromSqlMethodInfo.MakeGenericMethod(methodCall.Method.GetGenericArguments()[0]),
								Expression.Constant(dc), 
								Expression.New(RawSqlStringConstructor, methodCall.Arguments[1]),
								methodCall.Arguments[2]), false, true);
						}
#endif

						if (typeof(IQueryable<>).IsSameOrParentOf(methodCall.Type) && methodCall.Type.Assembly != typeof(LinqExtensions).Assembly)
						{
							if (((dc != null && !dc.MappingSchema.HasAttribute<ExpressionMethodAttribute>(methodCall.Type, methodCall.Method))
								|| (dc == null && !methodCall.Method.HasAttribute<ExpressionMethodAttribute>()))
								&& null == methodCall.Find(nonEvaluableParameters,
								    (c, t) => t.NodeType == ExpressionType.Parameter && c.Contains(t) || t.NodeType == ExpressionType.Extension))
							{
								// Invoking function to evaluate EF's Subquery located in function

								var obj = EvaluateExpression(methodCall.Object);
								var arguments = methodCall.Arguments.Select(EvaluateExpression).ToArray();
								if (methodCall.Method.Invoke(obj, arguments) is IQueryable result)
								{
									if (!ExpressionEqualityComparer.Instance.Equals(methodCall, result.Expression))
										return new TransformInfo(result.Expression, false, true);
								}
							}
						}

						if (generic == EFProperty)
						{
							var prop = Expression.Call(null, Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(methodCall.Method.GetGenericArguments()[0]),
								methodCall.Arguments[0], methodCall.Arguments[1]);
							return new TransformInfo(prop, false, true);
						}

						List<Expression>? newArguments = null;
						var parameters = generic.GetParameters();
						for (var i = 0; i < parameters.Length; i++)
						{
							var arg = methodCall.Arguments[i];
							var canWrap = true;

							if (arg.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression) arg;
								if (mc.Method.DeclaringType == typeof(Sql))
									canWrap = false;
							}

							if (canWrap)
							{
								if (parameters[i].HasAttribute<NotParameterizedAttribute>())
								{
									newArguments ??= new List<Expression>(methodCall.Arguments.Take(i));

									newArguments.Add(Expression.Call(ToSql.MakeGenericMethod(arg.Type), arg));
									continue;
								}
							}

							newArguments?.Add(methodCall.Arguments[i]);
						}

						if (newArguments != null)
							return new TransformInfo(methodCall.Update(methodCall.Object, newArguments), false, true);

						break;
					}
#if !EF31

					case ExpressionType.Extension:
					{
						if (dc != null && e is QueryRootExpression queryRoot)
							return new TransformInfo(TransformQueryRootExpression(dc, queryRoot));

						break;
					}
#endif
				}

				return new TransformInfo(e);
			}

			var newExpression = expression.Transform(LocalTransform);

			if (!ignoreTracking && dc is LinqToDBForEFToolsDataConnection dataConnection)
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				dataConnection.Tracking = tracking;
			}

			return newExpression;
		}

#if !EF31
		/// <summary>
		/// Transforms <see cref="QueryRootExpression"/> descendants to linq2db analogue. Handles Temporal tables also.
		/// </summary>
		/// <param name="dc">Data context.</param>
		/// <param name="queryRoot">Query root expression</param>
		/// <returns>Transformed expression.</returns>
		protected virtual Expression TransformQueryRootExpression(IDataContext dc, QueryRootExpression queryRoot)
		{
			static Expression GetAsOfSqlServer(Expression getTableExpr, Type entityType)
			{
				return Expression.Call(
					AsSqlServerTable.MakeGenericMethod(entityType),
					getTableExpr);
			}

			if (queryRoot is FromSqlQueryRootExpression fromSqlQueryRoot)
			{
				//convert the arguments from the FromSqlOnQueryable method from EF, to a L2DB FromSql call
				return Expression.Call(null,
					L2DBFromSqlMethodInfo.MakeGenericMethod(fromSqlQueryRoot.EntityType.ClrType),
					Expression.Constant(dc),
					Expression.New(RawSqlStringConstructor, Expression.Constant(fromSqlQueryRoot.Sql)),
					fromSqlQueryRoot.Argument);
			}

#if EF6
			var entityType = queryRoot.EntityType.ClrType;
#else
			var entityType = queryRoot.ElementType;
#endif
			var getTableExpr = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType),
				Expression.Constant(dc));

			var expressionTypeName = queryRoot.GetType().Name;
			if (expressionTypeName == "TemporalAsOfQueryRootExpression")
			{
				var pointInTime = GetPropValue<DateTime>(queryRoot, "PointInTime");

				var asOf = Expression.Call(TemporalAsOfTable.MakeGenericMethod(entityType), 
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(pointInTime));

				return asOf;
			}

			if (expressionTypeName == "TemporalFromToQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(TemporalFromTo.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalBetweenQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(TemporalBetween.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalContainedInQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(TemporalContainedIn.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalAllQueryRootExpression")
			{
				var all = Expression.Call(TemporalAll.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType));

				return all;
			}

			return getTableExpr;
		}
#endif

		static Expression EnsureEnumerable(Expression expression, MappingSchema mappingSchema)
		{
			var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type, mappingSchema));
			if (expression.Type != enumerable)
				expression = Expression.Convert(expression, enumerable);
			return expression;
		}

		static LambdaExpression EnsureEnumerable(LambdaExpression lambda, MappingSchema mappingSchema)
		{
			var newBody = EnsureEnumerable(lambda.Body, mappingSchema);
			if (newBody != lambda.Body)
				lambda = Expression.Lambda(newBody, lambda.Parameters);
			return lambda;
		}


		static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}

		static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}

		/// <summary>
		/// Extracts <see cref="DbContext"/> instance from <see cref="IQueryable"/> object.
		/// Due to unavailability of integration API in EF Core this method use reflection and could became broken after EF Core update.
		/// </summary>
		/// <param name="query">EF Core query.</param>
		/// <returns>Current <see cref="DbContext"/> instance.</returns>
		public virtual DbContext? GetCurrentContext(IQueryable query)
		{
			var compilerField = typeof (EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var compiler = (QueryCompiler)compilerField.GetValue(query.Provider)!;

			var queryContextFactoryField = compiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new LinqToDBForEFToolsException($"Can not find private field '{compiler.GetType()}._queryContextFactory' in current EFCore Version.");
			if (queryContextFactoryField.GetValue(compiler) is not RelationalQueryContextFactory queryContextFactory)
				throw new LinqToDBForEFToolsException("LinqToDB Tools for EFCore support only Relational Databases.");

			var dependencies = (QueryContextDependencies)ContextDependenciesGetValueMethod(queryContextFactory)!;

			return dependencies.CurrentContext?.Context;
		}

		/// <summary>
		/// Extracts EF Core connection information object from <see cref="IDbContextOptions"/>.
		/// </summary>
		/// <param name="options"><see cref="IDbContextOptions"/> instance.</param>
		/// <returns>EF Core connection data.</returns>
		public virtual EFConnectionInfo ExtractConnectionInfo(IDbContextOptions? options)
		{
			var relational = options?.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();
			return new  EFConnectionInfo
			{
				ConnectionString = relational?.ConnectionString,
				Connection = relational?.Connection
			};
		}

		/// <summary>
		/// Extracts EF Core data model instance from <see cref="IDbContextOptions"/>.
		/// </summary>
		/// <param name="options"><see cref="IDbContextOptions"/> instance.</param>
		/// <returns>EF Core data model instance.</returns>
		public virtual IModel? ExtractModel(IDbContextOptions? options)
		{
			var coreOptions = options?.Extensions.OfType<CoreOptionsExtension>().FirstOrDefault();
			return coreOptions?.Model;
		}

		/// <summary>
		/// Logs lin2db trace event to logger.
		/// </summary>
		/// <param name="info">lin2db trace event.</param>
		/// <param name="logger">Logger instance.</param>
		public virtual void LogConnectionTrace(TraceInfo info, ILogger logger)
		{
			var logLevel = info.TraceLevel switch
			{
				TraceLevel.Off => LogLevel.None,
				TraceLevel.Error => LogLevel.Error,
				TraceLevel.Warning => LogLevel.Warning,
				TraceLevel.Info => LogLevel.Information,
				TraceLevel.Verbose => LogLevel.Debug,
				_ => LogLevel.Trace,
			};

#pragma warning disable CA1848 // Use the LoggerMessage delegates
			using var _ = logger.BeginScope("TraceInfoStep: {TraceInfoStep}, IsAsync: {IsAsync}", info.TraceInfoStep, info.IsAsync);

			switch (info.TraceInfoStep)
			{
				case TraceInfoStep.BeforeExecute:
					logger.Log(logLevel, "{SqlText}", info.SqlText);
					break;

				case TraceInfoStep.AfterExecute:
					if (info.RecordsAffected is null)
					{
						logger.Log(logLevel, "Query Execution Time: {ExecutionTime}.", info.ExecutionTime);
					}
					else
					{
						logger.Log(logLevel, "Query Execution Time: {ExecutionTime}. Records Affected: {RecordsAffected}.", info.ExecutionTime, info.RecordsAffected);
					}
					break;

				case TraceInfoStep.Error:
				{
					logger.Log(logLevel, info.Exception, "Failed executing command.");
					break;
				}

				case TraceInfoStep.Completed:
				{
					if (info.RecordsAffected is null)
					{
						logger.Log(logLevel, "Total Execution Time: {TotalExecutionTime}.", info.ExecutionTime);
					}
					else
					{
						logger.Log(logLevel, "Total Execution Time: {TotalExecutionTime}. Rows Count: {RecordsAffected}.", info.ExecutionTime, info.RecordsAffected);
					}
					break;
				}
			}
#pragma warning restore CA1848 // Use the LoggerMessage delegates
		}

		/// <summary>
		/// Creates logger instance.
		/// </summary>
		/// <param name="options"><see cref="DbContext"/> options.</param>
		/// <returns>Logger instance.</returns>
		public virtual ILogger? CreateLogger(IDbContextOptions? options)
		{
			var coreOptions = options?.FindExtension<CoreOptionsExtension>();

			var logger = coreOptions?.LoggerFactory?.CreateLogger("LinqToDB");

			return logger;
		}

		/// <summary>
		/// Gets or sets default provider version for SQL Server. Set to <see cref="SqlServerVersion.AutoDetect"/> dialect.
		/// </summary>
		public static SqlServerVersion SqlServerDefaultVersion { get; set; } = SqlServerVersion.AutoDetect;

		/// <summary>
		/// Gets or sets default provider version for PostgreSQL Server. Set to <see cref="PostgreSQLVersion.AutoDetect"/> dialect.
		/// </summary>
		public static PostgreSQLVersion PostgreSqlDefaultVersion { get; set; } = PostgreSQLVersion.AutoDetect;

		/// <summary>
		/// Enables attaching entities to change tracker.
		/// Entities will be attached only if AsNoTracking() is not used in query and DbContext is configured to track entities. 
		/// </summary>
		public virtual bool EnableChangeTracker { get; set; } = true;
	}
}
