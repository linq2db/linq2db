using System.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	interface ILoadWithContext : IBuildContext
	{
		public LoadWithInfo  LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
