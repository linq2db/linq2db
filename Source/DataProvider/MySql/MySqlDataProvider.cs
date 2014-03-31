using System;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
	using System.Collections.Generic;
	using System.Text;

	using Common;

	using Data;

	using Mapping;
	using Reflection;
	using SqlProvider;

	public class MySqlDataProvider : DynamicDataProviderBase
	{
		public MySqlDataProvider()
			: this(ProviderName.MySql, new MySqlMappingSchema())
		{
		}

		protected MySqlDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			_sqlOptimizer = new MySqlSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace { get { return "MySql.Data.MySqlClient"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, MySql.Data".Args(ConnectionNamespace, "MySqlConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, MySql.Data".Args(ConnectionNamespace, "MySqlDataReader"); } }

		Type _mySqlDecimalType;
		Type _mySqlDateTimeType;

		Func<object,object> _mySqlDecimalValueGetter;
		Func<object,object> _mySqlDateTimeValueGetter;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_mySqlDecimalType  = connectionType.Assembly.GetType("MySql.Data.Types.MySqlDecimal",  true);
			_mySqlDateTimeType = connectionType.Assembly.GetType("MySql.Data.Types.MySqlDateTime", true);

			_mySqlDecimalValueGetter  = TypeAccessor.GetAccessor(_mySqlDecimalType) ["Value"].Getter;
			_mySqlDateTimeValueGetter = TypeAccessor.GetAccessor(_mySqlDateTimeType)["Value"].Getter;

			SetProviderField(_mySqlDecimalType,  "GetMySqlDecimal");
			SetProviderField(_mySqlDateTimeType, "GetMySqlDateTime");
			SetToTypeField  (_mySqlDecimalType,  "GetMySqlDecimal");
			SetToTypeField  (_mySqlDateTimeType, "GetMySqlDateTime");

			MappingSchema.SetDataType(_mySqlDecimalType,  DataType.Decimal);
			MappingSchema.SetDataType(_mySqlDateTimeType, DataType.DateTime2);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new MySqlSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Decimal    :
				case DataType.VarNumeric :
					if (value != null && value.GetType() == _mySqlDecimalType)
						value = _mySqlDecimalValueGetter(value);
					break;
				case DataType.Date       :
				case DataType.DateTime   :
				case DataType.DateTime2  :
					if (value != null && value.GetType() == _mySqlDateTimeType)
						value = _mySqlDateTimeValueGetter(value);
					break;
				case DataType.Char       :
				case DataType.NChar      :
					if (value is char)
						value = value.ToString();
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
		}

		public override int BulkCopy<T>([JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var bkCopyType = options.BulkCopyType == BulkCopyType.Default ?
				MySqlTools.DefaultBulkCopyType :
				options.BulkCopyType;

			if (bkCopyType == BulkCopyType.RowByRow)
				return base.BulkCopy(dataConnection, options, source);

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName = sqlBuilder
				.BuildTableName(
					new StringBuilder(),
					descriptor.DatabaseName == null ? null : sqlBuilder.Convert(descriptor.DatabaseName, ConvertType.NameToDatabase).ToString(),
					descriptor.SchemaName == null ? null : sqlBuilder.Convert(descriptor.SchemaName, ConvertType.NameToOwner).ToString(),
					descriptor.TableName == null ? null : sqlBuilder.Convert(descriptor.TableName, ConvertType.NameToQueryTable).ToString())
				.ToString();

			return MultipleRowsBulkCopy(dataConnection, options, source, sqlBuilder, descriptor, tableName);
		}

		int MultipleRowsBulkCopy<T>(
			DataConnection dataConnection,
			BulkCopyOptions options,
			IEnumerable<T> source,
			BasicSqlBuilder sqlBuilder,
			EntityDescriptor descriptor,
			string tableName)
		{
			{
				var sb = new StringBuilder();
				var buildValue = BasicSqlBuilder.GetBuildValue(sqlBuilder, sb);
				var columns = descriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();
				var pname = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();

				sb.AppendFormat("INSERT \tINTO {0} (", tableName);
				foreach (var column in columns)
					sb
						.Append(sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
						.Append(", ");

				sb.Length -= 2;

				sb.Append(") VALUES (");

				var headerLen = sb.Length;
				var totalCount = 0;
				var currentCount = 0;
				var batchSize = options.MaxBatchSize ?? 1000;

				if (batchSize <= 0)
					batchSize = 1000;

				var parms = new List<DataParameter>();
				var pidx = 0;

				foreach (var item in source)
				{
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
										case DataType.NVarChar:
										case DataType.Char:
										case DataType.VarChar:
										case DataType.NChar:
										case DataType.Undefined:
											isString = true;
											break;
									}

									if (isString) goto case TypeCode.Int32;
									goto default;

								case TypeCode.Boolean:
								case TypeCode.Char:
								case TypeCode.SByte:
								case TypeCode.Byte:
								case TypeCode.Int16:
								case TypeCode.UInt16:
								case TypeCode.Int32:
								case TypeCode.UInt32:
								case TypeCode.Int64:
								case TypeCode.UInt64:
								case TypeCode.Single:
								case TypeCode.Double:
								case TypeCode.Decimal:
								case TypeCode.DateTime:
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
					sb.AppendLine(")");

					totalCount++;
					currentCount++;

					if (currentCount >= batchSize || parms.Count > 100000 || sb.Length > 100000)
					{
						dataConnection.Execute(sb.AppendLine().ToString(), parms.ToArray());

						parms.Clear();
						pidx = 0;
						currentCount = 0;
						sb.Length = headerLen;
					}
					else
					{
						sb.Append(",(");
					}
				}

				if (currentCount > 0)
				{
					//sb.AppendLine("SELECT * FROM dual");
					sb.Length-=2;
					dataConnection.Execute(sb.ToString(), parms.ToArray());
					sb.Length = headerLen;
				}

				return totalCount;
			}
		}
	}
}
