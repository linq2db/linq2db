using System;
using System.Linq;

namespace LinqToDB
{
	public static partial class Sql
	{
		public interface IAggregateFunction<out T, out TR> : IQueryableContainer
		{

		}

		public interface IAggregateFunctionNotOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}

		public interface IAggregateFunctionOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}

		public class AggregateFunctionNotOrderedImpl<T, TR> : IAggregateFunctionNotOrdered<T, TR>, IAggregateFunctionOrdered<T, TR>
		{
			public AggregateFunctionNotOrderedImpl(IQueryable<TR> query)
			{
				Query = query ?? throw new ArgumentNullException(nameof(query));
			}

			public IQueryable Query { get; }
		}
	}
}
