using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
	/// <summary>
	/// Marks the raw SQL <see cref="FormattableString"/> parameter of <c>FromSql</c>, <c>FromSqlScalar</c> and <c>Sql.Expr</c>.
	/// Only the format is evaluated on client side; the arguments are translated as part of the query and become parameters.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	sealed class SqlQueryDependentFormatAttribute : SqlQueryDependentAttribute
	{
		public override bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2,
			Func<TContext, Expression, Expression, bool> comparer)
		{
			// The format is compared by value, the arguments as any other expression of the query. The arguments can
			// reference query parameters, so a split side is never evaluated.
			if (TrySplit(expr1, out var format1, out var arguments1))
			{
				return TrySplit(expr2, out var format2, out var arguments2)
					&& string.Equals(format1.EvaluateExpression<string>(), format2.EvaluateExpression<string>(), StringComparison.Ordinal)
					&& comparer(context, arguments1, arguments2);
			}

			if (TrySplit(expr2, out _, out _))
				return false;

			return base.ExpressionsEqual(context, expr1, expr2, comparer);
		}

		/// <summary>
		/// Splits <c>FormattableStringFactory.Create(format, new object[] { ... })</c> into its format and argument array.
		/// </summary>
		public static bool TrySplit(Expression expression, [NotNullWhen(true)] out Expression? format, [NotNullWhen(true)] out NewArrayExpression? arguments)
		{
			if (expression is MethodCallExpression { Arguments: [var f, NewArrayExpression { NodeType: ExpressionType.NewArrayInit } a] } create
				&& create.Method == Methods.System.FormattableStringFactory_Create)
			{
				format    = f;
				arguments = a;
				return true;
			}

			format    = null;
			arguments = null;
			return false;
		}
	}
}
