using System;
using System.ComponentModel;
using System.Globalization;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Defines the base class for the namespace exceptions.
	/// </summary>
	/// <remarks>
	/// This class is the base class for exceptions that may occur during
	/// execution of the namespace members.
	/// </remarks>
	// TODO: Remove in v7
	[Obsolete($"This exception type is not used anymore. Please update your code to handle {nameof(LinqToDBException)}."), EditorBrowsable(EditorBrowsableState.Never)]
	[Serializable]
	public class LinqException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes the <see cref="Exception.Message"/>
		/// property of the new instance
		/// to a system-supplied message that describes the error,
		/// such as "LinqToDB Linq error has occurred."
		/// </remarks>
		public LinqException()
			: base("A LinqToDB Linq error has occurred.")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the exception is thrown.</param>
		/// <seealso cref="Exception.Message"/>
		public LinqException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the exception is thrown.</param>
		/// <param name="args">An <see cref="System.Object"/> array containing zero or more objects to format.</param>
		/// <seealso cref="Exception.Message"/>
		[JetBrains.Annotations.StringFormatMethod("message")]
		public LinqException(string message, params object?[] args)
			: base(string.Format(CultureInfo.InvariantCulture, message, args))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class
		/// with the specified error message and InnerException property.
		/// </summary>
		/// <param name="message">The message to display to the client when the exception is thrown.</param>
		/// <param name="innerException">The InnerException, if any, that threw the current exception.</param>
		/// <seealso cref="Exception.Message"/>
		/// <seealso cref="Exception.InnerException"/>
		public LinqException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class
		/// with the InnerException property.
		/// </summary>
		/// <param name="innerException">The InnerException, if any, that threw the current exception.</param>
		/// <seealso cref="Exception.InnerException"/>
		public LinqException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}
	}
}
