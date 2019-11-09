#nullable disable
using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.MySql
{
	using Data;
	using Common;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlProvider;
	using Tools;
	using System.Linq.Expressions;

	public class MySqlDataProvider : DynamicDataProviderBase
	{
		public MySqlDataProvider()
			: this(ProviderName.MySql, new MySqlMappingSchema())
		{
		}

		public MySqlDataProvider(string name)
			: this(name, null)
		{
		}

		protected MySqlDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsDistinctOrderBySupported        = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			_sqlOptimizer = new MySqlSqlOptimizer(SqlProviderFlags);
		}

		private bool _customReadersConfigured = false;

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			if (!_customReadersConfigured)
			{
				var wrapper = MySqlWrappers.Initialize(this);

				// configure provider-specific data readers
				if (wrapper.GetMySqlDecimalMethodName != null)
				{
					// SetProviderField is not needed for this type
					SetToTypeField  (wrapper.MySqlDecimalType, wrapper.GetMySqlDecimalMethodName, wrapper.DataReaderType);
				}

				SetProviderField(wrapper.MySqlDateTimeType, wrapper.GetMySqlDateTimeMethodName, wrapper.DataReaderType);
				SetToTypeField  (wrapper.MySqlDateTimeType, wrapper.GetMySqlDateTimeMethodName, wrapper.DataReaderType);

				_customReadersConfigured = true;
			}

			return base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
		}


		public    override string ConnectionNamespace => "MySql.Data.MySqlClient";
		protected override string ConnectionTypeName  => Name == ProviderName.MySqlConnector
			? $"{ConnectionNamespace}.MySqlConnection, MySqlConnector"
			: $"{ConnectionNamespace}.MySqlConnection, MySql.Data";

		protected override string DataReaderTypeName  => Name == ProviderName.MySqlConnector
			? $"{ConnectionNamespace}.MySqlDataReader, MySqlConnector"
			: $"{ConnectionNamespace}.MySqlDataReader, MySql.Data";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new MySqlSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly MySqlMappingSchema.MySqlOfficialMappingSchema MySqlOfficialMappingSchema   = new MySqlMappingSchema.MySqlOfficialMappingSchema();
			public static readonly MySqlMappingSchema.MySqlConnectorMappingSchema MySqlConnectorMappingSchema = new MySqlMappingSchema.MySqlConnectorMappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.MySqlConnector
			? MappingSchemaInstance.MySqlConnectorMappingSchema
			: MappingSchemaInstance.MySqlOfficialMappingSchema as MappingSchema;

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if NETSTANDARD2_0 || NETCOREAPP2_1
		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}
#endif

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			var wrapper = MySqlWrappers.Initialize(this);

			switch (dataType.DataType)
			{
				case DataType.Decimal    :
				case DataType.VarNumeric :
					if (wrapper.MySqlDecimalGetter != null && value != null && value.GetType() == wrapper.MySqlDecimalType)
						value = wrapper.MySqlDecimalGetter(value);
					break;
				//case DataType.Date       :
				//case DataType.DateTime   :
				//case DataType.DateTime2  :
				//	if (value != null && value.GetType() == wrapper.MySqlDateTimeType)
				//		value = wrapper.MySqlDateTimeGetter(value);
				//	break;


				//case DataType.Char       :
				//case DataType.VarChar    :
				//case DataType.NVarChar   :
				//case DataType.NChar      :
				//	if (value is char)
				//		value = value.ToString();
				//	break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			// VarNumeric - mysql.data trims fractional part
			// Date/DateTime2 - mysql.data trims time part
			switch (dataType.DataType)
			{
				case DataType.VarNumeric: parameter.DbType = DbType.Decimal;  return;
				case DataType.Date:
				case DataType.DateTime2 : parameter.DbType = DbType.DateTime; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentException("Source is null!", nameof(source));

#pragma warning disable 618
			if (options.RetrieveSequence)
			{
				var list = source.RetrieveIdentity((DataConnection)table.DataContext);

				if (!ReferenceEquals(list, source))
					options = new BulkCopyOptions(options) { KeepIdentity = true };

				source = list;
			}
#pragma warning restore 618

			return new MySqlBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
