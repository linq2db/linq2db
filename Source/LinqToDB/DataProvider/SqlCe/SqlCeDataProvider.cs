using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlCe.Translation;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SqlCe
{
	public class SqlCeDataProvider : DynamicDataProviderBase<SqlCeProviderAdapter>
	{
		public SqlCeDataProvider()
			: this(ProviderName.SqlCe, SqlCeMappingSchema.Instance)
		{
		}

		protected SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, SqlCeProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsSubQueryColumnSupported           = false;
			SqlProviderFlags.IsCountSubQuerySupported            = false;
			SqlProviderFlags.IsApplyJoinSupported                = true;
			SqlProviderFlags.IsInsertOrUpdateSupported           = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported    = false;
			SqlProviderFlags.IsUpdateFromSupported               = false;
			SqlProviderFlags.IsCountDistinctSupported            = false;
			SqlProviderFlags.IsAggregationDistinctSupported      = false;
			SqlProviderFlags.SupportsBooleanType                 = false;
			SqlProviderFlags.IsWindowFunctionsSupported          = false;
			SqlProviderFlags.IsOrderByAggregateFunctionSupported = false;

			SetCharFieldToType<char>("NChar", DataTools.GetCharExpression);

			SetCharField("NChar",    (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NVarChar", (r,i) => r.GetString(i).TrimEnd(' '));

			ReaderExpressions[new ReaderInfo { ToType = typeof(decimal), ProviderFieldType = typeof(SqlDecimal), DataReaderType = Adapter.DataReaderType }] = Adapter.GetDecimalExpression;

			_sqlOptimizer = new SqlCeSqlOptimizer(SqlProviderFlags);
		}

		#region Overrides

		public override TableOptions SupportedTableOptions => TableOptions.None;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new SqlCeMemberTranslator();
		}

		public override SqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new SqlCeSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SqlCeSchemaProvider();
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
#if NET8_0_OR_GREATER
			if (value is DateOnly d)
				value = d.ToDateTime(TimeOnly.MinValue);
#endif

			switch (dataType.DataType)
			{
				case DataType.Xml :
					dataType = dataType.WithDataType(DataType.NVarChar);

					if      (value is SqlXml xml)      value = xml.IsNull ? null : xml.Value;
					else if (value is XDocument xdoc)  value = xdoc.ToString();
					else if (value is XmlDocument doc) value = doc.InnerXml;

					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			SqlDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text       :
				case DataType.NText      : type = SqlDbType.NText;     break;
				case DataType.VarChar    :
				case DataType.NVarChar   : type = SqlDbType.NVarChar;  break;
				case DataType.Timestamp  : type = SqlDbType.Timestamp; break;
				case DataType.Binary     : type = SqlDbType.Binary;    break;
				case DataType.VarBinary  : type = SqlDbType.VarBinary; break;
				case DataType.Image      : type = SqlDbType.Image;     break;
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
				case DataType.SByte      : parameter.DbType = DbType.Int16;             return;
				case DataType.UInt16     : parameter.DbType = DbType.Int32;             return;
				case DataType.UInt32     : parameter.DbType = DbType.Int64;             return;
				case DataType.UInt64     : parameter.DbType = DbType.Decimal;           return;
				case DataType.VarNumeric : parameter.DbType = DbType.Decimal;           return;
				case DataType.Char       :
				case DataType.NChar      : parameter.DbType = DbType.String;            return;
				case DataType.Date       :
				case DataType.DateTime2  : parameter.DbType = DbType.DateTime;          return;
				case DataType.Money      : parameter.DbType = DbType.Currency;          return;
				case DataType.Text       :
				case DataType.VarChar    :
				case DataType.NText      : parameter.DbType = DbType.String;            return;
				case DataType.Timestamp  :
				case DataType.Binary     :
				case DataType.Image      : parameter.DbType = DbType.Binary;            return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

#endregion

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			return true;
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new SqlCeBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlCeOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SqlCeBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlCeOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SqlCeBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SqlCeOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
