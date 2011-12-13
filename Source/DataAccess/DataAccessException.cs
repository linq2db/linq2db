using System;
using System.Runtime.Serialization;

namespace LinqToDB.DataAccess
{
	/// <summary>
	/// Defines the base class for the namespace exceptions.
	/// </summary>
	/// <remarks>
	/// This class is the base class for exceptions that may occur during
	/// execution of the namespace members.
	/// </remarks>
	[Serializable]
	public class DataAccessException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataAccessException"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes the <see cref="Exception.Message"/>
		/// property of the new instance
		/// to a system-supplied message that describes the error,
		/// such as "LinqToDB Data Access error has occurred."
		/// </remarks>
		public DataAccessException()
			: base("A Data Access exception has occurred.")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataAccessException"/> class 
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <seealso cref="Exception.Message"/>
		public DataAccessException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataAccessException"/> class 
		/// with the specified error message and InnerException property.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.Message"/>
		/// <seealso cref="Exception.InnerException"/>
		public DataAccessException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataAccessException"/> class 
		/// with the InnerException property.
		/// </summary>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.InnerException"/>
		public DataAccessException(Exception innerException) 
			: base(innerException.Message, innerException) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataAccessException"/> class
		/// with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or
		/// destination.</param>
		/// <remarks>This constructor is called during deserialization to
		/// reconstitute the exception object transmitted over a stream.</remarks>
		protected DataAccessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
