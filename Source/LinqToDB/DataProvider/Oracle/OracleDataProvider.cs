using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;
	using Tools;

	public class OracleDataProvider : DynamicDataProviderBase
	{
		public OracleDataProvider(string name)
			: base(name, null!)
		{
			//SqlProviderFlags.IsCountSubQuerySupported        = false;
			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SqlProviderFlags.MaxInListValuesCount              = 1000;

			SetCharField            ("Char",  (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharField            ("NChar", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("Char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("NChar", (r, i) => DataTools.GetChar(r, i));

			_sqlOptimizer = new OracleSqlOptimizer(SqlProviderFlags);

			Wrapper = new Lazy<OracleWrappers.IOracleWrapper>(() => Initialize(), true);
		}

		internal readonly Lazy<OracleWrappers.IOracleWrapper> Wrapper;

		private OracleWrappers.IOracleWrapper Initialize()
		{
			var wrapper = OracleWrappers.Initialize(this);

			SetProviderField(wrapper.OracleBFileType       , wrapper.OracleBFileType       , "GetOracleBFile"       , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleBinaryType      , wrapper.OracleBinaryType      , "GetOracleBinary"      , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleBlobType        , wrapper.OracleBlobType        , "GetOracleBlob"        , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleClobType        , wrapper.OracleClobType        , "GetOracleClob"        , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleDateType        , wrapper.OracleDateType        , "GetOracleDate"        , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleDecimalType     , wrapper.OracleDecimalType     , "GetOracleDecimal"     , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleIntervalDSType  , wrapper.OracleIntervalDSType  , "GetOracleIntervalDS"  , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleIntervalYMType  , wrapper.OracleIntervalYMType  , "GetOracleIntervalYM"  , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleStringType      , wrapper.OracleStringType      , "GetOracleString"      , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleTimeStampType   , wrapper.OracleTimeStampType   , "GetOracleTimeStamp"   , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleTimeStampLTZType, wrapper.OracleTimeStampLTZType, "GetOracleTimeStampLTZ", dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleTimeStampTZType , wrapper.OracleTimeStampTZType , "GetOracleTimeStampTZ" , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.OracleXmlTypeType     , wrapper.OracleXmlTypeType     , "GetOracleXmlType"     , dataReaderType: wrapper.DataReaderType);

			// native provider only
			if (wrapper.OracleRefType != null)
				SetProviderField(wrapper.OracleRefType     , wrapper.OracleRefType     , "GetOracleRef"    , dataReaderType: wrapper.DataReaderType);

			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = wrapper.OracleTimeStampTZType, DataReaderType = wrapper.DataReaderType }]
				= wrapper.ReadDateTimeOffsetFromOracleTimeStampTZ;

			ReaderExpressions[new ReaderInfo { ToType = typeof(decimal),        ProviderFieldType = wrapper.OracleDecimalType,      DataReaderType = wrapper.DataReaderType }] = wrapper.ReadOracleDecimalToDecimalAdv;
			ReaderExpressions[new ReaderInfo { ToType = typeof(decimal),        FieldType = typeof(decimal),                        DataReaderType = wrapper.DataReaderType }] = wrapper.ReadOracleDecimalToDecimalAdv;
			ReaderExpressions[new ReaderInfo { ToType = typeof(int),            FieldType = typeof(decimal),                        DataReaderType = wrapper.DataReaderType }] = wrapper.ReadOracleDecimalToInt;
			ReaderExpressions[new ReaderInfo { ToType = typeof(long),           FieldType = typeof(decimal),                        DataReaderType = wrapper.DataReaderType }] = wrapper.ReadOracleDecimalToLong;
			ReaderExpressions[new ReaderInfo {                                  FieldType = typeof(decimal),                        DataReaderType = wrapper.DataReaderType }] = wrapper.ReadOracleDecimalToDecimal;
			ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = wrapper.OracleTimeStampLTZType, DataReaderType = wrapper.DataReaderType }] = wrapper.ReadDateTimeOffsetFromOracleTimeStampLTZ;

			return wrapper;
		}

#if !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override string? DbFactoryProviderName => Name == ProviderName.OracleNative ? "Oracle.DataAccess.Client" : null;
#endif
		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		public string AssemblyName => Name == ProviderName.OracleNative ? "Oracle.DataAccess" : "Oracle.ManagedDataAccess";

		public    override string ConnectionNamespace => $"{AssemblyName}.Client";
		protected override string ConnectionTypeName  => $"{AssemblyName}.Client.OracleConnection, {AssemblyName}";
		protected override string DataReaderTypeName  => $"{AssemblyName}.Client.OracleDataReader, {AssemblyName}";

		// TODO: remove? both managed and unmanaged providers support it
		public             bool   IsXmlTypeSupported  => Wrapper.Value.OracleXmlTypeType != null;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new OracleSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly OracleMappingSchema.NativeMappingSchema  NativeMappingSchema  = new OracleMappingSchema.NativeMappingSchema();
			public static readonly OracleMappingSchema.ManagedMappingSchema ManagedMappingSchema = new OracleMappingSchema.ManagedMappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.OracleNative
			? MappingSchemaInstance.NativeMappingSchema as MappingSchema
			: MappingSchemaInstance.ManagedMappingSchema;

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider() => new OracleSchemaProvider(this);

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			dataConnection.DisposeCommand();

			var command = TryConvertCommand(Wrapper.Value.CommandType, dataConnection.Command, dataConnection.MappingSchema);

			if (command != null)
			{
				// binding disabled for native provider without parameters to reduce changes to fail when SQL contains
				// parameter-like token.
				// This is mostly issue with triggers creation, because they can have record tokens like :NEW
				// incorectly identified by native provider as parameter
				var bind = Name != ProviderName.OracleNative || parameters?.Length > 0 || withParameters;
				Wrapper.Value.BindByNameSetter(command, bind);
			}

			base.InitCommand(dataConnection, commandType, commandText, parameters, withParameters);

			if (command != null)
			{
				// https://docs.oracle.com/cd/B19306_01/win.102/b14307/featData.htm
				// For LONG data type fetching initialization
				Wrapper.Value.InitialLONGFetchSizeSetter(command, -1);

				if (parameters != null)
					foreach (var parameter in parameters)
					{
						if (parameter.IsArray && parameter.Value is object[])
						{
							var value = (object[])parameter.Value;

							if (value.Length != 0)
							{
								Wrapper.Value.ArrayBindCountSetter(command, value.Length);

								break;
							}
						}
					}
			}
		}

		public override void DisposeCommand(DataConnection dataConnection)
		{
			foreach (DbParameter param in dataConnection.Command.Parameters)
			{
				if (param is IDisposable disposable)
					disposable.Dispose();
			}

			base.DisposeCommand(dataConnection);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.DateTimeOffset:
					if (value is DateTimeOffset dto)
					{
						var zone = (dto.Offset < TimeSpan.Zero ? "-" : "+") + dto.Offset.ToString("hh\\:mm");
						value    = Wrapper.Value.CreateOracleTimeStampTZ(dto, zone);
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
			}

			if (dataType.DataType == DataType.Undefined && value is string && ((string)value).Length >= 4000)
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
				case DataType.DateTimeOffset : if (type == typeof(DateTimeOffset)) return Wrapper.Value.OracleTimeStampTZType; break;
				case DataType.Boolean        : if (type == typeof(bool))           return typeof(byte);                        break;
				case DataType.Guid           : if (type == typeof(Guid))           return typeof(byte[]);                      break;
				case DataType.Int16          : if (type == typeof(bool))           return typeof(short);                       break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			OracleWrappers.OracleDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.BFile    : type = OracleWrappers.OracleDbType.BFile       ; break;
				case DataType.Xml      : type = OracleWrappers.OracleDbType.XmlType     ; break;
				case DataType.Single   : type = OracleWrappers.OracleDbType.BinaryFloat ; break;
				case DataType.Double   : type = OracleWrappers.OracleDbType.BinaryDouble; break;
				case DataType.Text     : type = OracleWrappers.OracleDbType.Clob        ; break;
				case DataType.NText    : type = OracleWrappers.OracleDbType.NClob       ; break;
				case DataType.Image    :
				case DataType.Binary   :
				case DataType.VarBinary: type = OracleWrappers.OracleDbType.Blob        ; break;
				case DataType.Cursor   : type = OracleWrappers.OracleDbType.RefCursor   ; break;
				case DataType.NVarChar : type = OracleWrappers.OracleDbType.NVarchar2   ; break;
				case DataType.Long     : type = OracleWrappers.OracleDbType.Long        ; break;
				case DataType.LongRaw  : type = OracleWrappers.OracleDbType.LongRaw     ; break;
			}

			if (type != null)
			{
				var param = TryConvertParameter(Wrapper.Value.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Wrapper.Value.TypeSetter(param, type.Value);
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

#pragma warning disable 618
			if (options.RetrieveSequence)
			{
				var list = source.RetrieveIdentity((DataConnection)table.DataContext);

				if (!ReferenceEquals(list, source))
					options = new BulkCopyOptions(options) { KeepIdentity = true };

				source = list;
			}
#pragma warning restore 618

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? OracleTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
