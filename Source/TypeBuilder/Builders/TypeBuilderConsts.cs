namespace LinqToDB.TypeBuilder.Builders
{
	public static class TypeBuilderConsts
	{
		public static class Priority
		{
			public const int Low              = int.MinValue / 2;
			public const int Normal           = 0;
			public const int High             = int.MaxValue / 2;

			public const int NotNullAspect    = High;
			public const int OverloadAspect   = High;
			public const int AsyncAspect      = Normal;
			public const int ClearCacheAspect = Normal;
			public const int LoggingAspect    = Normal;
			public const int CacheAspect      = Low;
			public const int DataAccessor     = Low;
			public const int PropChange       = int.MinValue + 1000000;
		}

		public const string AssemblyNameSuffix = "TypeBuilder";
	}
}
