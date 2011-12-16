using System;
using System.Runtime.Serialization;

namespace LinqToDB.Data
{
	using SqlProvider;

	/// <summary>
	/// Defines the base class for the namespace exceptions.
	/// </summary>
	/// <remarks>
	/// This class is the base class for exceptions that may occur during
	/// execution of the namespace members.
	/// </remarks>
	[Serializable] 
	public class DataException : System.Data.DataException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataException"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor initializes the <see cref="Exception.Message"/>
		/// property of the new instance
		/// to a system-supplied message that describes the error,
		/// such as "LinqToDB Data error has occurred."
		/// </remarks>
		public DataException()
			: base("A LinqToDB Data error has occurred.")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataException"/> class
		/// with the specified error message.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <seealso cref="Exception.Message"/>
		public DataException(string message)
			: base(message) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataException"/> class
		/// with the specified error message and InnerException property.
		/// </summary>
		/// <param name="message">The message to display to the client when the
		/// exception is thrown.</param>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.Message"/>
		/// <seealso cref="Exception.InnerException"/>
		public DataException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataException"/> class
		/// with the InnerException property.
		/// </summary>
		/// <param name="innerException">The InnerException, if any, that threw
		/// the current exception.</param>
		/// <seealso cref="Exception.InnerException"/>
		public DataException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataException"/> class
		/// with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or
		/// destination.</param>
		/// <remarks>This constructor is called during deserialization to
		/// reconstitute the exception object transmitted over a stream.</remarks>
		protected DataException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		#region Internal

		private readonly DbManager _dbManager;

		static string GetMessage(DbManager dbManager, Exception innerException)
		{
			var obj = dbManager.DataProvider.Convert(
				innerException, ConvertType.ExceptionToErrorMessage);

			return obj is Exception ? ((Exception)obj).Message : obj.ToString();
		}

		internal DataException(DbManager dbManager, Exception innerException)
			: this(GetMessage(dbManager, innerException), innerException)
		{
			_dbManager = dbManager;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets a number that identifies the type of error.
		/// </summary>
		public int? Number
		{
			get
			{
				return (int?)(_dbManager == null? null:
					_dbManager.DataProvider.Convert(
						InnerException, ConvertType.ExceptionToErrorNumber));
			}
		}

		#endregion
	}
}

