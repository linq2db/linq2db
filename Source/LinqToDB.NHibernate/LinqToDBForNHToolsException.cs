using System;

namespace LinqToDB.NHibernateExtension
{
	/// <summary>
	/// Exception class for NHibernate to LINQ To DB integration issues.
	/// </summary>
	public class LinqToDBForNHToolsException : Exception
	{
		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		public LinqToDBForNHToolsException()
		{
		}

		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		public LinqToDBForNHToolsException(string message) : base(message)
		{
		}

		/// <summary>
		/// Creates new instance of exception when it generated for other exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="innerException">Original exception.</param>
		public LinqToDBForNHToolsException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
