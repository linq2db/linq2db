using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace LinqToDB.Internal
{
	/// <summary>
	/// Internal data context helpers.
	/// </summary>
	static class DataContextExtensions
	{
		public static IReadOnlyCollection<string>? GetNextCommandHints(this IDataContext context, bool clearNextHints)
		{
			if (context.QueryHints.Count > 0 || context.NextQueryHints.Count > 0)
			{
				// it is safe to use existing collection as optimization as we don't save and/or use returned collection
				// after command execution
				if (context.NextQueryHints.Count == 0)
					return context.QueryHints;

				var queryHints = new List<string>(context.QueryHints.Count + context.NextQueryHints.Count);
				queryHints.AddRange(context.QueryHints);
				queryHints.AddRange(context.NextQueryHints);

				if (clearNextHints)
					context.NextQueryHints.Clear();

				return queryHints;
			}

			return null;
		}
	}
}
