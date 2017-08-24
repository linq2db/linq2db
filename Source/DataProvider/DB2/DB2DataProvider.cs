using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.DataProvider.DB2
{
	using Data;
	using Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class DB2DataProvider : DynamicDataProviderBase
	{
		public DB2DataProvider(string name, DB2Version version)
			: base(name, null)
		{
			Version = version;

			SqlProviderFlags.AcceptsTakeAsParameter       = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip = true;
			SqlProviderFlags.IsDistinctOrderBySupported   = version != DB2Version.zOS;

			SetCharFieldToType<char>("CHAR", (r, i) => DataTools.GetChar(r, i));

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new DB2SqlOptimizer(SqlProviderFlags);
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			DB2Types.ConnectionType = connectionType;

			DB2Types.DB2Int64.       Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Int64",        true);
			DB2Types.DB2Int32.       Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Int32",        true);
			DB2Types.DB2Int16.       Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Int16",        true);
			DB2Types.DB2Decimal.     Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Decimal",      true);
			DB2Types.DB2DecimalFloat.Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2DecimalFloat", true);
			DB2Types.DB2Real.        Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Real",         true);
			DB2Types.DB2Real370.     Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Real370",      true);
			DB2Types.DB2Double.      Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Double",       true);
			DB2Types.DB2String.      Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2String",       true);
			DB2Types.DB2Clob.        Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Clob",         true);
			DB2Types.DB2Binary.      Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Binary",       true);
			DB2Types.DB2Blob.        Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Blob",         true);
			DB2Types.DB2Date.        Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Date",         true);
			DB2Types.DB2Time.        Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Time",         true);
			DB2Types.DB2TimeStamp.   Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2TimeStamp",    true);
			DB2Types.DB2Xml               = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2Xml",          true);
			DB2Types.DB2RowId.       Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2RowId",        true);
			DB2Types.DB2DateTime.    Type = connectionType.AssemblyEx().GetType("IBM.Data.DB2Types.DB2DateTime",     false);

			SetProviderField(DB2Types.DB2Int64,        typeof(Int64),    "GetDB2Int64");
			SetProviderField(DB2Types.DB2Int32,        typeof(Int32),    "GetDB2Int32");
			SetProviderField(DB2Types.DB2Int16,        typeof(Int16),    "GetDB2Int16");
			SetProviderField(DB2Types.DB2Decimal,      typeof(Decimal),  "GetDB2Decimal");
			SetProviderField(DB2Types.DB2DecimalFloat, typeof(Decimal),  "GetDB2DecimalFloat");
			SetProviderField(DB2Types.DB2Real,         typeof(Single),   "GetDB2Real");
			SetProviderField(DB2Types.DB2Real370,      typeof(Single),   "GetDB2Real370");
			SetProviderField(DB2Types.DB2Double,       typeof(Double),   "GetDB2Double");
			SetProviderField(DB2Types.DB2String,       typeof(String),   "GetDB2String");
			SetProviderField(DB2Types.DB2Clob,         typeof(String),   "GetDB2Clob");
			SetProviderField(DB2Types.DB2Binary,       typeof(byte[]),   "GetDB2Binary");
			SetProviderField(DB2Types.DB2Blob,         typeof(byte[]),   "GetDB2Blob");
			SetProviderField(DB2Types.DB2Date,         typeof(DateTime), "GetDB2Date");
			SetProviderField(DB2Types.DB2Time,         typeof(TimeSpan), "GetDB2Time");
			SetProviderField(DB2Types.DB2TimeStamp,    typeof(DateTime), "GetDB2TimeStamp");
			SetProviderField(DB2Types.DB2Xml,          typeof(string),   "GetDB2Xml");
			SetProviderField(DB2Types.DB2RowId,        typeof(byte[]),   "GetDB2RowId");

			MappingSchema.AddScalarType(DB2Types.DB2Int64,        GetNullValue(DB2Types.DB2Int64),        true, DataType.Int64);
			MappingSchema.AddScalarType(DB2Types.DB2Int32,        GetNullValue(DB2Types.DB2Int32),        true, DataType.Int32);
			MappingSchema.AddScalarType(DB2Types.DB2Int16,        GetNullValue(DB2Types.DB2Int16),        true, DataType.Int16);
			MappingSchema.AddScalarType(DB2Types.DB2Decimal,      GetNullValue(DB2Types.DB2Decimal),      true, DataType.Decimal);
			MappingSchema.AddScalarType(DB2Types.DB2DecimalFloat, GetNullValue(DB2Types.DB2DecimalFloat), true, DataType.Decimal);
			MappingSchema.AddScalarType(DB2Types.DB2Real,         GetNullValue(DB2Types.DB2Real),         true, DataType.Single);
			MappingSchema.AddScalarType(DB2Types.DB2Real370,      GetNullValue(DB2Types.DB2Real370),      true, DataType.Single);
			MappingSchema.AddScalarType(DB2Types.DB2Double,       GetNullValue(DB2Types.DB2Double),       true, DataType.Double);
			MappingSchema.AddScalarType(DB2Types.DB2String,       GetNullValue(DB2Types.DB2String),       true, DataType.NVarChar);
			MappingSchema.AddScalarType(DB2Types.DB2Clob,         GetNullValue(DB2Types.DB2Clob),         true, DataType.NText);
			MappingSchema.AddScalarType(DB2Types.DB2Binary,       GetNullValue(DB2Types.DB2Binary),       true, DataType.VarBinary);
			MappingSchema.AddScalarType(DB2Types.DB2Blob,         GetNullValue(DB2Types.DB2Blob),         true, DataType.Blob);
			MappingSchema.AddScalarType(DB2Types.DB2Date,         GetNullValue(DB2Types.DB2Date),         true, DataType.Date);
			MappingSchema.AddScalarType(DB2Types.DB2Time,         GetNullValue(DB2Types.DB2Time),         true, DataType.Time);
			MappingSchema.AddScalarType(DB2Types.DB2TimeStamp,    GetNullValue(DB2Types.DB2TimeStamp),    true, DataType.DateTime2);
			MappingSchema.AddScalarType(DB2Types.DB2Xml,          GetNullValue(DB2Types.DB2Xml),          true, DataType.Xml);
			MappingSchema.AddScalarType(DB2Types.DB2RowId,        GetNullValue(DB2Types.DB2RowId),        true, DataType.VarBinary);

			_setBlob = GetSetParameter(connectionType, "DB2Parameter", "DB2Type", "DB2Type", "Blob");

			if (DB2Types.DB2DateTime.IsSupported)
			{
				SetProviderField(DB2Types.DB2DateTime, typeof(DateTime), "GetDB2DateTime");
				MappingSchema.AddScalarType(DB2Types.DB2DateTime, GetNullValue(DB2Types.DB2DateTime), true, DataType.DateTime);
			}

			if (DataConnection.TraceSwitch.TraceInfo)
			{
				DataConnection.WriteTraceLine(
					DataReaderType.AssemblyEx().FullName,
					DataConnection.TraceSwitch.DisplayName);

				DataConnection.WriteTraceLine(
					DB2Types.DB2DateTime.IsSupported ? "DB2DateTime is supported." : "DB2DateTime is not supported.",
					DataConnection.TraceSwitch.DisplayName);
			}

			DB2Tools.Initialized();
		}

		static object GetNullValue(Type type)
		{
			var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
			return getValue.Compile()();
		}

		public    override string ConnectionNamespace { get { return "IBM.Data.DB2"; } }
		protected override string ConnectionTypeName  { get { return "IBM.Data.DB2.DB2Connection, IBM.Data.DB2"; } }
		protected override string DataReaderTypeName  { get { return "IBM.Data.DB2.DB2DataReader, IBM.Data.DB2"; } }

		public DB2Version Version { get; private set; }

		static class MappingSchemaInstance
		{
			public static readonly DB2LUWMappingSchema DB2LUWMappingSchema = new DB2LUWMappingSchema();
			public static readonly DB2zOSMappingSchema DB2zOSMappingSchema = new DB2zOSMappingSchema();
		}

		public override MappingSchema MappingSchema
		{
			get
			{
				switch (Version)
				{
					case DB2Version.LUW : return MappingSchemaInstance.DB2LUWMappingSchema;
					case DB2Version.zOS : return MappingSchemaInstance.DB2zOSMappingSchema;
				}

				return base.MappingSchema;
			}
		}

#if !NETSTANDARD && !NETSTANDARD2_0
		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSchemaProvider() :
				new DB2LUWSchemaProvider();
		}
#endif

		public override ISqlBuilder CreateSqlBuilder()
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter) as ISqlBuilder:
				new DB2LUWSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly DB2SqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters)
		{
			dataConnection.DisposeCommand();
			base.InitCommand(dataConnection, commandType, commandText, parameters);
		}

		static Action<IDbDataParameter> _setBlob;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is sbyte)
			{
				value    = (short)(sbyte)value;
				dataType = DataType.Int16;
			}
			else if (value is byte)
			{
				value    = (short)(byte)value;
				dataType = DataType.Int16;
			}

			switch (dataType)
			{
				case DataType.UInt16     : dataType = DataType.Int32;    break;
				case DataType.UInt32     : dataType = DataType.Int64;    break;
				case DataType.UInt64     : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;  break;
				case DataType.DateTime2  : dataType = DataType.DateTime; break;
				case DataType.Char       :
				case DataType.VarChar    :
				case DataType.NChar      :
				case DataType.NVarChar   :
					     if (value is Guid) value = ((Guid)value).ToString();
					else if (value is bool)
						value = Common.ConvertTo<char>.From((bool)value);
					break;
				case DataType.Boolean    :
				case DataType.Int16      :
					if (value is bool)
					{
						value    = (bool)value ? 1 : 0;
						dataType = DataType.Int16;
					}
					break;
				case DataType.Guid       :
					if (value is Guid)
					{
						value    = ((Guid)value).ToByteArray();
						dataType = DataType.VarBinary;
					}
					if (value == null)
						dataType = DataType.VarBinary;
					break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					else if (parameter.Size == 0 && value != null && value.GetType().Name == "DB2Binary")
					{
						dynamic v = value;
						if (v.IsNull)
							value = DBNull.Value;
					}
					break;
				case DataType.Blob       :
					base.SetParameter(parameter, "@" + name, dataType, value);
					_setBlob(parameter);
					return;
			}

			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		#region BulkCopy

		DB2BulkCopy _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new DB2BulkCopy(GetConnectionType());

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? DB2Tools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion

		#region Merge

		public override int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			if (delete)
				throw new LinqToDBException("DB2 MERGE statement does not support DELETE by source.");

			return new DB2Merge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

#if !NOASYNC

		public override Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			if (delete)
				throw new LinqToDBException("DB2 MERGE statement does not support DELETE by source.");

			return new DB2Merge().MergeAsync(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName, token);
		}

#endif

		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget, TSource> merge)
		{
			return new DB2MergeBuilder<TTarget, TSource>(connection, merge);
		}

		#endregion
	}
}
