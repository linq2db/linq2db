using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Informix
{
	class InformixMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public InformixMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
			: base(merge, providerName)
		{
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// Informix doesn't support INSERT FROM
				return true;
			}
		}

		protected override bool OperationPredicateSupported
		{
			get
			{
				// operation conditions not supported
				return false;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// VALUES(...) syntax not supported in MERGE source
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				// or
				// sysmaster:'informix'.sysdual
				return "table(set{1})";
			}
		}

		protected override bool SupportsParametersInSource
		{
			get
			{
				// parameters in source select list not supported
				return false;
			}
		}

		protected override void AddFakeSourceTableName()
		{
			Command.Append(FakeSourceTable);
		}

		protected override void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor column,
			SqlDataType columnType,
			object value)
		{
			if (value != null)
			{
				base.AddSourceValue(valueConverter, column, columnType, value);
				return;
			}

			// Informix NULL values are typed and usually it can type them from context
			// source select is one of those places where it cannot infer type, so we should specify it explicitly
			Command.Append("NULL::");
			if (column.DbType != null)
				Command.Append(column.DbType);
			else
			{
				if (columnType.DataType == DataType.Undefined)
				{
					columnType = ContextInfo.DataContext.MappingSchema.GetDataType(column.StorageType);

					if (columnType.DataType == DataType.Undefined)
					{
						var canBeNull = column.CanBeNull;

						columnType = ContextInfo.DataContext.MappingSchema.GetUnderlyingDataType(column.StorageType, ref canBeNull);
					}
				}

				SqlBuilder.BuildTypeName(Command, columnType);
			}
		}
	}
}
