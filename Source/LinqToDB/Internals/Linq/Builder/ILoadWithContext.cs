using System.Reflection;

namespace LinqToDB.Internals.Linq.Builder
{
	interface ILoadWithContext : IBuildContext
	{
		public LoadWithInfo LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
