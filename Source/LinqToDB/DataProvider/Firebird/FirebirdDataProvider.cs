using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;
	using System.Threading.Tasks;

	public class FirebirdDataProvider : DynamicDataProviderBase<FirebirdProviderAdapter>
	{
		public FirebirdDataProvider()
			: this(ProviderName.Firebird, new FirebirdMappingSchema(), null)
		{
		}

		public FirebirdDataProvider(ISqlOptimizer sqlOptimizer)
			: this(ProviderName.Firebird, new FirebirdMappingSchema(), sqlOptimizer)
		{
		}

		protected FirebirdDataProvider(string name, MappingSchema mappingSchema, ISqlOptimizer? sqlOptimizer)
			: base(name, mappingSchema, FirebirdProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = sqlOptimizer ?? new FirebirdSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new FirebirdSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new FirebirdSchemaProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is bool)
			{
				value = (bool)value ? "1" : "0";
				dataType = dataType.WithDataType(DataType.Char);
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.SByte      : dataType = dataType.WithDataType(DataType.Int16);    break;
				case DataType.UInt16     : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32     : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64     : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2  : dataType = dataType.WithDataType(DataType.DateTime); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#if !NET45 && !NET46
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}
#endif

		#endregion
	}
}
