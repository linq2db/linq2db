using System;
using Config = LinqToDB.Common.Configuration;

namespace LinqToDB.SqlQuery
{
	public class AstFactory
	{
		#region Ctor

		public AstFactory() 
		{
			// REVIEW(jods): a real True/False predicate node might be nice for DBs that have BOOLEAN support
			TruePredicate = Equal(One, One);
			FalsePredicate = Equal(One, Zero);
		}

		#endregion

		#region Predicates 

		public ISqlPredicate IsNotNull(ISqlExpression expr) => IsNull(expr, isNot: true);

		public ISqlPredicate IsNull(ISqlExpression expr, bool isNot = false)
		{
			return expr.CanBeNull
				? new SqlPredicate.IsNull(expr, isNot)
				: isNot ? TruePredicate : FalsePredicate;
		}

		public ISqlPredicate Equal(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.Equal, right, configNulls);

		public ISqlPredicate NotEqual(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.NotEqual, right, configNulls);

		public ISqlPredicate Greater(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.Greater, right, configNulls);

		public ISqlPredicate GreaterEqual(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.GreaterOrEqual, right, configNulls);

		public ISqlPredicate NotGreaterEqual(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.NotGreater, right, configNulls);

		public ISqlPredicate Less(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.Less, right, configNulls);

		public ISqlPredicate LessEqual(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.LessOrEqual, right, configNulls);

		public ISqlPredicate NotLess(ISqlExpression left, ISqlExpression right, bool configNulls = false)
			=> Comparison(left, SqlPredicate.Operator.NotLess, right, configNulls);

		public ISqlPredicate Overlaps(ISqlExpression left, ISqlExpression right)
			=> Comparison(left, SqlPredicate.Operator.Overlaps, right, configNulls: false);

		public ISqlPredicate Comparison(
			ISqlExpression left, 
			SqlPredicate.Operator op, 
			ISqlExpression right, 
			bool configNulls = false)
		{
			bool? withNull = configNulls && Config.Linq.CompareNullsAsValues ? true : null;
			return new SqlPredicate.ExprExpr(left, op, right, withNull);
		}

		public SqlSearchCondition And(params ISqlPredicate[] operands)
			=> And((ICollection<ISqlPredicate>)operands);

		public SqlSearchCondition And(ICollection<ISqlPredicate> operands)
		{
			// TODO(jods): ensure nested AND are flattened. It's done by the Expression parser for efficiency,
			// but it isn't guaranteed when manipulating the AST, e.g. by optimizations.
			return new SqlSearchCondition(
				operands.Select(x => new SqlCondition(isNot: false, x))
			);
		}

		public SqlSearchCondition Or(params ISqlPredicate[] operands)
			=> Or((ICollection<ISqlPredicate>)operands);

		public SqlSearchCondition Or(ICollection<ISqlPredicate> operands)
		{
			// TODO(jods): ensure nested OR are flattened. It's done by the Expression parser for efficiency,
			// but it isn't guaranteed when manipulating the AST, e.g. by optimizations.
			return new SqlSearchCondition(
				operands.Select(x => new SqlCondition(isNot: false, x, isOr: true))
			);
		}

		public ISqlPredicate Not(ISqlPredicate a)
		{
			return a is IInvertibleElement { CanInvert: true } i
				? i.Invert()
				: new SqlPredicate.NotExpr(a);
		}

		// REVIEW(jods): this is ugly and inefficient, can't all ISqlPredicate be ISqlExpression?
		// Or completely split ISqlExpresion and ISqlPredicate (after all they are used in different contexts).
		// Or at least can we have a ISqlExpression node that is more efficient at representing a predicate than SqlSearchCondition!
		public ISqlExpression ToExpression(ISqlPredicate predicate)
		{
			return predicate is ISqlExpression expr
				? expr
				: new SqlSearchCondition(
					new SqlCondition(isNot: false, predicate)
				);
		}

		#endregion

		#region Expressions:Constants

		// Cached commonly used nodes
		public readonly SqlValue MinusOne = new SqlValue(-1);
		public readonly SqlValue Zero     = new SqlValue(0);
		public readonly SqlValue One      = new SqlValue(1);

		public readonly SqlValue True     = new SqlValue(true);
		public readonly SqlValue False    = new SqlValue(false);

		public readonly ISqlPredicate TruePredicate;
		public readonly ISqlPredicate FalsePredicate;

		#endregion

		#region Expressions:Numbers

		public ISqlExpression Negate(ISqlExpression a, Type sysType)
			=> new SqlBinaryExpression(sysType, MinusOne, "*", a, Precedence.Multiplicative);
		public ISqlExpression Add(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "+", b, Precedence.Additive);

		public ISqlExpression Subtract(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "-", b, Precedence.Subtraction);

		public ISqlExpression Multiply(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "*", b, Precedence.Multiplicative);

		public ISqlExpression Divide(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "/", b, Precedence.Multiplicative);

		public ISqlExpression Mod(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "%", b, Precedence.Multiplicative);

		public ISqlExpression Power(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlFunction(sysType, "Power", a, b);

		#endregion

		#region Expressions:Bits

		public ISqlExpression BitAnd(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "&", b, Precedence.Bitwise);

		public ISqlExpression BitOr(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "|", b, Precedence.Bitwise);

		public ISqlExpression BitXor(ISqlExpression a, ISqlExpression b, Type sysType)
			=> new SqlBinaryExpression(sysType, a, "^", b, Precedence.Bitwise);

		#endregion

		#region Expressions:Logic

		public ISqlExpression Coalesce(ISqlExpression a, ISqlExpression b, Type sysType)
		{
			// Flatten nested Coalesce calls
			var ua = QueryHelper.UnwrapExpression(a);
			var ub = QueryHelper.UnwrapExpression(b);
			ISqlExpression[] values = (ua, ub) switch
			{
				(SqlFunction { Name: "Coalesce" or PseudoFunctions.COALESCE } ca, 
				 SqlFunction { Name: "Coalesce" or PseudoFunctions.COALESCE } cb)    => ToArray(ca.Parameters, cb.Parameters),
				(SqlFunction { Name: "Coalesce" or PseudoFunctions.COALESCE } ca, _) => ToArray(ca.Parameters, b),
				(_, SqlFunction { Name: "Coalesce" or PseudoFunctions.COALESCE } cb) => ToArray(a, cb.Parameters),
				_ => new[] { a, b },
			};

			return new SqlFunction(sysType, PseudoFunctions.COALESCE, isAggregate: false, isPure: true, values)
			{
				CanBeNull = values.All(v => v.CanBeNull),
			};
		}

		public ISqlExpression If(ISqlExpression condition, ISqlExpression whenTrue, ISqlExpression whenFalse, Type sysType)
		{			
			// Flatten chained else-if calls
			var uf = QueryHelper.UnwrapExpression(whenFalse);
			if (uf is SqlFunction { Name: "CASE" } f)
			{
				return new SqlFunction(sysType, "CASE", ToArray(condition, whenTrue, f.Parameters))
				{
					CanBeNull = whenTrue.CanBeNull | whenFalse.CanBeNull
				};
			}

			return new SqlFunction(sysType, "CASE", condition, whenTrue, whenFalse) 
			{ 
				CanBeNull = whenTrue.CanBeNull | whenFalse.CanBeNull,
			};
		}

		#endregion

		#region Expressions:Types

		public ISqlExpression Convert(SqlDataType toType, ISqlExpression value, SqlDataType? fromType = null)
		{
			return new SqlFunction(
				toType.SystemType, 
				PseudoFunctions.CONVERT, 
				isAggregate: false, 
				isPure: true, 
				toType, 
				fromType ?? new SqlDataType(value.GetExpressionType()), 
				value)
			{
				CanBeNull = value.CanBeNull
			};
		}

		#endregion

		#region Private utilities

		private static T[] ToArray<T>(T[] a, T[] b)
		{
			var result = new T[a.Length + b.Length];
			a.CopyTo(result, 0);
			b.CopyTo(result, a.Length);
			return result;
		}

		private static T[] ToArray<T>(T[] a, T b)
		{
			var result = new T[a.Length + 1];
			a.CopyTo(result, 0);
			result[a.Length] = b;
			return result;
		}

		private static T[] ToArray<T>(T a, T[] b)
		{
			var result = new T[1 + b.Length];
			result[0] = a;
			b.CopyTo(result, 1);
			return result;
		}

		private static T[] ToArray<T>(T a, T b, T[] c)
		{
			var result = new T[2 + c.Length];
			result[0] = a;
			result[1] = b;
			c.CopyTo(result, 2);
			return result;
		}

		#endregion 
	}
}
