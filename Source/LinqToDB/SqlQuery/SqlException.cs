using System;
using System.ComponentModel;
using System.Globalization;

namespace LinqToDB.SqlQuery
{
	// TODO: Remove in v7
	[Obsolete($"This exception type is not used anymore. Please update your code to handle {nameof(LinqToDBException)}."), EditorBrowsable(EditorBrowsableState.Never)]
	[Serializable]
	public sealed class SqlException : Exception
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
	}
}
