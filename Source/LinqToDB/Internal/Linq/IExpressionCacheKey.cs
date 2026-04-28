namespace LinqToDB.Internal.Linq
{
	/// <summary>
	///     Marks a value stored in a <see cref="System.Linq.Expressions.ConstantExpression"/> as
	///     participating in the linq2db query cache key. Types implementing this interface are kept
	///     in the cached expression (not replaced with a <c>ConstantPlaceholderExpression</c>) so
	///     that distinct instances hash and compare by value via their own
	///     <see cref="object.GetHashCode"/> and <see cref="object.Equals(object?)"/> overrides.
	/// </summary>
	/// <remarks>
	///     Implementers must be immutable and provide value-based equality.
	/// </remarks>
	public interface IExpressionCacheKey
	{
	}
}
