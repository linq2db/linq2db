using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

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

		protected override void AddSourceValue(ValueToSqlConverter valueConverter, ColumnDescriptor column, SqlDataType columnType, object value, bool isFirstRow)
		{
			if (value == null)
			{
				/* DB2 doesn't like NULLs without type information
				 : ERROR [42610] [IBM][DB2/NT64] SQL0418N  The statement was not processed because the statement
				 contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword
				 , or a null value.

				See https://stackoverflow.com/questions/13381898

				Unfortunatelly, just use typed parameter doesn't help too
				*/
				Command.Append("CAST(NULL AS ");
				BuildColumnType(column, columnType);
				Command.Append(")");
				return;
			}

			base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow);
		}
	}
}
