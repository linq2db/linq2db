namespace Tests.Model.Remote
{
	public static class RemoteHost
	{
		// The address every remote test host binds to and every remote test client connects to.
		// "localhost" can resolve to ::1 first, and the IPv6 loopback is slow or unrouted on some
		// hosts (WSL with Docker in particular); the free-port probe also checks IPv4 only.
		public const string Loopback = "127.0.0.1";
	}
}
