using System;
using System.Data.Common;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Contains base information about ADO.NET provider.
	/// Could be extended by specific implementation to expose additional provider-specific services.
	/// </summary>
	public interface IDynamicProviderAdapter
	{
		/// <summary>
		/// Gets type, that implements <see cref="DbConnection"/> for current ADO.NET provider.
		/// </summary>
		Type ConnectionType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="DbDataReader"/> for current ADO.NET provider.
		/// </summary>
		Type DataReaderType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="DbParameter"/> for current ADO.NET provider.
		/// </summary>
		Type ParameterType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="DbCommand"/> for current ADO.NET provider.
		/// </summary>
		Type CommandType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="DbTransaction"/> for current ADO.NET provider.
		/// For providers/databases without transaction support contains <see langword="null"/>.
		/// </summary>
		Type? TransactionType { get; }

		/// <summary>
		/// Creates instance of database provider connection class using provided connection string.
		/// </summary>
		/// <param name="connectionString">Connection string to use with created connection.</param>
		/// <returns>Connection instance.</returns>
		DbConnection CreateConnection(string connectionString);
	}
}
