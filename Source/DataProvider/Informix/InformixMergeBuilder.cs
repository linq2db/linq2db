using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System;
using System.Globalization;

namespace LinqToDB.DataProvider.Informix
{
	class InformixMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public InformixMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
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
			// informix have really hard times to recognize it's own types, so in source we need to specify type
			// hint for most of types
			if (value != null)
			{
				if (!valueConverter.TryConvert(Command, columnType, value))
				{
					AddSourceValueAsParameter(column.DataType, value);

					// even for parameters
					WriteTypeHint(column, columnType);
				}
				else
				{
					if (value is decimal && columnType.Precision == null
						&& (columnType.DataType == DataType.Decimal || columnType.DataType == DataType.Undefined))
					{
						var decValue = (decimal)value;

						var precision = 0;
						var str = decValue.ToString(CultureInfo.InvariantCulture);
						var dotIndex = str.IndexOf(".");
						if (dotIndex >= 0)
						{
							precision = str.Substring(0, dotIndex).TrimStart('0').Length;
						}

						var scale = BitConverter.GetBytes(decimal.GetBits(decValue)[3])[2];
						precision += scale;

						columnType = new SqlDataType(DataType.Decimal, columnType.Type, null, precision, scale);
					}

					// this is the only place where hint is not required for some types but it doesn't make sense to
					// write extra logic to detect when it could be skipped
					WriteTypeHint(column, columnType);
				}
			}
			else
			{
				Command.Append("NULL");
				WriteTypeHint(column, columnType);
			}
		}

		private void WriteTypeHint(ColumnDescriptor column, SqlDataType columnType)
		{
			Command.Append("::");

			if (column.DbType != null)
				Command.Append(column.DbType);
			else
			{
				if (columnType.DataType == DataType.Undefined)
				{
					columnType = DataContext.MappingSchema.GetDataType(column.StorageType);

					if (columnType.DataType == DataType.Undefined)
					{
						var canBeNull = column.CanBeNull;

						columnType = DataContext.MappingSchema.GetUnderlyingDataType(column.StorageType, ref canBeNull);
					}
				}

				SqlBuilder.BuildTypeName(Command, columnType);
			}
		}
	}
}
