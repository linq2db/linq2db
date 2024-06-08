using System;

using JetBrains.Annotations;

namespace LinqToDB.Tools
{
	/// <summary>
	/// Represents a user-defined operation with context to be used for Activity Service events.
	/// </summary>
	[PublicAPI]
	public interface IActivity : IDisposable, IAsyncDisposableEx
	{
	}
}
