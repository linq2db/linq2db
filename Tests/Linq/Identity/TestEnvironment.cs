using System.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Tests.Identity
{
	public static class TestEnvironment
	{
		public static string ConnectionString => "Server=(localdb)\\MSSqlLocaldb;Integrated Security=true;MultipleActiveResultSets=true;Connect Timeout=30";
	}
}
