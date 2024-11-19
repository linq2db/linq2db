using System;

namespace LinqToDB.Tools.Activity
{
	interface IStatActivity
	{
		string   Name      { get; }
		TimeSpan Elapsed   { get; }
		long     CallCount { get; }
	}
}
