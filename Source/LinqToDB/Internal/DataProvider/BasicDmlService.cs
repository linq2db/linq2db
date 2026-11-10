using System;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Default <see cref="IDmlService"/> registered for every provider that does not supply its own: standard
	/// command-scenario construction (inherited from <see cref="DmlServiceBase"/>) and no provider-specific
	/// "table not found" detection.
	/// </summary>
	public sealed class BasicDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;
	}
}
