#pragma warning disable CA1873 // Avoid potentially expensive logging
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

using LinqToDB.Common;
using LinqToDB.Common.Internal;
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
using LinqToDB.EntityFrameworkCore.Internal;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using LinqToDB.SqlQuery;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LinqToDB.EntityFrameworkCore
{
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
				return HashCode.Combine(ProviderName, ConnectionString);
			}
			
			#endregion
		}

		readonly ConcurrentDictionary<ProviderKey, IDataProvider> _knownProviders = new();

		private readonly MemoryCache _schemaCache = new(
			new MemoryCacheOptions()
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
				ProviderName.SqlServer                                                    => CreateSqlServerProvider(SqlServerDefaultVersion, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2005                                                => CreateSqlServerProvider(SqlServerVersion.v2005, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2008                                                => CreateSqlServerProvider(SqlServerVersion.v2008, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2012                                                => CreateSqlServerProvider(SqlServerVersion.v2012, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2014                                                => CreateSqlServerProvider(SqlServerVersion.v2014, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2016                                                => CreateSqlServerProvider(SqlServerVersion.v2016, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2017                                                => CreateSqlServerProvider(SqlServerVersion.v2017, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2019                                                => CreateSqlServerProvider(SqlServerVersion.v2019, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.SqlServer2022                                                => CreateSqlServerProvider(SqlServerVersion.v2022, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.MySql                                                        => MySqlTools.GetDataProvider(MySqlVersion.AutoDetect, MySqlProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.MySql57                                                      => MySqlTools.GetDataProvider(MySqlVersion.MySql57, MySqlProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.MySql80                                                      => MySqlTools.GetDataProvider(MySqlVersion.MySql80, MySqlProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.MariaDB10                                                    => MySqlTools.GetDataProvider(MySqlVersion.MariaDB10, MySqlProvider.MySqlConnector, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.PostgreSQL                                                   => CreatePostgreSqlProvider(PostgreSqlDefaultVersion, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.PostgreSQL92                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v92, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.PostgreSQL93                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v93, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.PostgreSQL95                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v95, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.PostgreSQL15                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v15, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.PostgreSQL18                                                 => CreatePostgreSqlProvider(PostgreSQLVersion.v18, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.SQLite or ProviderName.SQLiteMS                              => SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.Firebird                                                     => FirebirdTools.GetDataProvider(FirebirdVersion.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Firebird25                                                   => FirebirdTools.GetDataProvider(FirebirdVersion.v25, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Firebird3                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v3, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Firebird4                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v4, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Firebird5                                                    => FirebirdTools.GetDataProvider(FirebirdVersion.v5, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.DB2 or ProviderName.DB2LUW                                   => DB2Tools.GetDataProvider(DB2Version.LUW, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.DB2zOS                                                       => DB2Tools.GetDataProvider(DB2Version.zOS, connectionInfo.ConnectionString, connectionInfo.Connection),

				ProviderName.Oracle                                                       => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.AutoDetect, connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Oracle11Native                                               => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Native    , connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.OracleNative                                                 => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Native    , connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Oracle11Managed                                              => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Managed   , connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.OracleManaged                                                => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Managed   , connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.Oracle11Devart                                               => OracleTools.GetDataProvider(OracleVersion.v11       , OracleProvider.Devart    , connectionInfo.ConnectionString, connectionInfo.Connection),
				ProviderName.OracleDevart                                                 => OracleTools.GetDataProvider(OracleVersion.AutoDetect, OracleProvider.Devart    , connectionInfo.ConnectionString, connectionInfo.Connection),

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
		/// <param name="connection">Connection.</param>
		/// <returns>Linq To DB SQL Server provider instance.</returns>
		protected virtual IDataProvider CreateSqlServerProvider(SqlServerVersion version, string? connectionString, DbConnection? connection)
		{
			return DataProvider.SqlServer.SqlServerTools.GetDataProvider(version, SqlServerProvider.MicrosoftDataSqlClient, connectionString, connection);
		}

		/// <summary>
		/// Creates Linq To DB PostgreSQL database provider instance.
		/// </summary>
		/// <param name="version">PostgreSQL dialect.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="connection">Connection.</param>
		/// <returns>Linq To DB PostgreSQL provider instance.</returns>
		protected virtual IDataProvider CreatePostgreSqlProvider(PostgreSQLVersion version, string? connectionString, DbConnection? connection)
		{
			return PostgreSQLTools.GetDataProvider(version, connectionString, connection);
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
		/// <param name="mappingSource">EF Core mapping source.</param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <param name="convertorSelector"></param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		public virtual MappingSchema CreateMappingSchema(
			IModel model,
			IRelationalTypeMappingSource? mappingSource,
			IMetadataReader? metadataReader,
			IValueConverterSelector? convertorSelector,
			DataOptions dataOptions)
		{
			var schema = new MappingSchema();
			if (metadataReader != null)
				schema.AddMetadataReader(metadataReader);

			DefineConvertors(schema, model, mappingSource, convertorSelector, dataOptions);

			return schema;
		}

		/// <summary>
		/// Import type conversions from EF Core model into Linq To DB mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Linq To DB mapping schema.</param>
		/// <param name="model">EF Core data mode.</param>
		/// <param name="mappingSource">EF Core mapping source.</param>
		/// <param name="convertorSelector">Type filter.</param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		public virtual void DefineConvertors(
			MappingSchema mappingSchema,
			IModel model,
			IRelationalTypeMappingSource? mappingSource,
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
				if (modelType.IsEnum)
				{
					MapEnumType(modelType);
					continue;
				}

				// skipping arrays
				if (modelType.IsArray)
					continue;

				MapEFCoreType(modelType);
				if (modelType.IsValueType && !typeof(Nullable<>).IsSameOrParentOf(modelType))
					MapEFCoreType(typeof(Nullable<>).MakeGenericType(modelType));
			}

			void MapEnumType(Type type)
			{
				var mapping = mappingSource?.FindMapping(type);
				if (mapping?.GetType().Name == "NpgsqlEnumTypeMapping")
				{
					var labels = mapping.GetType().GetProperty("Labels")?.GetValue(mapping) as IReadOnlyDictionary<object, string>;
					if (labels != null)
					{
						var typedLabels = labels.ToDictionary(kv => kv.Key, kv => $"'{kv.Value}'::{mapping.StoreType}");

						mappingSchema.SetDataType(type, new SqlDataType(new DbDataType(type, DataType.Enum, mapping.StoreType)));
						mappingSchema.SetValueToSqlConverter(type, (sb, _, v) => sb.Append(typedLabels[v]));
					}
				}
			}

			void MapEFCoreType(Type modelType)
			{
				var currentType = mappingSchema.GetDataType(modelType);
				if (!currentType.Equals(SqlDataType.Undefined))
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
					ReflectionMethods.DataParameterConstructor,
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
		/// <param name="mappingSource">EF Core mapping source.</param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <param name="convertorSelector"></param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		public virtual MappingSchema GetMappingSchema(
			IModel model,
			IRelationalTypeMappingSource? mappingSource,
			IMetadataReader? metadataReader,
			IValueConverterSelector? convertorSelector,
			DataOptions? dataOptions)
		{
			dataOptions ??= new();

			var result = _schemaCache.GetOrCreate(
				(
					dataOptions,
					model,
					mappingSource,
					metadataReader,
					convertorSelector,
					EnableChangeTracker
				),
				e =>
				{
					e.SlidingExpiration = TimeSpan.FromHours(1);
					return CreateMappingSchema(model, mappingSource, metadataReader, convertorSelector, dataOptions);
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

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvokeExt();
			return value;
		}

		/// <summary>
		/// Transforms EF Core expression tree to LINQ To DB expression.
		/// Method replaces EF Core <see cref="EntityQueryable{TResult}"/> instances with LINQ To DB
		/// <see cref="DataExtensions.GetTable{T}(IDataContext)"/> calls.
		/// </summary>
		/// <param name="expression">EF Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <param name="ctx">Optional DbContext instance.</param>
		/// <param name="model">EF Core data model instance.</param>
		/// <param name="isQueryExpression">Indicates that query may contain tracking information</param>
		/// <returns>Transformed expression.</returns>
		public virtual Expression TransformExpression(Expression expression, IDataContext? dc, DbContext? ctx, IModel? model, bool isQueryExpression)
		{
			var visitor       = new TransformExpressionVisitor();
			var newExpression = visitor.Transform(dc, model, expression);

			if (ReferenceEquals(newExpression, expression))
				return expression;

			if (isQueryExpression && dc is LinqToDBForEFToolsDataConnection dataConnection)
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse

				bool tracking;

				if (visitor.Tracking == null)
				{
					if (ctx == null)
					{
						tracking = true;
					}
					else
					{
						var options = ctx.GetDbContextOptions();
						if (options == null)
							tracking = true;
						else
						{
							var coreOptions = options.FindExtension<CoreOptionsExtension>();
							tracking = coreOptions?.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll;
						}
					}
				}
				else
					tracking = visitor.Tracking.Value;

				dataConnection.Tracking = tracking;
			}

			return newExpression;
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

			// Allow subclasses of QueryCompiler. E.g. used by https://github.com/koenbeuk/EntityFrameworkCore.Projectables.
			// In case we never find it in the class hierarchy, the GetField below will throw an exception.
			var compilerType = compiler.GetType();
			while (compilerType != typeof(QueryCompiler)
					&& compilerType.BaseType is {} baseType)
			{
				compilerType = baseType;
			}

			var queryContextFactoryField = compilerType.GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new LinqToDBForEFToolsException($"Can not find private field '{compiler.GetType()}._queryContextFactory' in current EFCore Version.");
			if (queryContextFactoryField.GetValue(compiler) is not RelationalQueryContextFactory queryContextFactory)
				throw new LinqToDBForEFToolsException("LinqToDB Tools for EFCore support only Relational Databases.");

			var dependencies = (QueryContextDependencies)ReflectionMethods.ContextDependenciesGetValueMethod(queryContextFactory)!;

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
			return new EFConnectionInfo
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
