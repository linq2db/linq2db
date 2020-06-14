using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Linq.Builder;

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

			if (obj1 is IEnumerable list1 && obj2 is IEnumerable list2)
			{
				var enum1 = list1.GetEnumerator();
				var enum2 = list2.GetEnumerator();
				using (enum1 as IDisposable)
				using (enum2 as IDisposable)
				{
					while (enum1.MoveNext())
					{
						if (!enum2.MoveNext() || !object.Equals(enum1.Current, enum2.Current))
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
		/// <param name="expr1"></param>
		/// <param name="expr2"></param>
		/// <param name="comparer">Default function for comparing expressions.</param>
		/// <returns>Result of comparison</returns>
		public virtual bool ExpressionsEqual(Expression expr1, Expression expr2,
			Func<Expression, Expression, bool> comparer)
		{
			return ObjectsEqual(expr1.EvaluateExpression(), expr2.EvaluateExpression());
		}

		/// <summary>
		/// Used for preparation method argument to cached expression value.
		/// </summary>
		/// <param name="expression">Expression for caching.</param>
		/// <returns>Ready to cache expression.</returns>
		public virtual Expression PrepareForCache(Expression expression)
		{
			return Expression.Constant(expression.EvaluateExpression());
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
