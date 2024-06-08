﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	sealed class PostgreSQLDataProvider92 : PostgreSQLDataProvider { public PostgreSQLDataProvider92() : base(ProviderName.PostgreSQL92, PostgreSQLVersion.v92) {} }
	sealed class PostgreSQLDataProvider93 : PostgreSQLDataProvider { public PostgreSQLDataProvider93() : base(ProviderName.PostgreSQL93, PostgreSQLVersion.v93) {} }
	sealed class PostgreSQLDataProvider95 : PostgreSQLDataProvider { public PostgreSQLDataProvider95() : base(ProviderName.PostgreSQL95, PostgreSQLVersion.v95) {} }
	sealed class PostgreSQLDataProvider15 : PostgreSQLDataProvider { public PostgreSQLDataProvider15() : base(ProviderName.PostgreSQL15, PostgreSQLVersion.v15) {} }

	public abstract class PostgreSQLDataProvider : DynamicDataProviderBase<NpgsqlProviderAdapter>
	{
		protected PostgreSQLDataProvider(PostgreSQLVersion version)
			: this(GetProviderName(version), version)
		{
		}

		protected PostgreSQLDataProvider(string name, PostgreSQLVersion version)
			: base(name, GetMappingSchema(version), NpgsqlProviderAdapter.GetInstance())
		{
			Version = version;

			SqlProviderFlags.IsApplyJoinSupported              = version != PostgreSQLVersion.v92;
			SqlProviderFlags.IsInsertOrUpdateSupported         = version is not PostgreSQLVersion.v92 and not PostgreSQLVersion.v93;
			SqlProviderFlags.IsUpdateSetTableAliasSupported    = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = true;

			SqlProviderFlags.RowConstructorSupport = RowFeature.Equality        | RowFeature.Comparisons |
			                                         RowFeature.CompareToSelect | RowFeature.In | RowFeature.IsNull |
			                                         RowFeature.Update          | RowFeature.UpdateLiteral |
			                                         RowFeature.Overlaps        | RowFeature.Between;

			SetCharFieldToType<char>("bpchar"   , DataTools.GetCharExpression);
			SetCharFieldToType<char>("character", DataTools.GetCharExpression);

			SetCharField("bpchar"   , (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("character", (r,i) => r.GetString(i).TrimEnd(' '));

			if (Adapter.SupportsBigInteger)
				SetProviderField<DbDataReader, BigInteger, decimal>((DbDataReader rd, int idx) => rd.GetFieldValue<BigInteger>(idx));

			_sqlOptimizer = new PostgreSQLSqlOptimizer(SqlProviderFlags);

			ConfigureTypes();
		}

		private void ConfigureTypes()
		{
			// https://www.postgresql.org/docs/current/static/datatype.html
			// not all types are supported now
			// numeric types
			mapType("smallint"                , NpgsqlProviderAdapter.NpgsqlDbType.Smallint);
			mapType("integer"                 , NpgsqlProviderAdapter.NpgsqlDbType.Integer);
			mapType("bigint"                  , NpgsqlProviderAdapter.NpgsqlDbType.Bigint);
			mapType("numeric"                 , NpgsqlProviderAdapter.NpgsqlDbType.Numeric);
			mapType("real"                    , NpgsqlProviderAdapter.NpgsqlDbType.Real);
			mapType("double precision"        , NpgsqlProviderAdapter.NpgsqlDbType.Double);
			// monetary types
			mapType("money"                   , NpgsqlProviderAdapter.NpgsqlDbType.Money);
			// character types
			mapType("character"               , NpgsqlProviderAdapter.NpgsqlDbType.Char);
			mapType("character varying"       , NpgsqlProviderAdapter.NpgsqlDbType.Varchar);
			mapType("text"                    , NpgsqlProviderAdapter.NpgsqlDbType.Text);
			mapType("name"                    , NpgsqlProviderAdapter.NpgsqlDbType.Name);
			mapType("char"                    , NpgsqlProviderAdapter.NpgsqlDbType.InternalChar);
			// binary types
			mapType("bytea"                   , NpgsqlProviderAdapter.NpgsqlDbType.Bytea);
			// date/time types (reltime missing from enum)
			mapType("timestamp"               , NpgsqlProviderAdapter.NpgsqlDbType.Timestamp);
			mapType("timestamp with time zone", NpgsqlProviderAdapter.NpgsqlDbType.TimestampTZ);
			mapType("date"                    , NpgsqlProviderAdapter.NpgsqlDbType.Date);
			mapType("time"                    , NpgsqlProviderAdapter.NpgsqlDbType.Time);
			mapType("time with time zone"     , NpgsqlProviderAdapter.NpgsqlDbType.TimeTZ);
			mapType("interval"                , NpgsqlProviderAdapter.NpgsqlDbType.Interval);
			mapType("abstime"                 , NpgsqlProviderAdapter.NpgsqlDbType.Abstime);
			// boolean type
			mapType("boolean"                 , NpgsqlProviderAdapter.NpgsqlDbType.Boolean);
			// geometric types
			mapType("point"                   , NpgsqlProviderAdapter.NpgsqlDbType.Point);
			mapType("line"                    , NpgsqlProviderAdapter.NpgsqlDbType.Line);
			mapType("lseg"                    , NpgsqlProviderAdapter.NpgsqlDbType.LSeg);
			mapType("box"                     , NpgsqlProviderAdapter.NpgsqlDbType.Box);
			mapType("path"                    , NpgsqlProviderAdapter.NpgsqlDbType.Path);
			mapType("polygon"                 , NpgsqlProviderAdapter.NpgsqlDbType.Polygon);
			mapType("circle"                  , NpgsqlProviderAdapter.NpgsqlDbType.Circle);
			// network address types
			mapType("cidr"                    , NpgsqlProviderAdapter.NpgsqlDbType.Cidr);
			mapType("inet"                    , NpgsqlProviderAdapter.NpgsqlDbType.Inet);
			mapType("macaddr"                 , NpgsqlProviderAdapter.NpgsqlDbType.MacAddr);
			mapType("macaddr8"                , NpgsqlProviderAdapter.NpgsqlDbType.MacAddr8);
			// bit string types
			mapType("bit"                     , NpgsqlProviderAdapter.NpgsqlDbType.Bit);
			mapType("bit varying"             , NpgsqlProviderAdapter.NpgsqlDbType.Varbit);
			// text search types
			mapType("tsvector"                , NpgsqlProviderAdapter.NpgsqlDbType.TsVector);
			mapType("tsquery"                 , NpgsqlProviderAdapter.NpgsqlDbType.TsQuery);
			// UUID type
			mapType("uuid"                    , NpgsqlProviderAdapter.NpgsqlDbType.Uuid);
			// XML type
			mapType("xml"                     , NpgsqlProviderAdapter.NpgsqlDbType.Xml);
			// JSON types
			mapType("json"                    , NpgsqlProviderAdapter.NpgsqlDbType.Json);
			mapType("jsonb"                   , NpgsqlProviderAdapter.NpgsqlDbType.Jsonb);
			// Object Identifier Types (only supported by npgsql)
			mapType("oid"                     , NpgsqlProviderAdapter.NpgsqlDbType.Oid);
			mapType("regtype"                 , NpgsqlProviderAdapter.NpgsqlDbType.Regtype);
			mapType("xid"                     , NpgsqlProviderAdapter.NpgsqlDbType.Xid);
			mapType("xid8"                    , NpgsqlProviderAdapter.NpgsqlDbType.Xid8);
			mapType("cid"                     , NpgsqlProviderAdapter.NpgsqlDbType.Cid);
			mapType("tid"                     , NpgsqlProviderAdapter.NpgsqlDbType.Tid);
			// other types
			mapType("citext"                  , NpgsqlProviderAdapter.NpgsqlDbType.Citext);
			mapType("hstore"                  , NpgsqlProviderAdapter.NpgsqlDbType.Hstore);
			mapType("refcursor"               , NpgsqlProviderAdapter.NpgsqlDbType.Refcursor);
			mapType("oidvector"               , NpgsqlProviderAdapter.NpgsqlDbType.Oidvector);
			mapType("int2vector"              , NpgsqlProviderAdapter.NpgsqlDbType.Int2Vector);
			// ranges
			mapType("int4range"               , NpgsqlProviderAdapter.NpgsqlDbType.IntegerRange);
			mapType("int8range"               , NpgsqlProviderAdapter.NpgsqlDbType.BigIntRange);
			mapType("numrange"                , NpgsqlProviderAdapter.NpgsqlDbType.NumericRange);
			mapType("tsrange"                 , NpgsqlProviderAdapter.NpgsqlDbType.TimestampRange);
			mapType("tstzrange"               , NpgsqlProviderAdapter.NpgsqlDbType.TimestampTzRange);
			mapType("daterange"               , NpgsqlProviderAdapter.NpgsqlDbType.DateRange);
			// multi-ranges
			mapType("int4multirange"          , NpgsqlProviderAdapter.NpgsqlDbType.IntegerMultirange);
			mapType("int8multirange"          , NpgsqlProviderAdapter.NpgsqlDbType.BigIntMultirange);
			mapType("nummultirange"           , NpgsqlProviderAdapter.NpgsqlDbType.NumericMultirange);
			mapType("tsmultirange"            , NpgsqlProviderAdapter.NpgsqlDbType.TimestampMultirange);
			mapType("tstzmultirange"          , NpgsqlProviderAdapter.NpgsqlDbType.TimestampTzMultirange);
			mapType("datemultirange"          , NpgsqlProviderAdapter.NpgsqlDbType.DateMultirange);


			if (Adapter.NpgsqlTimeSpanType != null) SetProviderField(Adapter.NpgsqlTimeSpanType, Adapter.NpgsqlTimeSpanType, Adapter.GetIntervalReaderMethod!    , dataReaderType: Adapter.DataReaderType);
			if (Adapter.NpgsqlDateTimeType != null) SetProviderField(Adapter.NpgsqlDateTimeType, Adapter.NpgsqlDateTimeType, Adapter.GetTimeStampReaderMethod!   , dataReaderType: Adapter.DataReaderType);
			if (Adapter.NpgsqlDateType     != null) SetProviderField(Adapter.NpgsqlDateType    , Adapter.NpgsqlDateType    , Adapter.GetDateReaderMethod!        , dataReaderType: Adapter.DataReaderType);
			if (Adapter.NpgsqlCidrType     != null) SetProviderField(Adapter.NpgsqlCidrType    , Adapter.NpgsqlCidrType    , GetProviderSpecificValueReaderMethod, dataReaderType: Adapter.DataReaderType);

			if (Adapter.NpgsqlIntervalType != null)
				ReaderExpressions[new ReaderInfo { ToType = Adapter.NpgsqlIntervalType }] = Adapter.NpgsqlIntervalReader!;

			SetProviderField(Adapter.NpgsqlInetType, Adapter.NpgsqlInetType, GetProviderSpecificValueReaderMethod, dataReaderType: Adapter.DataReaderType);

			bool mapType(string dbType, NpgsqlProviderAdapter.NpgsqlDbType type)
			{
				if (Adapter.IsDbTypeSupported(type))
				{
					_npgsqlTypeMap.Add(dbType, type);
					return true;
				}
				return false;
			}
		}

		protected override string? NormalizeTypeName(string? typeName)
		{
			if (typeName == null)
				return null;

			if (typeName.StartsWith("character("))
				return "character";

			return typeName;
		}

		public PostgreSQLVersion Version { get; }

		public bool HasMacAddr8 => Adapter.IsDbTypeSupported(NpgsqlProviderAdapter.NpgsqlDbType.MacAddr8);

		/// <summary>
		/// Map of canonical PostgreSQL type name to NpgsqlDbType enumeration value.
		/// This map shouldn't be used directly, you should resolve PostgreSQL types using
		/// <see cref="GetNativeType(string, bool)"/> method, which takes into account different type aliases.
		/// </summary>
		private readonly IDictionary<string, NpgsqlProviderAdapter.NpgsqlDbType> _npgsqlTypeMap = new Dictionary<string, NpgsqlProviderAdapter.NpgsqlDbType>();

		private static string GetProviderName(PostgreSQLVersion version)
		{
			return version switch
			{
				PostgreSQLVersion.v15 => ProviderName.PostgreSQL15,
				PostgreSQLVersion.v92 => ProviderName.PostgreSQL92,
				PostgreSQLVersion.v93 => ProviderName.PostgreSQL93,
				_                     => ProviderName.PostgreSQL95,
			};
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsTransactionTemporaryData |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new PostgreSQLSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new PostgreSQLSchemaProvider(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object? NormalizeTimeStamp(PostgreSQLOptions options, object? value, DbDataType dataType, NpgsqlProviderAdapter.NpgsqlDbType? npgsqlType)
		{
			if (options.NormalizeTimestampData)
			{
				// normalize DateTimeOffset values to prevent unnecessary (in this case) npgsql 6.0.0 complains
				if (value is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
					value = dto.ToUniversalTime();
				// set DateTime.Kind to expected value for timestamp and timestamptz parameters to prevent npgsql 6.0.0 complains
				else if (value is DateTime dt)
				{
					// timestamptz should have UTC Kind
					if (dataType.DataType == DataType.DateTimeOffset || npgsqlType == NpgsqlProviderAdapter.NpgsqlDbType.TimestampTZ)
					{
						if (dt.Kind != DateTimeKind.Utc)
							value = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
					}
					// timestamp should have non-UTC Kind (Unspecified used by default by npgsql)
					else if (dataType.DataType == DataType.DateTime2 || npgsqlType == NpgsqlProviderAdapter.NpgsqlDbType.Timestamp)
					{
						if (dt.Kind == DateTimeKind.Utc)
							value = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
					}
				}
			}

			return value;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is IDictionary && dataType.DataType == DataType.Undefined)
			{
				dataType = dataType.WithDataType(DataType.Dictionary);
			}
			else
			{
				value = NormalizeTimeStamp(dataConnection.Options.FindOrDefault(PostgreSQLOptions.Default), value, dataType, GetNativeType(dataType.DbType));
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			// didn't tried to detect and cleanup unnecessary type mappings, as npgsql develops rapidly and
			// it doesn't pay efforts to track changes for each version in this area
			NpgsqlProviderAdapter.NpgsqlDbType? type = null;
			switch (dataType.DataType)
			{
				case DataType.Money     : type = NpgsqlProviderAdapter.NpgsqlDbType.Money    ; break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary : type = NpgsqlProviderAdapter.NpgsqlDbType.Bytea    ; break;
				case DataType.Boolean   : type = NpgsqlProviderAdapter.NpgsqlDbType.Boolean  ; break;
				case DataType.Xml       : type = NpgsqlProviderAdapter.NpgsqlDbType.Xml      ; break;
				case DataType.Text      :
				case DataType.NText     : type = NpgsqlProviderAdapter.NpgsqlDbType.Text     ; break;
				case DataType.BitArray  : type = NpgsqlProviderAdapter.NpgsqlDbType.Bit      ; break;
				case DataType.Dictionary: type = NpgsqlProviderAdapter.NpgsqlDbType.Hstore   ; break;
				case DataType.Json      : type = NpgsqlProviderAdapter.NpgsqlDbType.Json     ; break;
				case DataType.BinaryJson: type = NpgsqlProviderAdapter.NpgsqlDbType.Jsonb    ; break;
				case DataType.Interval  : type = NpgsqlProviderAdapter.NpgsqlDbType.Interval ; break;
				case DataType.Int64     : type = NpgsqlProviderAdapter.NpgsqlDbType.Bigint   ; break;
					// address npgsql 6.0.0 mapping DateTime by default to timestamptz
				case DataType.DateTime  :
				case DataType.DateTime2 : type = NpgsqlProviderAdapter.NpgsqlDbType.Timestamp; break;
					// npgsql 6.0.0 changed some DbType <-> NpgsqlDbType mappings
					// while it doesn't look like having any impact on queries
					// it makes sense to hint more precise types when we know that npgsql use less precise type
					//
					// Npgsql default was: NpgsqlDbType.Text
				case DataType.NChar     :
				case DataType.Char      : type = NpgsqlProviderAdapter.NpgsqlDbType.Char     ; break;
				case DataType.NVarChar  :
				case DataType.VarChar   : type = NpgsqlProviderAdapter.NpgsqlDbType.Varchar  ; break;
			}

			if (!string.IsNullOrEmpty(dataType.DbType))
			{
				type = GetNativeType(dataType.DbType);
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
				case DataType.SByte     : parameter.DbType = DbType.Int16     ; return;
				case DataType.UInt16    : parameter.DbType = DbType.Int32     ; return;
				case DataType.UInt32    : parameter.DbType = DbType.Int64     ; return;
				case DataType.UInt64    :
				case DataType.VarNumeric: parameter.DbType = DbType.Decimal   ; return;
				case DataType.DateTime2 : parameter.DbType = DbType.DateTime  ; return;
				// fallback mappings
				case DataType.Money     : parameter.DbType = DbType.Currency  ; break;
				case DataType.Xml       : parameter.DbType = DbType.Xml       ; break;
				case DataType.Text      : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText     : parameter.DbType = DbType.String    ; break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary : parameter.DbType = DbType.Binary    ; break;
				// those types doesn't have fallback DbType
				case DataType.BitArray  : parameter.DbType = DbType.Binary    ; break;
				case DataType.Dictionary: parameter.DbType = DbType.Object    ; break;
				case DataType.Json      : parameter.DbType = DbType.String    ; break;
				case DataType.BinaryJson: parameter.DbType = DbType.String    ; break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new PostgreSQLBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(PostgreSQLOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new PostgreSQLBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(PostgreSQLOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new PostgreSQLBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(PostgreSQLOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion

		/// <summary>
		/// Returns <see cref="NpgsqlProviderAdapter.NpgsqlDbType"/> enumeration value for requested postgresql type or null if type cannot be resolved.
		/// This method expects correct PostgreSQL type as input.
		/// Custom types not supported. Also could fail on some types as PostgreSQL have a lot of ways to write same
		/// type.
		/// </summary>
		/// <remarks>
		/// Returned value could be invalid for <see cref="NpgsqlProviderAdapter.NpgsqlDbType"/> type, if range/array flags
		/// were applied. Don't try to use results of this function for anything except passing it directly to npgsql.
		/// </remarks>
		internal NpgsqlProviderAdapter.NpgsqlDbType? GetNativeType(string? dbType, bool convertAlways = false)
		{
			if (string.IsNullOrWhiteSpace(dbType))
				return null;

			dbType = dbType!.ToLowerInvariant();

			// detect arrays
			var isArray = false;
			var idx = dbType.IndexOf("array");

			if (idx == -1)
				idx = dbType.IndexOf("[");

			if (idx != -1)
			{
				isArray = true;
				dbType = dbType.Substring(0, idx);
			}

			var isRange      = false;
			var isMultiRange = false;

			dbType = dbType.Trim();

			// normalize synonyms and parameterized type names
			switch (dbType)
			{
				case "int4range":
					dbType  = "integer";
					isRange = true;
					break;
				case "int8range":
					dbType  = "bigint";
					isRange = true;
					break;
				case "numrange":
					dbType  = "numeric";
					isRange = true;
					break;
				case "tsrange":
					dbType  = "timestamp";
					isRange = true;
					break;
				case "tstzrange":
					dbType  = "timestamp with time zone";
					isRange = true;
					break;
				case "daterange":
					dbType = "date";
					isRange = true;
					break;

				case "int4multirange":
					dbType = "integer";
					isMultiRange = true;
					break;
				case "int8multirange":
					dbType = "bigint";
					isMultiRange = true;
					break;
				case "nummultirange":
					dbType = "numeric";
					isMultiRange = true;
					break;
				case "tsmultirange":
					dbType = "timestamp";
					isMultiRange = true;
					break;
				case "tstzmultirange":
					dbType = "timestamp with time zone";
					isMultiRange = true;
					break;
				case "datemultirange":
					dbType = "date";
					isMultiRange = true;
					break;

				case "timestamptz":
					dbType = "timestamp with time zone";
					break;
				case "int2":
				case "smallserial":
				case "serial2":
					dbType = "smallint";
					break;
				case "int":
				case "int4":
				case "serial":
				case "serial4":
					dbType = "integer";
					break;
				case "int8":
				case "bigserial":
				case "serial8":
					dbType = "bigint";
					break;
				case "float":
					dbType = "double precision";
					break;
				case "varchar":
					dbType = "character varying";
					break;
				case "varbit":
					dbType = "bit varying";
					break;
			}

			if (dbType.StartsWith("float(") && dbType.EndsWith(")"))
			{
				if (int.TryParse(dbType.Substring("float(".Length, dbType.Length - "float(".Length - 1), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var precision))
				{
					if (precision >= 1 && precision <= 24)
						dbType = "real";
					else if (precision >= 25 && precision <= 53)
						dbType = "real";
					// else bad type
				}
			}

			if (dbType.StartsWith("numeric(") || dbType.StartsWith("decimal"))
				dbType = "numeric";

			if (dbType.StartsWith("varchar(") || dbType.StartsWith("character varying("))
				dbType = "character varying";

			if (dbType.StartsWith("char(") || dbType.StartsWith("character("))
				dbType = "character";

			if (dbType.StartsWith("interval"))
				dbType = "interval";

			if (dbType.StartsWith("timestamp"))
				dbType = dbType.Contains("with time zone") ? "timestamp with time zone" : "timestamp";

			if (dbType.StartsWith("time(") || dbType.StartsWith("time "))
				dbType = dbType.Contains("with time zone") ? "time with time zone" : "time";

			if (dbType.StartsWith("bit("))
				dbType = "bit";

			if (dbType.StartsWith("bit varying("))
				dbType = "bit varying";

			if (_npgsqlTypeMap.TryGetValue(dbType, out var result))
			{
				// because NpgsqlDbType fields numeric values changed in npgsql4,
				// applying flag-like array/range bits is not straightforward process
				result = Adapter.ApplyDbTypeFlags(result, isArray, isRange, isMultiRange, convertAlways);

				return result;
			}

			return null;
		}

		static MappingSchema GetMappingSchema(PostgreSQLVersion version)
		{
			return version switch
			{
				PostgreSQLVersion.v15 => new PostgreSQLMappingSchema.PostgreSQL15MappingSchema(),
				PostgreSQLVersion.v92 => new PostgreSQLMappingSchema.PostgreSQL92MappingSchema(),
				PostgreSQLVersion.v93 => new PostgreSQLMappingSchema.PostgreSQL93MappingSchema(),
				_                     => new PostgreSQLMappingSchema.PostgreSQL95MappingSchema(),
			};
		}
	}
}
