using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Scalar, aggregate or window (analytic) function mapping attributes, used
	/// with <see cref="Sql.FunctionAttribute"/> mapping attribute.
	/// </summary>
	public sealed class FunctionMetadata
	{
		/// <summary>
		/// Function name.
		/// </summary>
		public SqlObjectName?      Name             { get; set; }
		/// <summary>
		/// Contains indexes of mapped method parameters, that should be mapped to function parameter with position
		/// matching position of index in array.
		/// </summary>
		public int[]?              ArgIndices       { get; set; }
		/// <summary>
		/// Mapping configuration name.
		/// </summary>
		public string?             Configuration    { get; set; }
		/// <summary>
		/// Prevent client-side method exection.
		/// </summary>
		public bool?               ServerSideOnly   { get; set; }
		/// <summary>
		/// Avoid client-side method execution when possible.
		/// </summary>
		public bool?               PreferServerSide { get; set; }
		/// <summary>
		/// Function parameters should be inlined into generated SQL as literals.
		/// </summary>
		public bool?               InlineParameters { get; set; }
		/// <summary>
		/// Marks <see cref="bool"/>-returning function as predicate: function, that could be used in boolean conditions
		/// directly without conversion to database boolean type/predicate.
		/// </summary>
		public bool?               IsPredicate      { get; set; }
		/// <summary>
		/// Function could be used as aggregate.
		/// </summary>
		public bool?               IsAggregate      { get; set; }
		/// <summary>
		/// Function could be used as window (analytic) function.
		/// </summary>
		public bool?               IsWindowFunction { get; set; }
		/// <summary>
		/// Pure functions (functions without side-effects that return same outputs for same inputs) calls
		/// information could be used by query optimizer to generate better SQL.
		/// </summary>
		public bool?               IsPure           { get; set; }
		/// <summary>
		/// Function could return NULL value.
		/// </summary>
		public bool?               CanBeNull        { get; set; }
		/// <summary>
		/// Provides more detailed nullability information than <see cref="CanBeNull"/> property.
		/// </summary>
		public Sql.IsNullableType? IsNullable       { get; set; }
	}
}
