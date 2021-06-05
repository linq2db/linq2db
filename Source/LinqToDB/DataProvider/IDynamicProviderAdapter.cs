﻿using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.DataProvider
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
		/// </summary>
		Type TransactionType { get; }
	}
}
