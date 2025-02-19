using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle.Translation;
using LinqToDB.Extensions;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Oracle
{
	sealed class OracleDataProviderNative11  : OracleDataProvider { public OracleDataProviderNative11()  : base(ProviderName.Oracle11Native , OracleProvider.Native,  OracleVersion.v11) {} }
	sealed class OracleDataProviderNative12  : OracleDataProvider { public OracleDataProviderNative12()  : base(ProviderName.OracleNative   , OracleProvider.Native,  OracleVersion.v12) {} }
	sealed class OracleDataProviderDevart11  : OracleDataProvider { public OracleDataProviderDevart11()  : base(ProviderName.Oracle11Devart , OracleProvider.Devart,  OracleVersion.v11) {} }
	sealed class OracleDataProviderDevart12  : OracleDataProvider { public OracleDataProviderDevart12()  : base(ProviderName.OracleDevart   , OracleProvider.Devart,  OracleVersion.v12) {} }
	sealed class OracleDataProviderManaged11 : OracleDataProvider { public OracleDataProviderManaged11() : base(ProviderName.Oracle11Managed, OracleProvider.Managed, OracleVersion.v11) {} }
	sealed class OracleDataProviderManaged12 : OracleDataProvider { public OracleDataProviderManaged12() : base(ProviderName.OracleManaged  , OracleProvider.Managed, OracleVersion.v12) {} }

	public abstract class OracleDataProvider : DynamicDataProviderBase<OracleProviderAdapter>
	{
		protected OracleDataProvider(string name, OracleProvider provider, OracleVersion version)
			: base(name, GetMappingSchema(provider, version), OracleProviderAdapter.GetInstance(provider))
		{
			Provider = provider;
			Version  = version;

			SqlProviderFlags.IsIdentityParameterRequired                           = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported                     = true;
			SqlProviderFlags.IsSubQueryOrderBySupported                            = true;
			SqlProviderFlags.IsUpdateFromSupported                                 = false;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel                       = IsolationLevel.ReadCommitted;
			SqlProviderFlags.IsNamingQueryBlockSupported                           = true;
			SqlProviderFlags.IsRowNumberWithoutOrderBySupported                    = false;
			SqlProviderFlags.IsSubqueryWithParentReferenceInJoinConditionSupported = false;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel                    = 1;
			SqlProviderFlags.IsColumnSubqueryShouldNotContainParentIsNotNull       = true;
			SqlProviderFlags.IsColumnSubqueryWithParentReferenceAndTakeSupported   = version >= OracleVersion.v12;
			SqlProviderFlags.IsDistinctFromSupported                               = true;
			SqlProviderFlags.DoesProviderTreatsEmptyStringAsNull                   = true;
			// TODO: retest for ORA 23
			SqlProviderFlags.SupportsBooleanType                                   = false;

			SqlProviderFlags.RowConstructorSupport = RowFeature.Equality | RowFeature.CompareToSelect | RowFeature.In |
			                                         RowFeature.Update   | RowFeature.Overlaps;

			if (version >= OracleVersion.v12)
				SqlProviderFlags.IsApplyJoinSupported          = true;

			SqlProviderFlags.MaxInListValuesCount              = 1000;

			SetCharField            ("Char",  (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharField            ("NChar", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("Char",  DataTools.GetCharExpression);
			SetCharFieldToType<char>("NChar", DataTools.GetCharExpression);

			if (version == OracleVersion.v11)
				_sqlOptimizer = new Oracle11SqlOptimizer(SqlProviderFlags);
			else
				_sqlOptimizer = new Oracle12SqlOptimizer(SqlProviderFlags);

			foreach (var (type, method) in Adapter.CustomReaders)
				SetProviderField(type, type, method, dataReaderType: Adapter.DataReaderType);

			if (Adapter.OracleTimeStampTZType != null)
				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = Adapter.OracleTimeStampTZType, DataReaderType = Adapter.DataReaderType }] = Adapter.ReadDateTimeOffsetFromOracleTimeStampTZ;
			else
				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = Adapter.OracleTimeStampType, DataReaderType = Adapter.DataReaderType, DataTypeName = "TIMESTAMP WITH TIME ZONE" }] = Adapter.ReadDateTimeOffsetFromOracleTimeStampTZ;

			if (Adapter.ReadDateTimeOffsetFromOracleTimeStamp != null)
				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = Adapter.OracleTimeStampType, DataReaderType = Adapter.DataReaderType, DataTypeName = "TIMESTAMP" }] = Adapter.ReadDateTimeOffsetFromOracleTimeStamp;

			if (Adapter.ReadOracleDecimalToDecimalAdv != null)
			{
				ReaderExpressions[new ReaderInfo { ToType = typeof(decimal), ProviderFieldType = Adapter.OracleDecimalType, DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimalAdv;
				ReaderExpressions[new ReaderInfo { ToType = typeof(decimal), FieldType = typeof(decimal), DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimalAdv;
			}

			if (Adapter.ReadOracleDecimalToInt != null)
				ReaderExpressions[new ReaderInfo { ToType = typeof(int),            FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToInt;
			if (Adapter.ReadOracleDecimalToLong != null)
				ReaderExpressions[new ReaderInfo { ToType = typeof(long),           FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToLong;
			if (Adapter.ReadOracleDecimalToDecimal != null)
				ReaderExpressions[new ReaderInfo {                                  FieldType = typeof(decimal),                        DataReaderType = Adapter.DataReaderType }] = Adapter.ReadOracleDecimalToDecimal;
		}

		public OracleProvider Provider { get; }
		public OracleVersion  Version  { get; }

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsTransactionTemporaryData |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new OracleMemberTranslator();
		}

		protected override IIdentifierService CreateIdentifierService()
		{
			return new IdentifierServiceSimple(Version <= OracleVersion.v11 ? 30 : 128);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return Version switch
			{
				OracleVersion.v11 => new Oracle11SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				_                 => new Oracle12SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
			};
		}

		private static MappingSchema GetMappingSchema(OracleProvider provider, OracleVersion version)
		{
			return (provider, version) switch
			{
				(OracleProvider.Native, OracleVersion.v11)  => new OracleMappingSchema.Native11MappingSchema(),
				(OracleProvider.Native, OracleVersion.v12)  => new OracleMappingSchema.NativeMappingSchema(),
				(OracleProvider.Devart, OracleVersion.v11)  => new OracleMappingSchema.Devart11MappingSchema(),
				(OracleProvider.Devart, OracleVersion.v12)  => new OracleMappingSchema.DevartMappingSchema(),
				(OracleProvider.Managed, OracleVersion.v11) => new OracleMappingSchema.Managed11MappingSchema(),
				_                                           => new OracleMappingSchema.ManagedMappingSchema(),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider() => new OracleSchemaProvider(this);

		public override DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			command = base.InitCommand(dataConnection, command, commandType, commandText, parameters, withParameters);

			if (TryGetProviderCommand(dataConnection, command) is { } rawCommand)
			{
				// binding disabled for native provider without parameters to reduce chances to fail when SQL contains
				// parameter-like token.
				// This is mostly issue with triggers creation, because they can have record tokens like :NEW
				// incorrectly identified by native provider as parameter
				var bind = !Adapter.BindingByNameEnabled || parameters?.Length > 0 || withParameters;
				Adapter.SetBindByName(rawCommand, bind);

				// https://docs.oracle.com/cd/B19306_01/win.102/b14307/featData.htm
				// For LONG data type fetching initialization
				Adapter.SetInitialLONGFetchSize?.Invoke(rawCommand, -1);

				// Always reset SetArrayBindCount, in case it was modified by a previous command.
				// When using array bindings, _all_ parameters must be arrays and have the same length.
				if (Adapter.SetArrayBindCount != null)
				{
					int arrayBindCount = parameters?.Length > 0 && parameters[0] is { IsArray: true, Value: object[] value }
						? value.Length
						: 0;

					Adapter.SetArrayBindCount(rawCommand, arrayBindCount);
				}
			}

			return command;
		}

		public override void ClearCommandParameters(DbCommand command)
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

		public override IQueryParametersNormalizer GetQueryParameterNormalizer()
		{
			// TODO: versioning support
			// currently we cannot enable Oracle122ParametersNormalizer
			// as we don't have Version for Oracle 12.2 or higher
			// also see https://github.com/linq2db/linq2db/issues/4219
			return new Oracle11ParametersNormalizer();
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset:
					if (value is DateTimeOffset dto)
					{
						dto      = dto.WithPrecision(dataType.Precision ?? 6);
						var zone = (dto.Offset < TimeSpan.Zero ? "-" : "+") + dto.Offset.ToString("hh\\:mm", DateTimeFormatInfo.InvariantInfo);
						value    = Adapter.CreateOracleTimeStampTZ(dto, zone);
					}

					break;

				case DataType.Boolean  :
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool boolValue)
						value = boolValue ? (byte)1 : (byte)0;
					break;

				case DataType.Guid     :
				case DataType.Binary   :
				case DataType.VarBinary:
				case DataType.Blob     :
				case DataType.Image    :
					// https://github.com/linq2db/linq2db/issues/3207
					if (value is Guid guid) value = guid.ToByteArray();
					break;

				case DataType.Time     :
					// According to http://docs.oracle.com/cd/E16655_01/win.121/e17732/featOraCommand.htm#ODPNT258
					// Inference of DbType and OracleDbType from Value: TimeSpan - Object - IntervalDS
					if (value is TimeSpan)
						dataType = dataType.WithDataType(DataType.Undefined);
					break;

				case DataType.BFile    :
					// TODO: BFile we do not support setting parameter value
					value = null;
					break;

				case DataType.DateTime :
				{
					if (value is DateTime dt)
						value = dt.WithPrecision(0);
					break;
				}

				case DataType.DateTime2:
				{
					if (value is DateTime dt)
						value = dt.WithPrecision(dataType.Precision ?? 6);
					break;
				}

#if NET6_0_OR_GREATER
				case DataType.Date     :
					if (value is DateOnly d)
						value = d.ToDateTime(TimeOnly.MinValue);
					break;
#endif
			}

			if (dataType.DataType == DataType.Undefined && value is string @string && @string.Length >= 4000)
				dataType = dataType.WithDataType(DataType.NText);

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset : if (type == typeof(DateTimeOffset)) return Adapter.OracleTimeStampTZType ?? Adapter.OracleTimeStampType; break;
				case DataType.Boolean        : if (type == typeof(bool))           return typeof(byte);                                                 break;
				case DataType.Guid           : if (type == typeof(Guid))           return typeof(byte[]);                                               break;
				case DataType.Int16          : if (type == typeof(bool))           return typeof(short);                                                break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
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
				case DataType.Blob     : type = OracleProviderAdapter.OracleDbType.Blob        ; break;
				case DataType.Binary   :
				case DataType.VarBinary: type = (dataType.Length ?? 0) == 0
						? OracleProviderAdapter.OracleDbType.Blob
						: OracleProviderAdapter.OracleDbType.Raw;
					break;
				case DataType.Cursor   : type = OracleProviderAdapter.OracleDbType.RefCursor   ; break;
				case DataType.NVarChar : type = OracleProviderAdapter.OracleDbType.NVarchar2   ; break;
				case DataType.Long     : type = OracleProviderAdapter.OracleDbType.Long        ; break;
				case DataType.LongRaw  : type = OracleProviderAdapter.OracleDbType.LongRaw     ; break;
				case DataType.Json     : type = OracleProviderAdapter.OracleDbType.Json        ; break;
				case DataType.Guid     : type = OracleProviderAdapter.OracleDbType.Raw         ; break;
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

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			var oracleOptions = options.FindOrDefault(OracleOptions.Default);
			var bulkCopy      = new OracleBulkCopy(this, oracleOptions.AlternativeBulkCopy);

			return bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					oracleOptions.BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var oracleOptions = options.FindOrDefault(OracleOptions.Default);
			var bulkCopy      = new OracleBulkCopy(this, oracleOptions.AlternativeBulkCopy);

			return bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					oracleOptions.BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var extension = options.FindOrDefault(OracleOptions.Default);
			var bulkCopy  = new OracleBulkCopy(this, extension.AlternativeBulkCopy);

			return bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					extension.BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
