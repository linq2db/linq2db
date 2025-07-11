namespace LinqToDB.Remote.SignalR
{
	public class LinqToDBHub<T> : LinqToDBHub
		where T : IDataContext
	{
		readonly ILinqService<T> _linqService;

		public LinqToDBHub(ILinqService<T> linqService)
		{
			_linqService                   = linqService;
			_linqService.RemoteClientTag ??= "Signal/R";
		}

		protected override ILinqService LinqService => _linqService;
	}
}
