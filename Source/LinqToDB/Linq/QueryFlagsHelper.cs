namespace LinqToDB.Linq
{
	internal static class QueryFlagsHelper
	{
		public static QueryFlags GetQueryFlags(this IDataContext dataContext)
		{
			// calculate query flags
			var flags = QueryFlags.None;

			if (dataContext.InlineParameters)
				flags |= QueryFlags.InlineParameters;

			// TODO: here we have race condition due to flag being global setting
			// TODO: v4: move QueryFlags to IDataContext?
			// to fix it we must move flags to context level and remove global flags or invalidate caches on
			// global flag change
			if (Common.Configuration.Linq.GuardGrouping)
				flags |= QueryFlags.GroupByGuard;
			if (Common.Configuration.Linq.ParameterizeTakeSkip)
				flags |= QueryFlags.ParameterizeTakeSkip;
			if (Common.Configuration.Linq.PreferApply)
				flags |= QueryFlags.PreferApply;

			return flags;
		}
	}
}
