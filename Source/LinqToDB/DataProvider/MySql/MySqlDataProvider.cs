using System;
using System.Collections.Generic;
using System.Data;
using LinqToDB.Tools;

namespace LinqToDB.DataProvider.MySql
{
	using Data;
	using Common;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlProvider;

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

			_sqlOptimizer = new MySqlSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace => "MySql.Data.MySqlClient";
		protected override string ConnectionTypeName  => Name == ProviderName.MySqlConnector
			? $"{ConnectionNamespace}.MySqlConnection, MySqlConnector"
			: $"{ConnectionNamespace}.MySqlConnection, MySql.Data";
			
		protected override string DataReaderTypeName  => Name == ProviderName.MySqlConnector
			? $"{ConnectionNamespace}.MySqlDataReader, MySqlConnector"
			: $"{ConnectionNamespace}.MySqlDataReader, MySql.Data";

		Type _mySqlDecimalType;
		Type _mySqlDateTimeType;

		Func<object,object> _mySqlDecimalValueGetter;
		Func<object,object> _mySqlDateTimeValueGetter;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_mySqlDateTimeType = connectionType.AssemblyEx().GetType("MySql.Data.Types.MySqlDateTime", true);

			if (Name != ProviderName.MySqlConnector)
			{
				_mySqlDecimalType         = connectionType.AssemblyEx().GetType("MySql.Data.Types.MySqlDecimal", true);

				_mySqlDecimalValueGetter  = TypeAccessor.GetAccessor(_mySqlDecimalType) ["Value"].Getter;
				_mySqlDateTimeValueGetter = TypeAccessor.GetAccessor(_mySqlDateTimeType)["Value"].Getter;

				SetProviderField(_mySqlDecimalType, "GetMySqlDecimal");
				SetToTypeField(_mySqlDecimalType,   "GetMySqlDecimal");

				MappingSchema.SetDataType(_mySqlDecimalType, DataType.Decimal);
			}
			
			SetProviderField(_mySqlDateTimeType, "GetMySqlDateTime");
			SetToTypeField(_mySqlDateTimeType,   "GetMySqlDateTime");
			
			MappingSchema.SetDataType(_mySqlDateTimeType, DataType.DateTime2);
		}

#if !NETSTANDARD1_6
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider();
		}
#endif

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new MySqlSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
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

#if NETSTANDARD2_0
		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}
#endif

		public override void SetParameter(IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			switch (dataType.DataType)
			{
				case DataType.Decimal    :
				case DataType.VarNumeric :
					if (value != null && value.GetType() == _mySqlDecimalType)
						value = _mySqlDecimalValueGetter(value);
					break;
				case DataType.Date       :
				case DataType.DateTime   :
				case DataType.DateTime2  :
					if (value != null && _mySqlDateTimeValueGetter != null && value.GetType() == _mySqlDateTimeType)
						value = _mySqlDateTimeValueGetter(value);
					break;
				case DataType.Char       :
				case DataType.VarChar    :
				case DataType.NVarChar   :
				case DataType.NChar      :
					if (value is char)
						value = value.ToString();
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			if (Name == ProviderName.MySqlConnector)
			{
				base.SetParameterType(parameter, dataType);
			}
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
					options.KeepIdentity = true;

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
