using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.DB2
{
	using System.Text;

	using Common;

	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class DB2DataProvider : DynamicDataProviderBase
	{
		public DB2DataProvider()
			: this(ProviderName.DB2, new DB2MappingSchema())
		{
		}

		protected DB2DataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter       = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			_sqlOptimizer = new DB2SqlOptimizer(SqlProviderFlags);
		}

		Type _db2Int64;
		Type _db2Int32;
		Type _db2Int16;
		Type _db2Decimal;
		Type _db2DecimalFloat;
		Type _db2Real;
		Type _db2Real370;
		Type _db2Double;
		Type _db2String;
		Type _db2Clob;
		Type _db2Binary;
		Type _db2Blob;
		Type _db2Date;
		Type _db2Time;
		Type _db2TimeStamp;
		Type _db2Xml;
		Type _db2RowId;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_db2Int64        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int64",        true);
			_db2Int32        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int32",        true);
			_db2Int16        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Int16",        true);
			_db2Decimal      = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Decimal",      true);
			_db2DecimalFloat = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2DecimalFloat", true);
			_db2Real         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real",         true);
			_db2Real370      = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Real370",      true);
			_db2Double       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Double",       true);
			_db2String       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2String",       true);
			_db2Clob         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Clob",         true);
			_db2Binary       = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Binary",       true);
			_db2Blob         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Blob",         true);
			_db2Date         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Date",         true);
			_db2Time         = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Time",         true);
			_db2TimeStamp    = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2TimeStamp",    true);
			_db2Xml          = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2Xml",          true);
			_db2RowId        = connectionType.Assembly.GetType("IBM.Data.DB2Types.DB2RowId",        true);

			SetProviderField(_db2Int64,        typeof(Int64),    "GetDB2Int64");
			SetProviderField(_db2Int32,        typeof(Int32),    "GetDB2Int32");
			SetProviderField(_db2Int16,        typeof(Int16),    "GetDB2Int16");
			SetProviderField(_db2Decimal,      typeof(Decimal),  "GetDB2Decimal");
			SetProviderField(_db2DecimalFloat, typeof(Decimal),  "GetDB2DecimalFloat");
			SetProviderField(_db2Real,         typeof(Single),   "GetDB2Real");
			SetProviderField(_db2Real370,      typeof(Single),   "GetDB2Real370");
			SetProviderField(_db2Double,       typeof(Double),   "GetDB2Double");
			SetProviderField(_db2String,       typeof(String),   "GetDB2String");
			SetProviderField(_db2Clob,         typeof(String),   "GetDB2Clob");
			SetProviderField(_db2Binary,       typeof(byte[]),   "GetDB2Binary");
			SetProviderField(_db2Blob,         typeof(byte[]),   "GetDB2Blob");
			SetProviderField(_db2Date,         typeof(DateTime), "GetDB2Date");
			SetProviderField(_db2Time,         typeof(TimeSpan), "GetDB2Time");
			SetProviderField(_db2TimeStamp,    typeof(DateTime), "GetDB2TimeStamp");
			SetProviderField(_db2Xml,          typeof(string),   "GetDB2Xml");
			SetProviderField(_db2RowId,        typeof(byte[]),   "GetDB2RowId");

			MappingSchema.AddScalarType(_db2Int64,        GetNullValue(_db2Int64),        true, DataType.Int64);
			MappingSchema.AddScalarType(_db2Int32,        GetNullValue(_db2Int32),        true, DataType.Int32);
			MappingSchema.AddScalarType(_db2Int16,        GetNullValue(_db2Int16),        true, DataType.Int16);
			MappingSchema.AddScalarType(_db2Decimal,      GetNullValue(_db2Decimal),      true, DataType.Decimal);
			MappingSchema.AddScalarType(_db2DecimalFloat, GetNullValue(_db2DecimalFloat), true, DataType.Decimal);
			MappingSchema.AddScalarType(_db2Real,         GetNullValue(_db2Real),         true, DataType.Single);
			MappingSchema.AddScalarType(_db2Real370,      GetNullValue(_db2Real370),      true, DataType.Single);
			MappingSchema.AddScalarType(_db2Double,       GetNullValue(_db2Double),       true, DataType.Double);
			MappingSchema.AddScalarType(_db2String,       GetNullValue(_db2String),       true, DataType.NVarChar);
			MappingSchema.AddScalarType(_db2Clob,         GetNullValue(_db2Clob),         true, DataType.NText);
			MappingSchema.AddScalarType(_db2Binary,       GetNullValue(_db2Binary),       true, DataType.VarBinary);
			MappingSchema.AddScalarType(_db2Blob,         GetNullValue(_db2Blob),         true, DataType.Blob);
			MappingSchema.AddScalarType(_db2Date,         GetNullValue(_db2Date),         true, DataType.Date);
			MappingSchema.AddScalarType(_db2Time,         GetNullValue(_db2Time),         true, DataType.Time);
			MappingSchema.AddScalarType(_db2TimeStamp,    GetNullValue(_db2TimeStamp),    true, DataType.DateTime2);
			MappingSchema.AddScalarType(_db2Xml,          GetNullValue(_db2Xml),          true, DataType.Xml);
			MappingSchema.AddScalarType(_db2RowId,        GetNullValue(_db2RowId),        true, DataType.VarBinary);

			_setBlob = GetSetParameter(connectionType, "DB2Parameter", "DB2Type", "DB2Type", "Blob");
		}

		static object GetNullValue(Type type)
		{
			var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
			return getValue.Compile()();
		}

		public    override string ConnectionNamespace { get { return "IBM.Data.DB2"; } }
		protected override string ConnectionTypeName  { get { return "IBM.Data.DB2.DB2Connection, IBM.Data.DB2"; } }
		protected override string DataReaderTypeName  { get { return "IBM.Data.DB2.DB2DataReader, IBM.Data.DB2"; } }

		public override ISchemaProvider GetSchemaProvider()
		{
			return new DB2SchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2SqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
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
			if (options.BulkCopyType == BulkCopyType.RowByRow)
				return base.BulkCopy(dataConnection, options, source);

			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = sqlBuilder
				.BuildTableName(
					new StringBuilder(),
					descriptor.DatabaseName == null ? null : sqlBuilder.Convert(descriptor.DatabaseName, ConvertType.NameToDatabase).  ToString(),
					descriptor.SchemaName   == null ? null : sqlBuilder.Convert(descriptor.SchemaName,   ConvertType.NameToOwner).     ToString(),
					descriptor.TableName    == null ? null : sqlBuilder.Convert(descriptor.TableName,    ConvertType.NameToQueryTable).ToString())
				.ToString();

			if (options.BulkCopyType == BulkCopyType.ProviderSpecific && dataConnection.Transaction == null)
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
					var rd = new BulkCopyReader(descriptor, source);

					using (var bc = _bulkCopyCreator(dataConnection.Connection))
					{
						dynamic dbc = bc;

						if (options.BulkCopyTimeout.HasValue)
							dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						dbc.DestinationTableName = tableName;

						for (var i = 0; i < rd.Columns.Length; i++)
							dbc.ColumnMappings.Add((dynamic)_columnMappingCreator(i, rd.Columns[i].ColumnName));

						dbc.WriteToServer(rd);
					}

					return rd.Count;
				}
			}

			var iszOS = true;

			{
				var sb         = new StringBuilder();
				var buildValue = BasicSqlBuilder.GetBuildValue(sqlBuilder, sb);
				var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();

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

				foreach (var item in source)
				{
					sb
						.AppendLine()
						.Append(iszOS ? "SELECT " : "(");

					foreach (var column in columns)
					{
						buildValue(column.GetValue(item));
						sb.Append(",");
					}

					sb.Length--;
					sb.Append(iszOS ? " FROM SYSIBM.SYSDUMMY1 UNION ALL" : "),");

					totalCount++;
					currentCount++;

					if (currentCount >= batchSize)
					{
						if (iszOS)
							sb.Length -= " UNION ALL".Length;
						else
							sb.Length--;

						dataConnection.Execute(sb.AppendLine().ToString());
						currentCount = 0;
						sb.Length = headerLen;
					}
				}

				if (currentCount > 0)
				{
					if (iszOS)
						sb.Length -= " UNION ALL".Length;
					else
						sb.Length--;

					dataConnection.Execute(sb.ToString());
					sb.Length = headerLen;
				}

				return totalCount;
			}
		}
	}
}
