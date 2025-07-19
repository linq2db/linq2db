using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace LinqToDB.SqlQuery
{
	[Serializable]
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
		public SqlException(string message, params object?[] args)
			: base(string.Format(CultureInfo.InvariantCulture, message, args))
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

		protected SqlException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
			: base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
		{
		}
	}
}

