using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Query cache support for CTE-defining calls. The builder merges two occurrences of such a call into one CTE when
	/// their constants compare equal under <see cref="ExpressionEqualityComparer"/>, while the cache key replaces those
	/// constants with placeholders. Recording which constants are equal lets only an expression that the builder would
	/// merge the same way reuse the cached query.
	/// </summary>
	static class CteConstantsPattern
	{
		static readonly MethodInfo _computeMethod = typeof(CteConstantsPattern).GetMethod(nameof(Compute))!;

		public static bool IsCteDefiningCall(MethodCallExpression call)
		{
			var method = call.Method;

			if (method.DeclaringType == typeof(LinqExtensions))
				return method.Name is nameof(LinqExtensions.AsCte) or nameof(LinqExtensions.AsCteInternal);

			if (method.DeclaringType == typeof(DataExtensions))
				return string.Equals(method.Name, nameof(DataExtensions.GetCte), StringComparison.Ordinal);

			return false;
		}

		/// <summary>
		/// Builds an expression that computes the pattern over the expression passed at cache lookup, and the pattern
		/// of <paramref name="mainExpression"/>. Returns <see langword="false"/> when fewer than two CTE-defining call
		/// positions carry a constant: no merge decision can exist then.
		/// </summary>
		public static bool TryCreate(
			Expression                            mainExpression,
			Expression                            mainExpressionPath,
			[NotNullWhen(true)] out Expression?   accessor,
			[NotNullWhen(true)] out string?       pattern)
		{
			var calls = new List<(Expression node, Expression path)>();

			mainExpression.Path(mainExpressionPath, calls, static (calls, e, p) =>
			{
				if (e is MethodCallExpression call && IsCteDefiningCall(call))
					calls.Add((e, p));
			});

			var constants = new List<(ConstantExpression node, Expression accessor)>();
			var positions = 0;

			foreach (var (node, path) in calls)
			{
				var count = constants.Count;

				node.Path(path, constants, static (constants, e, p) =>
				{
					if (e is ConstantExpression constant)
						constants.Add((constant, Expression.Property(p, ReflectionHelper.Constant.Value)));
				});

				if (constants.Count > count)
					positions++;
			}

			if (positions < 2)
			{
				accessor = null;
				pattern  = null;
				return false;
			}

			var types = constants.Select(c => c.node.Type).ToArray();

			pattern  = Compute(constants.Select(c => c.node.Value).ToArray(), types);
			accessor = Expression.Call(
				_computeMethod,
				Expression.NewArrayInit(typeof(object), constants.Select(c => c.accessor)),
				Expression.Constant(types));

			return true;
		}

		/// <summary>
		/// Numbers each constant by the first constant it compares equal to, using the builder's constant comparison.
		/// </summary>
		public static string Compute(object?[] values, Type[] types)
		{
			var pattern         = new StringBuilder(values.Length * 2);
			var representatives = new List<int>();

			for (var i = 0; i < values.Length; i++)
			{
				var found = -1;

				for (var r = 0; r < representatives.Count; r++)
				{
					var j = representatives[r];

					if (types[i] == types[j]
						&& ExpressionEqualityComparer.Instance.Equals(Expression.Constant(values[i], types[i]), Expression.Constant(values[j], types[j])))
					{
						found = r;
						break;
					}
				}

				if (found < 0)
				{
					found = representatives.Count;
					representatives.Add(i);
				}

				if (i > 0)
					pattern.Append(',');

				pattern.Append(found.ToString(CultureInfo.InvariantCulture));
			}

			return pattern.ToString();
		}
	}
}
