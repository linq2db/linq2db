using System;
using System.Data.Common;

using LinqToDB.Common;
using LinqToDB.Interceptors;

namespace Tests
{
	/// <summary>
	/// Provides access to last command and parameters. Skips execution of the command and returns 1 as result for ExecuteNonQuery.
	/// </summary>
	public sealed class SaveAndSkipCommandInterceptor : SaveCommandInterceptor
	{
		public override Option<int> ExecuteNonQuery(
			CommandEventData eventData,
			DbCommand        command,
			Option<int>      result)
		{
			return Option<int>.Some(1);
		}
	}
}
