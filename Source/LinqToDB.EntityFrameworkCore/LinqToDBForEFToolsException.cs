using System;
using System.ComponentModel;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Exception class for EF.Core to LINQ To DB integration issues.
	/// </summary>
	public sealed class LinqToDBForEFToolsException : Exception
	{
		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		// don't remove, we just want to guard users from using it explicitly
		[Obsolete("Use one of constructors with message parameter"), EditorBrowsable(EditorBrowsableState.Never)]
		public LinqToDBForEFToolsException()
		{
		}

		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		public LinqToDBForEFToolsException(string message) : base(message)
		{
		}

		/// <summary>
		/// Creates new instance of exception when it generated for other exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="innerException">Original exception.</param>
		public LinqToDBForEFToolsException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
