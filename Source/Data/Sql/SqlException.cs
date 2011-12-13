using System;
using System.Runtime.Serialization;

namespace LinqToDB.Data.Sql
{
	public class SqlException : Exception
	{
		public SqlException()
			: base("A LinqToDB Sql error has occurred.")
		{
		}

		public SqlException(string message)
			: base(message) 
		{
		}

		[JetBrains.Annotations.StringFormatMethod("message")]
		public SqlException(string message, params object[] args)
			: base(string.Format(message, args))
		{
		}

		public SqlException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public SqlException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

#if !SILVERLIGHT

		protected SqlException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
	}
}

