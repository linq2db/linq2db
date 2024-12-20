using System;

namespace LinqToDB
{
	/// <summary>
	/// Exception type for exceptions, thrown by Linq To DB.
	/// </summary>
	[Serializable]
	public class LinqToDBException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBException"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes the <see cref="Exception.Message"/>
		/// property with generic error message "A Linq To DB exception has occurred.".
		/// </remarks>
		[Obsolete("Use one of constructors with message")]
		public LinqToDBException()
			: base("A Linq To DB exception has occurred.")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBException"/> class
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <seealso cref="Exception.Message"/>
		public LinqToDBException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBException"/> class
		/// with the specified error message and InnerException property.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.Message"/>
		/// <seealso cref="Exception.InnerException"/>
		public LinqToDBException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBException"/> class
		/// with the specified InnerException property.
		/// </summary>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.InnerException"/>
		[Obsolete("Use one of constructors with message")]
		public LinqToDBException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}
	}
}
