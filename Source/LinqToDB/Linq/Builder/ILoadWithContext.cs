using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	interface ILoadWithContext : IBuildContext
	{
		public LoadWithInfo  LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
