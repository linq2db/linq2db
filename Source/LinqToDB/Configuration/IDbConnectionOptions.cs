using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

namespace LinqToDB.Configuration
{
	public interface IDbConnectionOptions
	{
		/// <summary>
		/// Gets <see cref="MappingSchema"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		MappingSchema? MappingSchema { get; }

		/// <summary>
		/// Gets <see cref="IDataProvider"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		IDataProvider? DataProvider { get; }

		/// <summary>
		/// Gets <see cref="System.Data.Common.DbConnection"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		DbConnection? DbConnection { get; }

		/// <summary>
		/// Gets <see cref="DbConnection"/> ownership status for <see cref="DataConnection"/> instance.
		/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
		/// </summary>
		bool DisposeConnection { get; }

		/// <summary>
		/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		string? ConfigurationString { get; }

		/// <summary>
		/// Gets provider name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		string? ProviderName { get; }

		/// <summary>
		/// Gets connection string to use with <see cref="DataConnection"/> instance.
		/// </summary>
		string? ConnectionString { get; }

		/// <summary>
		/// Gets connection factory to use with <see cref="DataConnection"/> instance.
		/// </summary>
		Func<DbConnection>? ConnectionFactory { get; }

		/// <summary>
		/// Gets <see cref="DbTransaction"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		DbTransaction? DbTransaction { get; }

		/// <summary>
		/// Gets custom trace method to use with <see cref="DataConnection"/> instance.
		/// </summary>
		Action<TraceInfo>? OnTrace { get; }

		/// <summary>
		/// Gets custom trace level to use with <see cref="DataConnection"/> instance.
		/// </summary>
		TraceLevel? TraceLevel { get; }

		/// <summary>
		/// Gets custom trace writer to use with <see cref="DataConnection"/> instance.
		/// </summary>
		Action<string?, string?, TraceLevel>? WriteTrace { get; }

		/// <summary>
		/// Gets list of interceptors to use with <see cref="DataConnection"/> instance.
		/// </summary>
		IReadOnlyList<IInterceptor>? Interceptors { get; }
	}
}
