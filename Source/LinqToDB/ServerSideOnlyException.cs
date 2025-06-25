using System;
using System.ComponentModel;

namespace LinqToDB
{
	/// <summary>
	/// Exception to throw for server-side only APIs, called on client.
	/// </summary>
	[Serializable]
	public class ServerSideOnlyException : Exception
	{
		// don't remove, we just want to guard users from using it explicitly
		[Obsolete("Use constructor with parameter"), EditorBrowsable(EditorBrowsableState.Never)]
		public ServerSideOnlyException()
			: base("Cannot call server-side API on client.")
		{
		}

		/// <summary>
		/// Exception to throw for server-side only APIs, called on client.
		/// </summary>
		/// <param name="apiName">Name of server-side property or method.</param>
		public ServerSideOnlyException(string apiName)
			: base($"'{apiName}' is server-side API.")
		{
		}
	}
}
