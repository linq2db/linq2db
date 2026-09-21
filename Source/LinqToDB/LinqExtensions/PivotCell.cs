using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB
{
	/// <summary>
	/// One cell template of a dynamic pivot: an aggregate over the source rows carrying a pivoted value, crossed
	/// with every pivoted value to produce one output column per (template, value) pair. Declared statically -
	/// only the pivoted values are runtime data.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	public sealed class PivotCell<TSource, TFor>
	{
		PivotCell(LambdaExpression aggregate, bool liftResult, Func<TFor, string>? name)
		{
			Aggregate  = aggregate;
			LiftResult = liftResult;
			Name       = name;
		}

		// Func<IEnumerable<TSource>, TCell>, applied to the rows matching one pivoted value.
		internal LambdaExpression    Aggregate  { get; }
		internal bool                LiftResult { get; }
		internal Func<TFor, string>? Name       { get; }

		/// <summary>
		/// A cell computed by an arbitrary aggregate over the source rows carrying the pivoted value - a distinct
		/// count, or anything else the provider can translate over a grouping.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">Aggregate over the matching rows.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		public static PivotCell<TSource, TFor> Custom<TCell>(Expression<Func<IEnumerable<TSource>, TCell>> aggregate, Func<TFor, string>? name = null)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			// A cell no row matches must read null rather than default(TCell), which is what a non-nullable
			// result type would otherwise materialize.
			return new PivotCell<TSource, TFor>(aggregate, !typeof(TCell).IsNullableOrReferenceType, name);
		}

		/// <summary>A <c>SUM</c> cell.</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		public static PivotCell<TSource, TFor> Sum<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Named(nameof(Enumerable.Sum), value, name);

		/// <summary>A <c>MIN</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Min<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Named(nameof(Enumerable.Min), value, name);

		/// <summary>A <c>MAX</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Max<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Named(nameof(Enumerable.Max), value, name);

		/// <summary>An <c>AVG</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Avg<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Named(nameof(Enumerable.Average), value, name);

		/// <summary>A <c>COUNT</c> cell: counts the rows carrying the pivoted value (see <see cref="Sum{TCell}"/>).</summary>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Count(Func<TFor, string>? name = null)
		{
			var rowsParam = Expression.Parameter(typeof(IEnumerable<TSource>), "rows");
			var method    = Methods.Enumerable.Count.MakeGenericMethod(typeof(TSource));

			// COUNT returns 0 for an empty group on every provider, so it is the one cell that is not lifted.
			return new PivotCell<TSource, TFor>(Expression.Lambda(Expression.Call(method, rowsParam), rowsParam), false, name);
		}

		static PivotCell<TSource, TFor> Named<TCell>(string methodName, Expression<Func<TSource, TCell>> value, Func<TFor, string>? name)
		{
			ArgumentNullException.ThrowIfNull(value);

			var rowsParam = Expression.Parameter(typeof(IEnumerable<TSource>), "rows");
			var rowParam  = Expression.Parameter(typeof(TSource), "row");

			var cellType = typeof(TCell).MakeNullable();
			var body     = value.GetBody(rowParam);
			var selector = Expression.Lambda(body.Type == cellType ? body : Expression.Convert(body, cellType), rowParam);

			return new PivotCell<TSource, TFor>(
				Expression.Lambda(Expression.Call(GetAggregate(methodName, cellType), rowsParam, selector), rowsParam),
				false,
				name);
		}

		static MethodInfo GetAggregate(string methodName, Type cellType)
		{
			if (string.Equals(methodName, nameof(Enumerable.Sum), StringComparison.Ordinal)
				|| string.Equals(methodName, nameof(Enumerable.Average), StringComparison.Ordinal))
			{
				// Sum and Average are overloaded per numeric type rather than generic in the result.
				var method = typeof(Enumerable).GetMethods()
					.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal)
						&& m.IsGenericMethodDefinition
						&& m.GetParameters().Length == 2
						&& m.GetParameters()[1].ParameterType.IsGenericType
						&& m.GetParameters()[1].ParameterType.GetGenericArguments()[1] == cellType);

				if (method == null)
					throw new LinqToDBException($"Pivot cannot apply {methodName} to a column of type '{(Nullable.GetUnderlyingType(cellType) ?? cellType).Name}'.");

				return method.MakeGenericMethod(typeof(TSource));
			}

			return typeof(Enumerable).GetMethods()
				.First(m => string.Equals(m.Name, methodName, StringComparison.Ordinal)
					&& m.IsGenericMethodDefinition
					&& m.GetGenericArguments().Length == 2
					&& m.GetParameters().Length == 2)
				.MakeGenericMethod(typeof(TSource), cellType);
		}
	}
}
