using System;
using System.Collections.Generic;
using System.Data;
using LinqToDB.Extensions;
using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
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
			_mySqlDecimalType  = connectionType.AssemblyEx().GetType("MySql.Data.Types.MySqlDecimal",  true);
			_mySqlDateTimeType = connectionType.AssemblyEx().GetType("MySql.Data.Types.MySqlDateTime", true);

			_mySqlDecimalValueGetter  = TypeAccessor.GetAccessor(_mySqlDecimalType) ["Value"].Getter;
			_mySqlDateTimeValueGetter = TypeAccessor.GetAccessor(_mySqlDateTimeType)["Value"].Getter;

			SetProviderField(_mySqlDecimalType,  "GetMySqlDecimal");
			SetProviderField(_mySqlDateTimeType, "GetMySqlDateTime");
			SetToTypeField  (_mySqlDecimalType,  "GetMySqlDecimal");
			SetToTypeField  (_mySqlDateTimeType, "GetMySqlDateTime");

			MappingSchema.SetDataType(_mySqlDecimalType,  DataType.Decimal);
			MappingSchema.SetDataType(_mySqlDateTimeType, DataType.DateTime2);
		}

#if !NETSTANDARD
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider();
		}
#endif
		public override ISqlBuilder CreateSqlBuilder()
		{
			return new MySqlSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
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

#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
            if (options.RetrieveSequence)
            {
                var supportedFileTypes = new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(short), typeof(ushort) };

                var entityDescriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
                var columns          = entityDescriptor.Columns.Where(x => x.IsPrimaryKey).ToList();

                if (columns.Count == 1 && supportedFileTypes.Contains(columns.First().MemberType))
                {
                    var c = columns.First();
                    var lastId = dataConnection.Query<int>($"SELECT {c.ColumnName} FROM {entityDescriptor.TableName} ORDER BY {c.ColumnName} DESC LIMIT 1").FirstOrDefault();
                    lastId = lastId + source.Count(); //Insert the max id first in order to prevent inserting collisions on inserting from another thread (on big batches)
                    foreach (var item in source)
                        c.MemberAccessor.SetValue(item, lastId--);
                    
                    options.KeepIdentity = true;
                }
            }

            return new MySqlBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

#endregion
	}
}
