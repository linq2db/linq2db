namespace LinqToDB.Linq.Builder
{
	interface ILoadWithContext : IBuildContext
	{
		public LoadWithEntity? LoadWithRoot { get; set; }
	}
}
