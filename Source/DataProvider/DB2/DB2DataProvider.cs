using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using Data;
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

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			_sqlOptimizer = new DB2SqlOptimizer(SqlProviderFlags);
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			DB2Types.ConnectionType = connectionType;

			DB2Types.DB2Int64.       Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int64",        true);
			DB2Types.DB2Int32.       Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int32",        true);
			DB2Types.DB2Int16.       Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int16",        true);
			DB2Types.DB2Decimal.     Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Decimal",      true);
			DB2Types.DB2DecimalFloat.Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2DecimalFloat", true);
			DB2Types.DB2Real.        Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real",         true);
			DB2Types.DB2Real370.     Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real370",      true);
			DB2Types.DB2Double.      Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Double",       true);
			DB2Types.DB2String.      Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2String",       true);
			DB2Types.DB2Clob.        Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Clob",         true);
			DB2Types.DB2Binary.      Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Binary",       true);
			DB2Types.DB2Blob.        Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Blob",         true);
			DB2Types.DB2Date.        Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Date",         true);
			DB2Types.DB2DateTime.    Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2DateTime",     false);
			DB2Types.DB2Time.        Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Time",         true);
			DB2Types.DB2TimeStamp.   Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2TimeStamp",    true);
			DB2Types.DB2Xml               = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Xml",          true);
			DB2Types.DB2RowId.       Type = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2RowId",        true);

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
			SetProviderField(DB2Types.DB2DateTime,     typeof(DateTime), "GetDB2DateTime");
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
			MappingSchema.AddScalarType(DB2Types.DB2DateTime,     GetNullValue(DB2Types.DB2DateTime),     true, DataType.DateTime);
			MappingSchema.AddScalarType(DB2Types.DB2Time,         GetNullValue(DB2Types.DB2Time),         true, DataType.Time);
			MappingSchema.AddScalarType(DB2Types.DB2TimeStamp,    GetNullValue(DB2Types.DB2TimeStamp),    true, DataType.DateTime2);
			MappingSchema.AddScalarType(DB2Types.DB2Xml,          GetNullValue(DB2Types.DB2Xml),          true, DataType.Xml);
			MappingSchema.AddScalarType(DB2Types.DB2RowId,        GetNullValue(DB2Types.DB2RowId),        true, DataType.VarBinary);

			_setBlob = GetSetParameter(connectionType, "DB2Parameter", "DB2Type", "DB2Type", "Blob");

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

		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSchemaProvider() :
				new DB2LUWSchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return Version == DB2Version.zOS ?
				new DB2zOSSqlBuilder(GetSqlOptimizer(), SqlProviderFlags) as ISqlBuilder:
				new DB2LUWSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly DB2SqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override void InitCommand(DataConnection dataConnection)
		{
			dataConnection.DisposeCommand();
			base.InitCommand(dataConnection);
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
					break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
				case DataType.Blob       :
					base.SetParameter(parameter, "@" + name, dataType, value);
					_setBlob(parameter);
					return;
			}

			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		static Func<IDbConnection,IDisposable> _bulkCopyCreator;
		static Func<int,string,object>         _columnMappingCreator;

		public override int BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection  dataConnection,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var bkCopyType = options.BulkCopyType == BulkCopyType.Default ?
				DB2Tools.DefaultBulkCopyType :
				options.BulkCopyType;

			if (bkCopyType == BulkCopyType.RowByRow)
				return base.BulkCopy(dataConnection, options, source);

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = sqlBuilder
				.BuildTableName(
					new StringBuilder(),
					descriptor.DatabaseName == null ? null : sqlBuilder.Convert(descriptor.DatabaseName, ConvertType.NameToDatabase).  ToString(),
					descriptor.SchemaName   == null ? null : sqlBuilder.Convert(descriptor.SchemaName,   ConvertType.NameToOwner).     ToString(),
					descriptor.TableName    == null ? null : sqlBuilder.Convert(descriptor.TableName,    ConvertType.NameToQueryTable).ToString())
				.ToString();

			if (bkCopyType == BulkCopyType.ProviderSpecific && dataConnection.Transaction == null)
			{
				if (_bulkCopyCreator == null)
				{
					var connType          = GetConnectionType();
					var bulkCopyType      = connType.Assembly.GetType("IBM.Data.DB2.DB2BulkCopy",              false);
					var columnMappingType = connType.Assembly.GetType("IBM.Data.DB2.DB2BulkCopyColumnMapping", false);

					if (bulkCopyType != null)
					{
						{
							var p = Expression.Parameter(typeof(IDbConnection), "p");
							var l = Expression.Lambda<Func<IDbConnection,IDisposable>>(
								Expression.Convert(
									Expression.New(
										bulkCopyType.GetConstructor(new[] { connType }),
										Expression.Convert(p, connType)),
									typeof(IDisposable)),
								p);

							_bulkCopyCreator = l.Compile();
						}
						{
							var p1 = Expression.Parameter(typeof(int),    "p1");
							var p2 = Expression.Parameter(typeof(string), "p2");
							var l  = Expression.Lambda<Func<int,string,object>>(
								Expression.Convert(
									Expression.New(
										columnMappingType.GetConstructor(new[] { typeof(int), typeof(string) }),
										new [] { p1, p2 }),
									typeof(object)),
								p1, p2);

							_columnMappingCreator = l.Compile();
						}
					}
				}

				if (_bulkCopyCreator != null)
				{
					var columns = descriptor.Columns.Where(c => !c.SkipOnInsert).ToList();
					var rd      = new BulkCopyReader(this, columns, source);

					using (var bc = _bulkCopyCreator(dataConnection.Connection))
					{
						dynamic dbc = bc;

						if (options.BulkCopyTimeout.HasValue)
							dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						dbc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							dbc.ColumnMappings.Add((dynamic)_columnMappingCreator(i, columns[i].ColumnName));

						dbc.WriteToServer(rd);
					}

					return rd.Count;
				}
			}

			return MultipleRowsBulkCopy(dataConnection, options, source, sqlBuilder, descriptor, tableName);
		}

		int MultipleRowsBulkCopy<T>(
			DataConnection   dataConnection,
			BulkCopyOptions  options,
			IEnumerable<T>   source,
			BasicSqlBuilder  sqlBuilder,
			EntityDescriptor descriptor,
			string           tableName)
		{
			var iszOS = Version == DB2Version.zOS;

			{
				var sb         = new StringBuilder();
				var buildValue = BasicSqlBuilder.GetBuildValue(sqlBuilder, sb);
				var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();
				var pname      = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();

				sb
					.AppendFormat("INSERT INTO {0}", tableName).AppendLine()
					.Append("(");

				foreach (var column in columns)
					sb
						.AppendLine()
						.Append("\t")
						.Append(sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
						.Append(",");

				sb.Length--;
				sb
					.AppendLine()
					.Append(")");

				if (!iszOS)
					sb
						.AppendLine()
						.Append("VALUES");

				var headerLen    = sb.Length;
				var totalCount   = 0;
				var currentCount = 0;
				var batchSize    = options.MaxBatchSize ?? 1000;

				if (batchSize <= 0)
					batchSize = 1000;

				var parms = new List<DataParameter>();
				var pidx  = 0;

				foreach (var item in source)
				{
					sb
						.AppendLine()
						.Append(iszOS ? "SELECT " : "(");

					foreach (var column in columns)
					{
						var value = column.GetValue(item);

						if (value == null)
						{
							sb.Append("NULL");
						}
						else
							switch (Type.GetTypeCode(value.GetType()))
							{
								case TypeCode.DBNull:
									sb.Append("NULL");
									break;

								case TypeCode.String:
									var isString = false;

									switch (column.DataType)
									{
										case DataType.NVarChar  :
										case DataType.Char      :
										case DataType.VarChar   :
										case DataType.NChar     :
										case DataType.Undefined :
											isString = true;
											break;
									}

									if (isString) goto case TypeCode.Int32;
									goto default;

								case TypeCode.Boolean  :
								case TypeCode.Char     :
								case TypeCode.SByte    :
								case TypeCode.Byte     :
								case TypeCode.Int16    :
								case TypeCode.UInt16   :
								case TypeCode.Int32    :
								case TypeCode.UInt32   :
								case TypeCode.Int64    :
								case TypeCode.UInt64   :
								case TypeCode.Single   :
								case TypeCode.Double   :
								case TypeCode.Decimal  :
								case TypeCode.DateTime :
									//SetParameter(dataParam, "", column.DataType, value);

									buildValue(value);
									break;

								default:
									var name = pname + ++pidx;

									sb.Append(name);
									parms.Add(new DataParameter("p" + pidx, value, column.DataType));

									break;
							}

						sb.Append(",");
					}

					sb.Length--;
					sb.Append(iszOS ? " FROM SYSIBM.SYSDUMMY1 UNION ALL" : "),");

					totalCount++;
					currentCount++;

					if (currentCount >= batchSize || parms.Count > 100000 || sb.Length > 100000)
					{
						if (iszOS)
							sb.Length -= " UNION ALL".Length;
						else
							sb.Length--;

						dataConnection.Execute(sb.AppendLine().ToString(), parms.ToArray());

						parms.Clear();
						pidx         = 0;
						currentCount = 0;
						sb.Length    = headerLen;
					}
				}

				if (currentCount > 0)
				{
					if (iszOS)
						sb.Length -= " UNION ALL".Length;
					else
						sb.Length--;

					dataConnection.Execute(sb.ToString(), parms.ToArray());
					sb.Length = headerLen;
				}

				return totalCount;
			}
		}
	}
}
