using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class OracleDataProvider : DynamicDataProviderBase<OracleProviderAdapter>
	{
		public OracleDataProvider(string name) : this(name, OracleVersion.v12)
		{ }

		public OracleDataProvider(string name, OracleVersion version)
			: base(
				name,
				GetMappingSchema(name, OracleProviderAdapter.GetInstance(name).MappingSchema),
				OracleProviderAdapter.GetInstance(name))
		{
			Version = version;

			//SqlProviderFlags.IsCountSubQuerySupported        = false;
			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel   = IsolationLevel.ReadCommitted;

			if (version >= OracleVersion.v12)
			{
				SqlProviderFlags.IsApplyJoinSupported          = true;
			}

			SqlProviderFlags.MaxInListValuesCount              = 1000;

			SetCharField            ("Char",  (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharField            ("NChar", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("Char",  DataTools.GetCharExpression);
			SetCharFieldToType<char>("NChar", DataTools.GetCharExpression);

			if (version == OracleVersion.v11)
				_sqlOptimizer = new Oracle11SqlOptimizer(SqlProviderFlags);
			else
				_sqlOptimizer = new Oracle12SqlOptimizer(SqlProviderFlags);

			SetProviderField(Adapter.OracleBFileType       , Adapter.OracleBFileType       , Adapter.GetOracleBFileReaderMethod       , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleBinaryType      , Adapter.OracleBinaryType      , Adapter.GetOracleBinaryReaderMethod      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleBlobType        , Adapter.OracleBlobType        , Adapter.GetOracleBlobReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleClobType        , Adapter.OracleClobType        , Adapter.GetOracleClobReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleDateType        , Adapter.OracleDateType        , Adapter.GetOracleDateReaderMethod        , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleDecimalType     , Adapter.OracleDecimalType     , Adapter.GetOracleDecimalReaderMethod     , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleIntervalDSType  , Adapter.OracleIntervalDSType  , Adapter.GetOracleIntervalDSReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleIntervalYMType  , Adapter.OracleIntervalYMType  , Adapter.GetOracleIntervalYMReaderMethod  , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleStringType      , Adapter.OracleStringType      , Adapter.GetOracleStringReaderMethod      , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleTimeStampType   , Adapter.OracleTimeStampType   , Adapter.GetOracleTimeStampReaderMethod   , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleTimeStampLTZType, Adapter.OracleTimeStampLTZType, Adapter.GetOracleTimeStampLTZReaderMethod, dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleTimeStampTZType , Adapter.OracleTimeStampTZType , Adapter.GetOracleTimeStampTZReaderMethod , dataReaderType: Adapter.DataReaderType);
			SetProviderField(Adapter.OracleXmlTypeType     , Adapter.OracleXmlTypeType     , Adapter.GetOracleXmlTypeReaderMethod     , dataReaderType: Adapter.DataReaderType);

			// native provider only
			if (Adapter.OracleRefType != null)
				SetProviderField(Adapter.OracleRefType     , Adapter.OracleRefType         , Adapter.GetOracleRefReaderMethod!        , dataReaderType: Adapter.DataReaderType);

			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = Adapter.OracleTimeStampTZType, DataReaderType = Adapter.DataReaderType }]
				= Adapter.ReadDateTimeOffsetFromOracleTimeStampTZ;

			ReaderExpressions[new ReaderInfo { ToType = typeof(decimal),        ProviderFieldType = Adapter.OracleDecimalType,      DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimalAdv;
			ReaderExpressions[new ReaderInfo { ToType = typeof(decimal),        FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimalAdv;
			ReaderExpressions[new ReaderInfo { ToType = typeof(int),            FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToInt;
			ReaderExpressions[new ReaderInfo { ToType = typeof(long),           FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToLong;
			ReaderExpressions[new ReaderInfo {                                  FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimal;
			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = Adapter.OracleTimeStampLTZType, DataReaderType = Adapter.DataReaderType }] = Adapter.ReadDateTimeOffsetFromOracleTimeStampLTZ;
		}

		public OracleVersion Version { get; }

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsTransactionTemporaryData |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return Version switch
			{
				OracleVersion.v11 => new Oracle11SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
				_                 => new Oracle12SqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags),
			};
		}

		private static MappingSchema GetMappingSchema(string name, MappingSchema providerSchema)
		{
			return name switch
			{
				ProviderName.OracleNative => new OracleMappingSchema.NativeMappingSchema(providerSchema),
				_                         => new OracleMappingSchema.ManagedMappingSchema(providerSchema),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider() => new OracleSchemaProvider(this);

		public override DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			command = base.InitCommand(dataConnection, command, commandType, commandText, parameters, withParameters);

			var rawCommand = TryGetProviderCommand(command, dataConnection.MappingSchema);

			if (rawCommand != null)
			{
				// binding disabled for native provider without parameters to reduce changes to fail when SQL contains
				// parameter-like token.
				// This is mostly issue with triggers creation, because they can have record tokens like :NEW
				// incorectly identified by native provider as parameter
				var bind = Name != ProviderName.OracleNative || parameters?.Length > 0 || withParameters;
				Adapter.SetBindByName(rawCommand, bind);

				// https://docs.oracle.com/cd/B19306_01/win.102/b14307/featData.htm
				// For LONG data type fetching initialization
				Adapter.SetInitialLONGFetchSize(rawCommand, -1);

				if (parameters != null)
					foreach (var parameter in parameters)
					{
						if (parameter.IsArray
							&& parameter.Value is object[] value
							&& value.Length != 0)
						{
							Adapter.SetArrayBindCount(rawCommand, value.Length);
							break;
						}
					}
			}

			return command;
		}

		public override void ClearCommandParameters(IDbCommand command)
		{
			// both native and managed providers implement IDisposable for parameters
			if (command.Parameters.Count > 0)
			{
				foreach (DbParameter? param in command.Parameters)
				{
					if (param is IDisposable disposable)
						disposable.Dispose();
				}

				command.Parameters.Clear();
			}
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset:
					if (value is DateTimeOffset dto)
					{
						dto      = DataTools.AdjustPrecision(dto, (byte?)dataType.Precision ?? 6);
						var zone = (dto.Offset < TimeSpan.Zero ? "-" : "+") + dto.Offset.ToString("hh\\:mm");
						value    = Adapter.CreateOracleTimeStampTZ(dto, zone);
					}
					break;
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool boolValue)
						value = boolValue ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value is Guid guid) value = guid.ToByteArray();
					break;
				case DataType.Time:
					// According to http://docs.oracle.com/cd/E16655_01/win.121/e17732/featOraCommand.htm#ODPNT258
					// Inference of DbType and OracleDbType from Value: TimeSpan - Object - IntervalDS
					if (value is TimeSpan)
						dataType = dataType.WithDataType(DataType.Undefined);
					break;
				case DataType.BFile:
					{
						// TODO: BFile we do not support setting parameter value
						value = null;
						break;
					}
				case DataType.DateTime :
					{
						if (value is DateTime dt)
							value = DataTools.AdjustPrecision(dt, 0);
						break;
					}
				case DataType.DateTime2:
					{
						if (value is DateTime dt)
							value = DataTools.AdjustPrecision(dt, (byte?)dataType.Precision ?? 6);
						break;
					}
			}

			if (dataType.DataType == DataType.Undefined && value is string @string && @string.Length >= 4000)
			{
				dataType = dataType.WithDataType(DataType.NText);
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset : if (type == typeof(DateTimeOffset)) return Adapter.OracleTimeStampTZType; break;
				case DataType.Boolean        : if (type == typeof(bool))           return typeof(byte);                  break;
				case DataType.Guid           : if (type == typeof(Guid))           return typeof(byte[]);                break;
				case DataType.Int16          : if (type == typeof(bool))           return typeof(short);                 break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			OracleProviderAdapter.OracleDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.BFile    : type = OracleProviderAdapter.OracleDbType.BFile       ; break;
				case DataType.Xml      : type = OracleProviderAdapter.OracleDbType.XmlType     ; break;
				case DataType.Single   : type = OracleProviderAdapter.OracleDbType.BinaryFloat ; break;
				case DataType.Double   : type = OracleProviderAdapter.OracleDbType.BinaryDouble; break;
				case DataType.Text     : type = OracleProviderAdapter.OracleDbType.Clob        ; break;
				case DataType.NText    : type = OracleProviderAdapter.OracleDbType.NClob       ; break;
				case DataType.Image    :
				case DataType.Binary   :
				case DataType.VarBinary: type = OracleProviderAdapter.OracleDbType.Blob        ; break;
				case DataType.Cursor   : type = OracleProviderAdapter.OracleDbType.RefCursor   ; break;
				case DataType.NVarChar : type = OracleProviderAdapter.OracleDbType.NVarchar2   ; break;
				case DataType.Long     : type = OracleProviderAdapter.OracleDbType.Long        ; break;
				case DataType.LongRaw  : type = OracleProviderAdapter.OracleDbType.LongRaw     ; break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				case DataType.Byte          :
				case DataType.SByte         : parameter.DbType = DbType.Int16;       break;
				case DataType.UInt16        : parameter.DbType = DbType.Int32;       break;
				case DataType.UInt32        : parameter.DbType = DbType.Int64;       break;
				case DataType.UInt64        :
				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;     break;
				case DataType.SmallDateTime : parameter.DbType = DbType.Date;        break;
				case DataType.DateTime2     : parameter.DbType = DbType.DateTime;    break;
				case DataType.Guid          : parameter.DbType = DbType.Binary;      break;
				case DataType.VarChar       : parameter.DbType = DbType.String;      break;

				// fallback (probably)
				case DataType.NVarChar      :
				case DataType.Text          :
				case DataType.NText         : parameter.DbType = DbType.String;      break;
				case DataType.Long          :
				case DataType.LongRaw       :
				case DataType.Image         :
				case DataType.Binary        :
				case DataType.Cursor        : parameter.DbType = DbType.Binary;      break;
				case DataType.BFile         : parameter.DbType = DbType.Binary;      break;
				case DataType.Xml           : parameter.DbType = DbType.String;      break;

				default: base.SetParameterType(dataConnection, parameter, dataType); break;
			}
		}

		#region BulkCopy

		OracleBulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new OracleBulkCopy(this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? OracleTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new OracleBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? OracleTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if !NETFRAMEWORK
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new OracleBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? OracleTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
