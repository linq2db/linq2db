using System;

namespace Tests.Remote.ServerContainer
{
	public class PortStatusRestorer : IDisposable
	{
		private readonly bool             _keepSamePortBetweenThreads;
		private readonly IServerContainer _serverContainer;

		public PortStatusRestorer(IServerContainer serverContainer, bool keepSamePortBetweenThreads)
		{
			_serverContainer                            = serverContainer;
			_keepSamePortBetweenThreads                 = _serverContainer.KeepSamePortBetweenThreads;
			_serverContainer.KeepSamePortBetweenThreads = keepSamePortBetweenThreads;
		}

		void IDisposable.Dispose()
		{
			_serverContainer.KeepSamePortBetweenThreads = _keepSamePortBetweenThreads;
		}
	}
}
