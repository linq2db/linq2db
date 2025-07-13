namespace LinqToDB.Internal.Linq.Builder
{
	interface ILoadWithContext : IBuildContext
	{
		public LoadWithEntity? LoadWithRoot { get; set; }
	}
}
