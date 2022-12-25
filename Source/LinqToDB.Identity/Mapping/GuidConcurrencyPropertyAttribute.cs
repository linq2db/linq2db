using System;
using System.Linq.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Identity
{
	/// <summary>
	/// Default optimistic lock key generator.
	/// Uses <c>Guid.NewGuid().ToString()</c> string as new key value.
	/// </summary>
	public sealed class GuidConcurrencyPropertyAttribute : ConcurrencyPropertyAttribute
	{
		public static readonly MappingAttribute Instance = new GuidConcurrencyPropertyAttribute();

		public GuidConcurrencyPropertyAttribute()
			: base(default)
		{
		}

		public override LambdaExpression GetNextValue(ColumnDescriptor column, ParameterExpression record)
		{
			return Expression.Lambda(
				Expression.Constant(Guid.NewGuid().ToString()),
				record);
		}
	}
}
