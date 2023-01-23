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
	public class ConcurrencyPropertyAttribute : ConcurrencyPropertyBaseAttribute
	{
		public ConcurrencyPropertyAttribute(VersionBehavior behavior)
		{
			Behavior = behavior;
		}

		/// <summary>
		/// Versioning strategy.
		/// </summary>
		public VersionBehavior Behavior { get; }

		/// <summary>
		/// Implements generation of update value expression for current optimistic lock column.
		/// </summary>
		/// <param name="column">Column mapping descriptor.</param>
		/// <param name="record">Updated record.</param>
		/// <returns><c>null</c> to skip explicit column update or update expression.</returns>
		public override LambdaExpression? GetNextValue(ColumnDescriptor column, ParameterExpression record)
		{
			switch (Behavior)
			{
				case VersionBehavior.Auto:
					return null;

				case VersionBehavior.AutoIncrement:
					return Expression.Lambda(
						Expression.Add(column.MemberAccessor.GetterExpression.GetBody(record), Expression.Constant(1)),
						record);

				case VersionBehavior.Guid:
					return Expression.Lambda(
						Expression.Call(null, Methods.System.Guid_NewGuid),
						record);

				default:
					throw new ArgumentOutOfRangeException($"Unsupported {nameof(VersionBehavior)} value: {Behavior}");
			}
		}

		public override string GetObjectID()
		{
			// TODO: replace after merge with mapping refactorings
			//return $".{base.GetObjectID()}.{(int)Behavior}.";
			return $".{(int)Behavior}.";
		}
	}
}
