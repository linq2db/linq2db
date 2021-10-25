using System;

namespace LinqToDB.DataProvider.SqlServer
{
	public enum JoinHint
	{
		Loop,
		Hash,
		Merge,
		Remote
	}
}
