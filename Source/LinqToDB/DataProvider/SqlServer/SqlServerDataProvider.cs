﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	class SqlServerDataProvider2005SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2005SystemDataSqlClient   () : base(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient)    {} }

	class SqlServerDataProvider2008SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2008SystemDataSqlClient   () : base(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2012SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2012SystemDataSqlClient   () : base(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2014SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2014SystemDataSqlClient   () : base(ProviderName.SqlServer2014, SqlServerVersion.v2014, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2016SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2016SystemDataSqlClient   () : base(ProviderName.SqlServer2016, SqlServerVersion.v2016, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2017SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2017SystemDataSqlClient   () : base(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2019SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2019SystemDataSqlClient   () : base(ProviderName.SqlServer2019, SqlServerVersion.v2019, SqlServerProvider.SystemDataSqlClient)    {} }
	class SqlServerDataProvider2005MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2005MicrosoftDataSqlClient() : base(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2008MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2008MicrosoftDataSqlClient() : base(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2012MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2012MicrosoftDataSqlClient() : base(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2014MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2014MicrosoftDataSqlClient() : base(ProviderName.SqlServer2014, SqlServerVersion.v2014, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2016MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2016MicrosoftDataSqlClient() : base(ProviderName.SqlServer2016, SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2017MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2017MicrosoftDataSqlClient() : base(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient) {} }
	class SqlServerDataProvider2019MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2019MicrosoftDataSqlClient() : base(ProviderName.SqlServer2019, SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient) {} }

	public class SqlServerDataProvider : DynamicDataProviderBase<SqlServerProviderAdapter>
	{
		#region Init

		public SqlServerDataProvider(string name, SqlServerVersion version)
			: this(name, version, SqlServerProvider.MicrosoftDataSqlClient)
		{
		}

		public SqlServerDataProvider(string name, SqlServerVersion version, SqlServerProvider provider)
			: base(
				  name,
				  MappingSchemaInstance.Get(version),
				  SqlServerProviderAdapter.GetInstance(provider))
		{
			Version  = version;
			Provider = provider;

			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = true;
			SqlProviderFlags.IsCountDistinctSupported          = true;
			SqlProviderFlags.IsUpdateFromSupported             = true;
			SqlProviderFlags.AcceptsOuterExpressionInAggregate = false;
			SqlProviderFlags.OutputDeleteUseSpecialTable       = true;
			SqlProviderFlags.OutputInsertUseSpecialTable       = true;
			SqlProviderFlags.OutputUpdateUseSpecialTables      = true;
			SqlProviderFlags.IsApplyJoinSupported              = true;
			SqlProviderFlags.TakeHintsSupported                = TakeHints.Percent | TakeHints.WithTies;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;

			SetCharField("char" , (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char" , DataTools.GetCharExpression);
			SetCharFieldToType<char>("nchar", DataTools.GetCharExpression);

			_sqlOptimizer = version switch
			{
				SqlServerVersion.v2005 => new SqlServer2005SqlOptimizer(SqlProviderFlags),
				SqlServerVersion.v2012 => new SqlServer2012SqlOptimizer(SqlProviderFlags),
				SqlServerVersion.v2014 => new SqlServer2014SqlOptimizer(SqlProviderFlags),
				SqlServerVersion.v2016 => new SqlServer2016SqlOptimizer(SqlProviderFlags),
				SqlServerVersion.v2017 => new SqlServer2017SqlOptimizer(SqlProviderFlags),
				SqlServerVersion.v2019 => new SqlServer2019SqlOptimizer(SqlProviderFlags),
				_                      => new SqlServer2008SqlOptimizer(SqlProviderFlags),
			};

			// missing:
			// GetSqlBytes
			// GetSqlChars
			SetProviderField<SqlBinary  , SqlBinary  >(SqlTypes.GetSqlBinaryReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlBoolean , SqlBoolean >(SqlTypes.GetSqlBooleanReaderMethod , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlByte    , SqlByte    >(SqlTypes.GetSqlByteReaderMethod    , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlDateTime, SqlDateTime>(SqlTypes.GetSqlDateTimeReaderMethod, dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlDecimal , SqlDecimal >(SqlTypes.GetSqlDecimalReaderMethod , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlDouble  , SqlDouble  >(SqlTypes.GetSqlDoubleReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlGuid    , SqlGuid    >(SqlTypes.GetSqlGuidReaderMethod    , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlInt16   , SqlInt16   >(SqlTypes.GetSqlInt16ReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlInt32   , SqlInt32   >(SqlTypes.GetSqlInt32ReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlInt64   , SqlInt64   >(SqlTypes.GetSqlInt64ReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlMoney   , SqlMoney   >(SqlTypes.GetSqlMoneyReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlSingle  , SqlSingle  >(SqlTypes.GetSqlSingleReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlString  , SqlString  >(SqlTypes.GetSqlStringReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField<SqlXml     , SqlXml     >(Adapter.GetSqlXmlReaderMethod      , dataReaderType: Adapter.DataReaderType);

			SetProviderField<DateTimeOffset>(Adapter.GetDateTimeOffsetReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField<TimeSpan>      (Adapter.GetTimeSpanReaderMethod              , dataReaderType: Adapter.DataReaderType);

			// non-specific fallback
			SetProviderField<DbDataReader, SqlString, SqlString>((r, i) => r.GetString(i));

			SqlServerTypes.Configure(this);
		}

		#endregion

		#region Public Properties

		public SqlServerVersion Version { get; }

		public SqlServerProvider Provider { get; }

		#endregion

		#region Overrides

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema SqlServer2005MappingSchema = new SqlServer2005MappingSchema();
			public static readonly MappingSchema SqlServer2008MappingSchema = new SqlServer2008MappingSchema();
			public static readonly MappingSchema SqlServer2012MappingSchema = new SqlServer2012MappingSchema();
			public static readonly MappingSchema SqlServer2014MappingSchema = new SqlServer2014MappingSchema();
			public static readonly MappingSchema SqlServer2016MappingSchema = new SqlServer2016MappingSchema();
			public static readonly MappingSchema SqlServer2017MappingSchema = new SqlServer2017MappingSchema();
			public static readonly MappingSchema SqlServer2019MappingSchema = new SqlServer2019MappingSchema();

			public static MappingSchema Get(SqlServerVersion version)
			{
				return version switch
				{
					SqlServerVersion.v2005 => SqlServer2005MappingSchema,
					SqlServerVersion.v2012 => SqlServer2012MappingSchema,
					SqlServerVersion.v2014 => SqlServer2014MappingSchema,
					SqlServerVersion.v2016 => SqlServer2016MappingSchema,
					SqlServerVersion.v2017 => SqlServer2017MappingSchema,
					SqlServerVersion.v2019 => SqlServer2019MappingSchema,
					_                      => SqlServer2008MappingSchema,
				};
			}
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsGlobalTemporaryData      |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return Version switch
			{
				SqlServerVersion.v2005 => new SqlServer2005SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2008 => new SqlServer2008SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2012 => new SqlServer2012SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2014 => new SqlServer2014SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2016 => new SqlServer2016SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2017 => new SqlServer2017SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				SqlServerVersion.v2019 => new SqlServer2019SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				_                      => throw new InvalidOperationException(),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SqlServerSchemaProvider(this);
		}

		static readonly ConcurrentDictionary<string,bool> _marsFlags = new ();

		public override object? GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			// take it from real Connection object, as dataConnection.ConnectionString could be null
			// also it will not cache original connection string with credentials in _marsFlags
			var connectionString = dataConnection.Connection.ConnectionString;
			switch (parameterName)
			{
				case "IsMarsEnabled" :
					if (connectionString != null)
					{
						if (!_marsFlags.TryGetValue(connectionString, out var flag))
						{
							flag = Adapter.CreateConnectionStringBuilder(connectionString).MultipleActiveResultSets;
							_marsFlags[connectionString] = flag;
						}

						return flag;
					}

					return false;
			}

			return null;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			var param = TryGetProviderParameter(dataConnection, parameter);

			switch (dataType.DataType)
			{
#if NET6_0_OR_GREATER
				case DataType.Date when value is DateOnly d:
					value = d.ToDateTime(TimeOnly.MinValue);
					break;

				case DataType.NText when value is DateOnly d:
					value = d.ToString("yyyy-MM-dd");
					break;
#endif

				case DataType.DateTime2 when value is DateTime dt:
					value = DataTools.AdjustPrecision(dt, (byte)(dataType.Precision ?? 7));
					break;

				case DataType.Udt:
				{
					if (param != null
						&& value != null
						&& _udtTypeNames.TryGetValue(value.GetType(), out var typeName))
					{
						Adapter.SetUdtTypeName(param, typeName);
					}
					break;
				}

				case DataType.NText when value is DateTimeOffset dto:
					value = dto.ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz");
					break;

				case DataType.NText when value is DateTime dt:
				{
					value = dt.ToString(
						dt.Millisecond == 0
							? "yyyy-MM-ddTHH:mm:ss"
							: "yyyy-MM-ddTHH:mm:ss.fff");
					break;
				}

				case DataType.NText when value is TimeSpan ts:
				{
					value = ts.ToString(
						ts.Days > 0
							? ts.Milliseconds > 0
								? "d\\.hh\\:mm\\:ss\\.fff"
								: "d\\.hh\\:mm\\:ss"
							: ts.Milliseconds > 0
								? "hh\\:mm\\:ss\\.fff"
								: "hh\\:mm\\:ss");
					break;
				}

				case DataType.Undefined:
					if (value != null
						&& (value is DataTable
						|| value is DbDataReader
							|| value is IEnumerable<DbDataRecord>
							|| value.GetType().IsEnumerableTType(Adapter.SqlDataRecordType)))
					{
						dataType = dataType.WithDataType(DataType.Structured);
					}

					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);

			if (param != null)
			{
				// Setting for NVarChar and VarChar constant size. It reduces count of cached plans.
				switch (Adapter.GetDbType(param))
				{
					case SqlDbType.Structured:
						{
							if (!string.IsNullOrEmpty(dataType.DbType))
								Adapter.SetTypeName(param, dataType.DbType!);

							// TVP doesn't support DBNull
							if (parameter.Value is DBNull)
								parameter.Value = null;

							break;
						}
					case SqlDbType.VarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 8000) || (value != null && strValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (strValue == null || strValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 8000;

							break;
						}
					case SqlDbType.NVarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 4000) || (value != null && strValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 4000 && (strValue == null || strValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 4000;

							break;
						}
					case SqlDbType.VarBinary:
						{
							var binaryValue = value as byte[];
							if ((binaryValue != null && binaryValue.Length > 8000) || (value != null && binaryValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (binaryValue == null || binaryValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 8000;

							break;
						}
				}
			}
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			SqlDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text          : type = SqlDbType.Text;          break;
				case DataType.NText         : type = SqlDbType.NText;         break;
				case DataType.Binary        : type = SqlDbType.Binary;        break;
				case DataType.Image         : type = SqlDbType.Image;         break;
				case DataType.SmallMoney    : type = SqlDbType.SmallMoney;    break;
				case DataType.Date          : type = SqlDbType.Date;          break;
				case DataType.Time          : type = SqlDbType.Time;          break;
				case DataType.SmallDateTime : type = SqlDbType.SmallDateTime; break;
				case DataType.Timestamp     : type = SqlDbType.Timestamp;     break;
				case DataType.Structured    : type = SqlDbType.Structured;    break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(dataConnection, parameter);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				// including provider-specific fallbacks
				case DataType.Text          : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText         : parameter.DbType = DbType.String;     break;
				case DataType.Binary        :
				case DataType.Timestamp     :
				case DataType.Image         : parameter.DbType = DbType.Binary;     break;
				case DataType.SmallMoney    :
				case DataType.Money         : parameter.DbType = DbType.Currency;    break;
				case DataType.SmallDateTime : parameter.DbType = DbType.DateTime;    break;
				case DataType.Structured    : parameter.DbType = DbType.Object;      break;
				case DataType.Xml           : parameter.DbType = DbType.Xml;         break;
				case DataType.SByte         : parameter.DbType = DbType.Int16;       break;
				case DataType.UInt16        : parameter.DbType = DbType.Int32;       break;
				case DataType.UInt32        : parameter.DbType = DbType.Int64;       break;
				case DataType.UInt64        :
				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;     break;
				case DataType.DateTime      :
				case DataType.DateTime2     :
					parameter.DbType =
						Version == SqlServerVersion.v2005 ?
							DbType.DateTime :
							DbType.DateTime2;
					break;
				default                     : base.SetParameterType(dataConnection, parameter, dataType); break;
			}
		}

		#endregion

		#region UDT support

		private readonly ConcurrentDictionary<Type, string> _udtTypeNames = new ();
		private readonly ConcurrentDictionary<string, Type> _udtTypes     = new ();

		public void AddUdtType(Type type, string udtName)
		{
			MappingSchema.SetScalarType(type);

			_udtTypeNames[type] = udtName;
			_udtTypes[udtName]  = type;
		}

		public void AddUdtType(Type type, string udtName, object? defaultValue, DataType dataType = DataType.Undefined)
		{
			MappingSchema.AddScalarType(type, defaultValue, dataType);

			_udtTypeNames[type] = udtName;
			_udtTypes[udtName]  = type;
		}

		public void AddUdtType<T>(string udtName, T defaultValue, DataType dataType = DataType.Undefined)
		{
			MappingSchema.AddScalarType(typeof(T), defaultValue, dataType);

			_udtTypeNames[typeof(T)] = udtName;
			_udtTypes[udtName]       = typeof(T);
		}

		internal Type? GetUdtTypeByName(string udtName)
		{
			if (_udtTypes.TryGetValue(udtName, out var type))
				return type;

			return null;
		}

		#endregion

		#region BulkCopy

		SqlServerBulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			_bulkCopy ??= new SqlServerBulkCopy(this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SqlServerTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new SqlServerBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SqlServerTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new SqlServerBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SqlServerTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
