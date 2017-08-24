using System;
using System.Runtime.Serialization;

namespace LinqToDB.Common
{
	/// <summary>
	/// Defines the base class for the namespace exceptions.
	/// </summary>
	/// <remarks>
	/// This class is the base class for exceptions that may occur during
	/// execution of the namespace members.
	/// </remarks>
	[Serializable]
	public class LinqToDBConvertException : LinqToDBException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBConvertException"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes the <see cref="Exception.Message"/>
		/// property of the new instance such as "A Build Type exception has occurred."
		/// </remarks>
		public LinqToDBConvertException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBConvertException"/> class
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <seealso cref="Exception.Message"/>
		public LinqToDBConvertException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBConvertException"/> class
		/// with the specified error message and InnerException property.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.Message"/>
		/// <seealso cref="Exception.InnerException"/>
		public LinqToDBConvertException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBConvertException"/> class
		/// with the specified InnerException property.
		/// </summary>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.InnerException"/>
		public LinqToDBConvertException(Exception innerException) 
			: base(innerException.Message, innerException)
		{
		}

#if !SILVERLIGHT && !NETFX_CORE && !NETSTANDARD && !NETSTANDARD2_0

		/// <summary>
		/// Initializes a new instance of the <see cref="LinqToDBConvertException"/> class
		/// with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or
		/// destination.</param>
		/// <remarks>This constructor is called during deserialization to
		/// reconstitute the exception object transmitted over a stream.</remarks>
		protected LinqToDBConvertException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
	}
}
