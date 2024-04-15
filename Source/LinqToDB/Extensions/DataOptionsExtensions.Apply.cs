namespace LinqToDB
{
	using Common;
	using Data;
	using Remote;

	partial class DataOptionsExtensions
	{
		public static void Apply(this DataOptions options, DataConnection dataConnection)
		{
			((IApplicable<DataConnection>)options.ConnectionOptions).Apply(dataConnection);
			((IApplicable<DataConnection>)options.RetryPolicyOptions).Apply(dataConnection);

			if (options.DataContextOptions is IApplicable<DataConnection> a)
				a.Apply(dataConnection);

			options.ApplySets(dataConnection);
		}

		public static void Apply(this DataOptions options, DataContext dataContext)
		{
			((IApplicable<DataContext>)options.ConnectionOptions).Apply(dataContext);

			if (options.DataContextOptions is IApplicable<DataContext> a)
				a.Apply(dataContext);

			options.ApplySets(dataContext);
		}

		public static void Apply(this DataOptions options, RemoteDataContextBase dataContext)
		{
			((IApplicable<RemoteDataContextBase>)options.ConnectionOptions).Apply(dataContext);

			if (options.DataContextOptions is IApplicable<RemoteDataContextBase> a)
				a.Apply(dataContext);

			options.ApplySets(dataContext);
		}

		public static void ApplySets<TA>(this DataOptions options, TA obj)
		{
			foreach (var item in options.OptionSets)
				if (item is IApplicable<TA> a)
					a.Apply(obj);
		}
	}
}
