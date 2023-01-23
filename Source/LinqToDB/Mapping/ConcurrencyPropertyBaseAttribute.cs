using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Identifies optimistic concurrency column behavior/strategy column and strategy.
	/// Used with <see cref="ConcurrencyExtensions" /> extensions, e.g. <see cref="ConcurrencyExtensions.UpdateConcurrent{T}(IDataContext, T)"/> or <see cref="ConcurrencyExtensions.UpdateConcurrentAsync{T}(IDataContext, T, System.Threading.CancellationToken)"/> methods.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public abstract class ConcurrencyPropertyBaseAttribute : MappingAttribute
	{
		public ConcurrencyPropertyBaseAttribute()
		{
		}

		public abstract LambdaExpression? GetNextValue(ColumnDescriptor column, ParameterExpression record);

		public override string GetObjectID() => $".{Configuration}.";
	}
}
