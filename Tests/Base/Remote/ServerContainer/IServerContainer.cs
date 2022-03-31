namespace Tests.Remote.ServerContainer
{
	public interface IServerContainer
	{
		bool KeepSamePortBetweenThreads { get; set; }
	}
}
