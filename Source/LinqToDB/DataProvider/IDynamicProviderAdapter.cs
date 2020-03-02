using System;
using System.Data;

namespace LinqToDB.DataProvider
{
	/// <summary>
	/// Contains base information about ADO.NET provider.
	/// Could be extended by specific implementation to expose additional provider-specific services.
	/// </summary>
	public interface IDynamicProviderAdapter
	{
		/// <summary>
		/// Gets type, that implements <see cref="IDbConnection"/> for current ADO.NET provider.
		/// </summary>
		Type ConnectionType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="IDataReader"/> for current ADO.NET provider.
		/// </summary>
		Type DataReaderType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="IDbDataParameter"/> for current ADO.NET provider.
		/// </summary>
		Type ParameterType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="IDbCommand"/> for current ADO.NET provider.
		/// </summary>
		Type CommandType { get; }

		/// <summary>
		/// Gets type, that implements <see cref="IDbTransaction"/> for current ADO.NET provider.
		/// </summary>
		Type TransactionType { get; }
	}
}
