using System;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// Exception class for NHibernate to LINQ To DB integration issues.
	/// </summary>
	public class LinqToDBForNHibernateToolsException : Exception
	{
		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		public LinqToDBForNHibernateToolsException()
		{
		}

		/// <summary>
		/// Creates new instance of exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		public LinqToDBForNHibernateToolsException(string message) : base(message)
		{
		}

		/// <summary>
		/// Creates new instance of exception when it generated for other exception.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="innerException">Original exception.</param>
		public LinqToDBForNHibernateToolsException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
