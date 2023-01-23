using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Implements built-in optimistic lock value generation strategies for updates.
	/// See <see cref="VersionBehavior"/> for supported strategies.
	/// Used with <see cref="ConcurrencyExtensions" /> extensions:
	/// <list type="bullet">
	/// <item><see cref="ConcurrencyExtensions.UpdateOptimistic{T}(IDataContext, T)"/></item>
	/// <item><see cref="ConcurrencyExtensions.UpdateOptimisticAsync{T}(IDataContext, T, System.Threading.CancellationToken)"/></item>
	/// <item><see cref="ConcurrencyExtensions.DeleteOptimistic{T}(IDataContext, T)"/></item>
	/// <item><see cref="ConcurrencyExtensions.DeleteOptimisticAsync{T}(IDataContext, T, System.Threading.CancellationToken)"/></item>
	/// </list>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class OptimisticLockPropertyAttribute : OptimisticLockPropertyBaseAttribute
	{
		private static readonly Expression _newGuidCall       = Expression.Call(null, Methods.System.Guid_NewGuid);
		private static readonly Expression _newGuidStringCall = Expression.Call(_newGuidCall, Methods.System.Guid_ToString);
		private static readonly Expression _newGuidArrayCall  = Expression.Call(_newGuidCall, Methods.System.Guid_ToByteArray);

		public OptimisticLockPropertyAttribute(VersionBehavior behavior)
		{
			Behavior = behavior;
		}

		/// <summary>
		/// Version column value generation strategy.
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
						Expression.Add(column.MemberAccessor.GetterExpression.GetBody(record), ExpressionInstances.Constant1),
						record);

				case VersionBehavior.Guid:
					if (column.MemberType == typeof(Guid))   return Expression.Lambda(_newGuidCall, record);
					if (column.MemberType == typeof(string)) return Expression.Lambda(_newGuidStringCall, record);
					if (column.MemberType == typeof(byte[])) return Expression.Lambda(_newGuidArrayCall, record);

					throw new LinqToDBException($"Unsupported column type '{column.MemberType}' for {nameof(VersionBehavior)}.{nameof(VersionBehavior.Guid)}");

				default:
					throw new ArgumentOutOfRangeException($"Unsupported {nameof(VersionBehavior)} value: {Behavior}");
			}
		}

		public override string GetObjectID()
		{
			return $".{Configuration}.{(int)Behavior}.";
		}
	}
}
