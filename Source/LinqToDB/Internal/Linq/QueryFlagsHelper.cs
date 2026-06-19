namespace LinqToDB.Internal.Linq
{
	internal static class QueryFlagsHelper
	{
		public static QueryFlags GetQueryFlags(this IDataContext dataContext)
		{
			// calculate query flags
			var flags = QueryFlags.None;

			if (dataContext.InlineParameters)
				flags |= QueryFlags.InlineParameters;

			return flags;
		}
	}
}
