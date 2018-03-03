﻿using System;

namespace LinqToDB
{
	using Linq;

	/// <summary>
	/// Table-like queryable source, e.g. table, view or table-valued function.
	/// </summary>
	/// <typeparam name="T">Record mapping type.</typeparam>
	public interface ITable<out T> : IExpressionQuery<T>
	{
		string DatabaseName { get; }
		string SchemaName   { get; }
		string TableName    { get; }

		string GetTableName();
	}
}
