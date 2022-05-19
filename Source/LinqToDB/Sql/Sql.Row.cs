using System;

namespace LinqToDB;

using SqlQuery;
using PN = ProviderName;

partial class Sql
{
	public abstract class SqlRow<T1, T2> : IComparable
	{ 
		// Prevent someone from inheriting this class and creating instances.
		// This class is never instantiated and its operators are never actually called.
		// It's all just for typing in LINQ expressions that will translate to SQL.
		private SqlRow() {}

		public static bool operator > (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> throw new NotImplementedException();

		public static bool operator < (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> throw new NotImplementedException();

		public static bool operator >= (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> throw new NotImplementedException();

		public static bool operator <= (SqlRow<T1, T2> x, SqlRow<T1, T2> y)
			=> throw new NotImplementedException();

		public abstract int CompareTo(object? obj);
	}

	
	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]		
	public static SqlRow<T1, T2> Row<T1, T2>(T1 value1, T2 value2)
		=> throw new LinqToDBException("Row is only server-side method.");

	// Nesting SqlRow looks inefficient, but it will never actually be instantiated.
	// It's only for static typing and it's good enough for that purpose 
	// without creating lots of types and operators.
	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, T3>> Row<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
		=> throw new LinqToDBException("Row is only server-side method.");

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, SqlRow<T3, T4>>> Row<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
		=> throw new LinqToDBException("Row is only server-side method.");

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, T5>>>> Row<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
		=> throw new LinqToDBException("Row is only server-side method.");

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, T6>>>>> Row<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
		=> throw new LinqToDBException("Row is only server-side method.");

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, SqlRow<T6, T7>>>>>> Row<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
		=> throw new LinqToDBException("Row is only server-side method.");

	[Extension("", BuilderType = typeof(RowBuilder), ServerSideOnly = true)]
	public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, SqlRow<T6, SqlRow<T7, T8>>>>>>> Row<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
		=> throw new LinqToDBException("Row is only server-side method.");

	private class RowBuilder : IExtensionCallBuilder
	{
		public void Build(ISqExtensionBuilder builder)
		{
			var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));
			builder.ResultExpression = new SqlRow(args);
		}
	}

	// Note that SQL standard doesn't define OVERLAPS for all comparable data types, such as numbers.
	// RDBMS only support OVERLAPS for date(-time) and interval types.		
	[Extension("",  "", IsPredicate = true, ServerSideOnly = true, Precedence = Precedence.Comparison, BuilderType = typeof(OverlapsBuilder))]
	public static bool Overlaps<T1, T2, T3, T4>(this SqlRow<T1, T2> thisRow, SqlRow<T3, T4> other)
		=> throw new NotImplementedException();

	private class OverlapsBuilder : IExtensionCallBuilder
	{
		public void Build(ISqExtensionBuilder builder)
		{
			var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));
			builder.ResultExpression = new SqlSearchCondition(new SqlCondition(false,
				new SqlPredicate.ExprExpr(args[0], SqlPredicate.Operator.Overlaps, args[1], false)));
		}
	}
}
