using System;
using LinqToDB.Linq;
using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	partial class Sql
	{
		public abstract class SqlRow<T1, T2>
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
		}

		[Sql.Expression(PN.Informix, "ROW ({0}, {1})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1})", ServerSideOnly = true)]
		public static SqlRow<T1, T2> Row<T1, T2>(T1 value1, T2 value2)
			=> throw new LinqException("Row is only server-side method.");

		// Nesting SqlRow looks inefficient, but it will never actually be instantiated.
		// It's only for static typing and it's good enough for that purpose 
		// without creating lots of types and operators.
		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, T3>> Row<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
			=> throw new LinqException("Row is only server-side method.");

		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2}, {3})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2}, {3})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, SqlRow<T3, T4>>> Row<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
			=> throw new LinqException("Row is only server-side method.");

		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2}, {3}, {4})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, T5>>>> Row<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
			=> throw new LinqException("Row is only server-side method.");

		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2}, {3}, {4}, {5})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2}, {3}, {4}, {5})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, T6>>>>> Row<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
			=> throw new LinqException("Row is only server-side method.");

		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2}, {3}, {4}, {5}, {6})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2}, {3}, {4}, {5}, {6})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, SqlRow<T6, T7>>>>>> Row<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
			=> throw new LinqException("Row is only server-side method.");

		[Sql.Expression(PN.Informix, "ROW ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})", ServerSideOnly = true)]
		[Sql.Expression("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})", ServerSideOnly = true)]
		public static SqlRow<T1, SqlRow<T2, SqlRow<T3, SqlRow<T4, SqlRow<T5, SqlRow<T6, SqlRow<T7, T8>>>>>>> Row<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
			=> throw new LinqException("Row is only server-side method.");			
	}
}
