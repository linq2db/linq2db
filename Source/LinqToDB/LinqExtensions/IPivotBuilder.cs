using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Builder surface used inside a <see cref="LinqExtensions.Pivot{TSource,TResult}"/> projection to declare the
	/// grouping key and the pivoted aggregate cells. Instances exist only as part of a parsed expression tree —
	/// the members are never invoked at runtime.
	/// </summary>
	/// <typeparam name="TSource">Source table record type.</typeparam>
	public interface IPivotBuilder<TSource>
	{
		/// <summary>The source row. Any member referenced through it (and not inside an aggregate) becomes a GROUP BY / pass-through column.</summary>
		TSource Key { get; }

		/// <summary>A <c>SUM</c> cell: sum of <paramref name="value"/> where <paramref name="forColumn"/> equals <paramref name="forValue"/>.</summary>
		/// <typeparam name="TValue">Aggregated value type.</typeparam>
		/// <typeparam name="TFor">Pivot key type (use an anonymous type for a composite/multi-column pivot).</typeparam>
		/// <param name="value">Value column to aggregate.</param>
		/// <param name="forColumn">The pivot (<c>FOR</c>) column(s).</param>
		/// <param name="forValue">The constant pivot value producing this output column.</param>
		TValue Sum<TValue, TFor>(Expression<Func<TSource, TValue>> value, Expression<Func<TSource, TFor>> forColumn, [SqlQueryDependent] TFor forValue);

		/// <summary>A <c>MIN</c> cell (see <see cref="Sum{TValue,TFor}"/>).</summary>
		TValue Min<TValue, TFor>(Expression<Func<TSource, TValue>> value, Expression<Func<TSource, TFor>> forColumn, [SqlQueryDependent] TFor forValue);

		/// <summary>A <c>MAX</c> cell (see <see cref="Sum{TValue,TFor}"/>).</summary>
		TValue Max<TValue, TFor>(Expression<Func<TSource, TValue>> value, Expression<Func<TSource, TFor>> forColumn, [SqlQueryDependent] TFor forValue);

		/// <summary>A <c>COUNT</c> cell (see <see cref="Sum{TValue,TFor}"/>).</summary>
		int Count<TValue, TFor>(Expression<Func<TSource, TValue>> value, Expression<Func<TSource, TFor>> forColumn, [SqlQueryDependent] TFor forValue);

		/// <summary>An <c>AVG</c> cell (see <see cref="Sum{TValue,TFor}"/>).</summary>
		double? Avg<TValue, TFor>(Expression<Func<TSource, TValue>> value, Expression<Func<TSource, TFor>> forColumn, [SqlQueryDependent] TFor forValue);
	}
}
