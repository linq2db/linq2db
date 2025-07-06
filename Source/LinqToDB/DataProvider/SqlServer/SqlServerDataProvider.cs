using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer.Translation;
using LinqToDB.Extensions;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	sealed class SqlServerDataProvider2005SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2005SystemDataSqlClient   () : base(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2008SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2008SystemDataSqlClient   () : base(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2012SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2012SystemDataSqlClient   () : base(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2014SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2014SystemDataSqlClient   () : base(ProviderName.SqlServer2014, SqlServerVersion.v2014, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2016SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2016SystemDataSqlClient   () : base(ProviderName.SqlServer2016, SqlServerVersion.v2016, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2017SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2017SystemDataSqlClient   () : base(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2019SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2019SystemDataSqlClient   () : base(ProviderName.SqlServer2019, SqlServerVersion.v2019, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2022SystemDataSqlClient    : SqlServerDataProvider { public SqlServerDataProvider2022SystemDataSqlClient   () : base(ProviderName.SqlServer2022, SqlServerVersion.v2022, SqlServerProvider.SystemDataSqlClient)    {} }
	sealed class SqlServerDataProvider2005MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2005MicrosoftDataSqlClient() : base(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2008MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2008MicrosoftDataSqlClient() : base(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2012MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2012MicrosoftDataSqlClient() : base(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2014MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2014MicrosoftDataSqlClient() : base(ProviderName.SqlServer2014, SqlServerVersion.v2014, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2016MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2016MicrosoftDataSqlClient() : base(ProviderName.SqlServer2016, SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2017MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2017MicrosoftDataSqlClient() : base(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2019MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2019MicrosoftDataSqlClient() : base(ProviderName.SqlServer2019, SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient) {} }
	sealed class SqlServerDataProvider2022MicrosoftDataSqlClient : SqlServerDataProvider { public SqlServerDataProvider2022MicrosoftDataSqlClient() : base(ProviderName.SqlServer2022, SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient) {} }

	public abstract class SqlServerDataProvider : DynamicDataProviderBase<SqlServerProviderAdapter>
	{
		#region Init

		protected SqlServerDataProvider(string name, SqlServerVersion version)
			: this(name, version, SqlServerProvider.AutoDetect)
		{
		}

		protected SqlServerDataProvider(string name, SqlServerVersion version, SqlServerProvider provider)
			: base(
				name,
				MappingSchemaInstance.Get(version, provider),
				SqlServerProviderAdapter.GetInstance(provider == SqlServerProvider.AutoDetect ? provider = SqlServerProviderDetector.DetectProvider() : provider))
		{
			Version  = version;
			Provider = provider;

			SqlProviderFlags.AcceptsOuterExpressionInAggregate  = false;
			SqlProviderFlags.OutputDeleteUseSpecialTable        = true;
			SqlProviderFlags.OutputInsertUseSpecialTable        = true;
			SqlProviderFlags.OutputUpdateUseSpecialTables       = true;
			SqlProviderFlags.OutputMergeUseSpecialTables        = true;
			SqlProviderFlags.IsApplyJoinSupported               = true;
			SqlProviderFlags.TakeHintsSupported                 = TakeHints.Percent | TakeHints.WithTies;
			SqlProviderFlags.IsCommonTableExpressionsSupported  = true;
			SqlProviderFlags.IsRowNumberWithoutOrderBySupported = false;
			SqlProviderFlags.IsCTESupportsOrdering              = false;
			SqlProviderFlags.IsUpdateTakeSupported              = true;
			SqlProviderFlags.IsDistinctFromSupported            = Version >= SqlServerVersion.v2022;
			SqlProviderFlags.SupportsBooleanType                = false;

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
				SqlServerVersion.v2022 => new SqlServer2022SqlOptimizer(SqlProviderFlags),
				_                      => new SqlServer2008SqlOptimizer(SqlProviderFlags),
			};

			// missing:
			// GetSqlBytes
			SetProviderField<SqlChars   , SqlChars   >(SqlTypes.GetSqlCharsReaderMethod   , dataReaderType: Adapter.DataReaderType);
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

			if (Adapter.SqlJsonType != null)
			{
				SetProviderField(Adapter.SqlJsonType, typeof(string), Adapter.GetSqlJsonReaderMethod!, dataReaderType: Adapter.DataReaderType, typeName: "json");
				// safe assumption as if SqlJson type found, JsonDocument will also exist
				var jsonDocumentType = Type.GetType("System.Text.Json.JsonDocument, System.Text.Json");
				if (jsonDocumentType != null)
				{
					SetGetFieldValueReader(jsonDocumentType, typeof(string), dataReaderType: Adapter.DataReaderType, typeName: "json");
				}
			}

			// TODO: finish implementation when SQL 2025 support/testing added
			if (Adapter.SqlVectorType != null)
			{
				SetProviderField(Adapter.SqlVectorType, typeof(byte[]), Adapter.GetSqlVectorReaderMethod!, dataReaderType: Adapter.DataReaderType, typeName: "vector");
			}

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
			public static MappingSchema Get(SqlServerVersion version, SqlServerProvider provider)
			{
				return (version, provider) switch
				{
					(SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2005MappingSchemaSystem(),
					(SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2008MappingSchemaSystem(),
					(SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2012MappingSchemaSystem(),
					(SqlServerVersion.v2014, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2014MappingSchemaSystem(),
					(SqlServerVersion.v2016, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2016MappingSchemaSystem(),
					(SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2017MappingSchemaSystem(),
					(SqlServerVersion.v2019, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2019MappingSchemaSystem(),
					(SqlServerVersion.v2022, SqlServerProvider.SystemDataSqlClient) => new SqlServerMappingSchema.SqlServer2022MappingSchemaSystem(),

					(SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2005MappingSchemaMicrosoft(),
					(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2008MappingSchemaMicrosoft(),
					(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2012MappingSchemaMicrosoft(),
					(SqlServerVersion.v2014, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2014MappingSchemaMicrosoft(),
					(SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2016MappingSchemaMicrosoft(),
					(SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2017MappingSchemaMicrosoft(),
					(SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2019MappingSchemaMicrosoft(),
					(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient) => new SqlServerMappingSchema.SqlServer2022MappingSchemaMicrosoft(),

					_ => throw new InvalidOperationException($"Unexpected dialect/provider: {version}, {provider}")
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

		protected override IMemberTranslator CreateMemberTranslator()
		{
			if (Version >= SqlServerVersion.v2022)
				return new SqlServer2022MemberTranslator();

			if (Version >= SqlServerVersion.v2012)
				return new SqlServer2012MemberTranslator();

			if (Version == SqlServerVersion.v2005)
				return new SqlServer2005MemberTranslator();

			return new SqlServerMemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return Version switch
			{
				SqlServerVersion.v2005 => new SqlServer2005SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2008 => new SqlServer2008SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2012 => new SqlServer2012SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2014 => new SqlServer2014SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2016 => new SqlServer2016SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2017 => new SqlServer2017SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2019 => new SqlServer2019SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				SqlServerVersion.v2022 => new SqlServer2022SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				_                      => throw new InvalidOperationException(),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SqlServerSchemaProvider(this);
		}

		static readonly ConcurrentDictionary<string,bool> _marsFlags = new ();

		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public override object? GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			// take it from real Connection object, as dataConnection.ConnectionString could be null
			// also it will not cache original connection string with credentials in _marsFlags
			var connectionString = dataConnection.OpenDbConnection().ConnectionString;
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
			// SqlClient supports less DbType's for SqlDateTime than for DateTime
			// SqlDateTime is designer for DATETIME type only
			if (value is SqlDateTime sdt && !sdt.IsNull)
				value = (DateTime)sdt;

			var param = TryGetProviderParameter(dataConnection, parameter);

			switch (dataType.DataType)
			{
				case DataType.SmallDateTime or DataType.DateTime
						when value is DateTimeOffset dto:
					value = dto.LocalDateTime;
					break;

				case DataType.DateTime2 when value is DateTimeOffset dto:
					if (Version == SqlServerVersion.v2005)
						value = dto.LocalDateTime.WithPrecision(dataType.Precision > 3 ? 3 : (dataType.Precision ?? 3));
					else
						value = dto.WithPrecision(dataType.Precision ?? 7).LocalDateTime;
					break;

				case DataType.DateTimeOffset when value is DateTimeOffset dto:
				{
					if (Version == SqlServerVersion.v2005)
						value = dto.LocalDateTime.WithPrecision(dataType.Precision > 3 ? 3 : (dataType.Precision ?? 3));
					else
						value = dto.WithPrecision(dataType.Precision ?? 7);
					break;
				}

				case DataType.Date when value is DateTime dt:
					if (Version is SqlServerVersion.v2005)
						value = dt.Date;
					break;

				case DataType.Date when value is DateTimeOffset dto:
					value = dto.LocalDateTime.Date;
					break;

#if NET8_0_OR_GREATER
				case DataType.Date when value is DateOnly d:
					value = d.ToDateTime(TimeOnly.MinValue);
					break;

				case DataType.Text or DataType.Char or DataType.VarChar
					or DataType.NText or DataType.NChar or DataType.NVarChar
						when value is DateOnly d:
					value = d.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo);
					break;
#endif

				case DataType.DateTime2 when value is DateTime dt:
					value = dt.WithPrecision(dataType.Precision ?? 7);
					break;

				case DataType.Udt:
					if (param != null
						&& value != null
						&& _udtTypeNames.TryGetValue(value.GetType(), out var typeName))
					{
						Adapter.SetUdtTypeName(param, typeName);
					}

					break;

				case DataType.NText when value is DateTime dt:
					value = dt.ToString(
						dt.Millisecond == 0
							? "yyyy-MM-ddTHH:mm:ss"
							: "yyyy-MM-ddTHH:mm:ss.fff",
						DateTimeFormatInfo.InvariantInfo);
					break;

				case DataType.Text or DataType.Char or DataType.VarChar
					or DataType.NText or DataType.NChar or DataType.NVarChar
						when value is DateTimeOffset dto:
					// SqlClient doesn't generate last digit for precision=6 ¯\_(ツ)_/¯
					value = SqlServerMappingSchema.ConvertDateTimeOffsetToString(dto, dataType.Precision ?? 7);
					break;

				case DataType.Text or DataType.Char or DataType.VarChar
					or DataType.NText or DataType.NChar or DataType.NVarChar
						when value is TimeSpan ts:
					value = SqlServerMappingSchema.ConvertTimeSpanToString(ts, dataType.Precision ?? 7);
					break;

				case DataType.Int64 when value is TimeSpan ts:
					value = ts.GetTicks(dataType.Precision ?? 7);
					break;
				case DataType.Time when value is TimeSpan ts:
					value = TimeSpan.FromTicks(ts.GetTicks(dataType.Precision ?? 7));
					break;

				case DataType.Money     :
				case DataType.SmallMoney:
					parameter.Precision = 0;
					parameter.Scale     = 0;
					break;

				case DataType.Decimal:
					if (parameter.Precision != 0 || parameter.Scale != 0)
					{
						SqlDecimal sqlDecimal;

						if (value is SqlDecimal sqlDec)
							sqlDecimal = sqlDec;
						else if (value is decimal dec)
							sqlDecimal = dec;
						else
						{
							// better safe than sorry
							parameter.Precision = 0;
							parameter.Scale     = 0;
							break;
						}

						// reset precison/scale if default mappings doesn't fit value
						if ((parameter.Precision - parameter.Scale) < (sqlDecimal.Precision - sqlDecimal.Scale) || parameter.Scale < sqlDecimal.Scale)
						{
							parameter.Precision = sqlDecimal.Precision;
							parameter.Scale     = sqlDecimal.Scale;
						}
					}

					break;

				case DataType.Undefined:
					if (value != null
						&& (value is DataTable
						|| value is DbDataReader
							|| value is IEnumerable<DbDataRecord>
							|| value.GetType().IsEnumerableType(Adapter.SqlDataRecordType)))
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
				case DataType.Text                    : type = SqlDbType.Text;          break;
				case DataType.NText                   : type = SqlDbType.NText;         break;
				case DataType.Binary                  : type = SqlDbType.Binary;        break;
				case DataType.Image                   : type = SqlDbType.Image;         break;
				case DataType.SmallMoney              : type = SqlDbType.SmallMoney;    break;
				// ArgumentException: The version of SQL Server in use does not support datatype 'date'
				case DataType.Date                    : type = Version == SqlServerVersion.v2005 ? SqlDbType.DateTime : SqlDbType.Date; break;
				case DataType.Time                    : type = SqlDbType.Time;          break;
				case DataType.SmallDateTime           : type = SqlDbType.SmallDateTime; break;
				case DataType.Timestamp               : type = SqlDbType.Timestamp;     break;
				case DataType.Structured              : type = SqlDbType.Structured;    break;
				case DataType.Json                    : type = Adapter.JsonDbType;      break;
				case DataType.Array | DataType.Single : type = Adapter.VectorDbType;    break;
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

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlServerOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlServerOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlServerOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
