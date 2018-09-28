using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.DB2
{
	class DB2MergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public DB2MergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		// DB2 doesn't support INSERT FROM (well, except latest DB2 LUW version, but linq2db doesn't support it yet)
		protected override bool ProviderUsesAlternativeUpdate => true;

		private readonly IDictionary<ColumnDescriptor, bool> _columnTypedTracker = new Dictionary<ColumnDescriptor, bool>();

		protected override void AddSourceValue(ValueToSqlConverter valueConverter, ColumnDescriptor column, SqlDataType columnType, object value, bool isFirstRow, bool isLastRow)
		{
			if (!_columnTypedTracker.ContainsKey(column))
				_columnTypedTracker.Add(column, value != null);

			if (value == null)
			{
				/* DB2 doesn't like NULLs without type information
				 : ERROR [42610] [IBM][DB2/NT64] SQL0418N  The statement was not processed because the statement
				 contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword
				 , or a null value.

				See https://stackoverflow.com/questions/13381898

				Unfortunatelly, just use typed parameter doesn't help

				To fix it we need to cast at least one NULL in column if all column values are null.
				We will do it for last row, when we know that there is no non-null values in column and type hint
				needed.

				One thing I don't like is that in some cases DB2 can process query without type hints
				*/

				if (isLastRow && !_columnTypedTracker[column])
				{
					// we type only NULLs in last row and only if there is no non-NULL values in other rows
					// adding casts in other cases not required and could even lead to type conflicts if type in
					// cast not compatible with non-null values in column
					Command.Append("CAST(NULL AS ");
					BuildColumnType(column, columnType);
					Command.Append(")");
					return;
				}
			}
			else
				_columnTypedTracker[column] = true;

			base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
		}
	}
}
