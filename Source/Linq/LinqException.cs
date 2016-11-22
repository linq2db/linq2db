using System;
using System.Runtime.Serialization;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Defines the base class for the namespace exceptions.
	/// </summary>
	/// <remarks>
	/// This class is the base class for exceptions that may occur during
	/// execution of the namespace members.
	/// </remarks>
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
		/// <param name="args">An System.Object array containing zero or more objects to format.</param>
		/// <seealso cref="Exception.Message"/>
		[JetBrains.Annotations.StringFormatMethod("args")]
		public LinqException(string message, params object[] args)
			: base(string.Format(message, args))
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

#if !SILVERLIGHT && !NETFX_CORE && !NETSTANDARD

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqException"/> class
		/// with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination.</param>
		/// <remarks>
		/// This constructor is called during deserialization to
		/// reconstitute the exception object transmitted over a stream.
		/// </remarks>
		protected LinqException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
	}
}

