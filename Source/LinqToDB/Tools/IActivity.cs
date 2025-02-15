using System;
using System.Data.Common;

using JetBrains.Annotations;

using LinqToDB.Data;

namespace LinqToDB.Tools
{
	/// <summary>
	/// Represents a user-defined operation with context to be used for Activity Service events.
	/// </summary>
	[PublicAPI]
	public interface IActivity : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Add or update the Activity tag with the input key and value.
		/// </summary>
		/// <param name="key">The tag key name</param>
		/// <param name="value">The tag value mapped to the input key</param>
		/// <returns><see langword="this" /> for convenient chaining.</returns>
		IActivity AddTag(ActivityTagID key, object? value);

		/// <summary>
		/// Add connection and command tags to query activity.
		/// </summary>
		/// <param name="context">Linq To DB data context, associated with current activity.</param>
		/// <param name="connection">ADO.NET database connection, associated with current activity.</param>
		/// <param name="command">ADO.NET command, associated with current activity.</param>
		/// <returns></returns>
		IActivity AddQueryInfo(DataConnection? context, DbConnection? connection, DbCommand? command);
	}
}
