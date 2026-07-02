using System;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	public partial class WindowFunctionsTests
	{
		static SqlExtendedFunction MakeCovarPop(ISqlExpression x, ISqlExpression y)
			=> new(
				new DbDataType(typeof(double)),
				"COVAR_POP",
				new[] { new SqlFunctionArgument(x), new SqlFunctionArgument(y) },
				new[] { true, true },
				isAggregate: true);

		// Regression: SqlExtendedFunction argument equality must be positional and symmetric, matching the
		// in-order GetElementHashCode. COVAR_POP(a, b) and COVAR_POP(b, a) are different functions, and
		// COVAR_POP(a, a) differs from COVAR_POP(a, b); an order-insensitive comparison can collapse them in
		// common-subexpression elimination / the query cache and return a wrong result.
		[Test]
		public void SqlExtendedFunction_ArgumentEquality_IsPositionalAndSymmetric()
		{
			var a = new SqlValue(1.0);
			var b = new SqlValue(2.0);

			Func<ISqlExpression, ISqlExpression, bool> comparer = static (x, y) => ReferenceEquals(x, y);

			var funcAB = MakeCovarPop(a, b);
			var funcBA = MakeCovarPop(b, a);
			var funcAA = MakeCovarPop(a, a);

			// Argument order is semantically load-bearing: COVAR_POP(a, b) != COVAR_POP(b, a).
			Assert.That(funcAB.Equals(funcBA, comparer), Is.False);

			// COVAR_POP(a, a) != COVAR_POP(a, b), and equality must be symmetric.
			Assert.That(funcAA.Equals(funcAB, comparer), Is.False);
			Assert.That(funcAA.Equals(funcAB, comparer), Is.EqualTo(funcAB.Equals(funcAA, comparer)));
		}
	}
}
