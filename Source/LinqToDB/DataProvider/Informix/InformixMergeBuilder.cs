using LinqToDB.Data;
using LinqToDB.Linq;
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

		// Informix doesn't support INSERT FROM
		protected override bool ProviderUsesAlternativeUpdate => true;

		// operation conditions not supported
		protected override bool OperationPredicateSupported => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool SupportsSourceDirectValues => false;

		// or
		// sysmaster:'informix'.sysdual
		protected override string FakeSourceTable => "table(set{1})";

		protected override void AddFakeSourceTableName()
		{
			Command.Append(FakeSourceTable);
		}

		protected override void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor    column,
			SqlDataType         columnType,
			object              value,
			bool                isFirstRow,
			bool                isLastRow)
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
			BuildColumnType(column, columnType);
		}

		protected override bool MergeHintsSupported => true;

	}
}
