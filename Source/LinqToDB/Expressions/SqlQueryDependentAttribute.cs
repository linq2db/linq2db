using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Expressions.Internal;

namespace LinqToDB.Expressions
{
	/// <summary>
	/// Used for controlling query caching of custom SQL Functions.
	/// Parameter with this attribute will be evaluated on client side before generating SQL.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class SqlQueryDependentAttribute : Attribute
	{
		/// <summary>
		/// Compares two objects during expression tree comparison. Handles sequences also.
		/// Has to be overriden if specific comparison required.
		/// </summary>
		/// <param name="obj1"></param>
		/// <param name="obj2"></param>
		/// <returns>Result of comparison</returns>
		public virtual bool ObjectsEqual(object? obj1, object? obj2)
		{
			if (ReferenceEquals(obj1, obj2))
				return true;

			// if both null, ReferenceEquals will return true
			if (obj1 == null || obj2 == null)
				return false;

			if (obj1 is RawSqlString str1 && obj2 is RawSqlString str2)
				return str1.Format == str2.Format;

			if (obj1 is not string and IEnumerable list1 && obj2 is IEnumerable list2)
			{
				var enum1 = list1.GetEnumerator();
				var enum2 = list2.GetEnumerator();
				using (enum1 as IDisposable)
				using (enum2 as IDisposable)
				{
					while (enum1.MoveNext())
					{
						if (!enum2.MoveNext() || !Equals(enum1.Current, enum2.Current))
							return false;
					}

					if (enum2.MoveNext())
						return false;
				}

				return true;
			}

			return obj1.Equals(obj2);
		}

		/// <summary>
		/// Compares two expressions during expression tree comparison.
		/// Has to be overriden if specific comparison required.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="expr1"></param>
		/// <param name="expr2"></param>
		/// <param name="comparer">Default function for comparing expressions.</param>
		/// <returns>Result of comparison</returns>
		public virtual bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2,
			Func<TContext, Expression, Expression, bool> comparer)
		{
			return ObjectsEqual(expr1.EvaluateExpression(), expr2.EvaluateExpression());
		}

		/// <summary>
		/// Used for preparation method argument to cached expression value.
		/// </summary>
		/// <param name="expression">Expression for caching.</param>
		/// <returns>Ready to cache expression.</returns>
		public virtual Expression PrepareForCache(Expression expression, IExpressionEvaluator evaluator)
		{
			if (expression.NodeType == ExpressionType.Constant)
				return expression;

			if (expression.NodeType == ExpressionType.NewArrayInit)
			{
				var arrayInit   = (NewArrayExpression)expression;
				var elementType = arrayInit.Type.GetElementType() ?? throw new InvalidOperationException();

				Expression[]? newExpressions = null;

				for (var i = 0; i < arrayInit.Expressions.Count; i++)
				{
					var arg      = arrayInit.Expressions[i];
					var newValue = PrepareForCache(arg, evaluator);
					if (!ReferenceEquals(newValue, arg))
					{
						newExpressions ??= arrayInit.Expressions.ToArray();

						newExpressions[i] = newValue;
					}
				}

				if (newExpressions != null)
					return arrayInit.Update(newExpressions);

				return expression;
			}

			if (evaluator.CanBeEvaluated(expression))
				return Expression.Constant(evaluator.Evaluate(expression), expression.Type);

			return expression;
		}

		/// <summary>
		/// Returns sub-expressions, if attribute applied to composite expression.
		/// Default (non-composite) implementation returns <paramref name="expression"/>.
		/// </summary>
		/// <param name="expression">Expression to split.</param>
		/// <returns>Passed expression of sub-expressions for composite expression.</returns>
		public virtual IEnumerable<Expression> SplitExpression(Expression expression)
		{
			yield return expression;
		}
	}
}
