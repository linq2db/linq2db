using System;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Defines loadable database objects.
	/// Used to specify types of database objects that should be loaded from schema.
	/// Doesn't include dependent objects like parameters or columns.
	/// </summary>
	[Flags]
	public enum SchemaObjects
	{
		/// <summary>
		/// Nothing selected.
		/// </summary>
		None              = 0,
		/// <summary>
		/// Table.
		/// </summary>
		Table             = 1,
		/// <summary>
		/// View.
		/// </summary>
		View              = 2,
		/// <summary>
		/// Stored procedure.
		/// </summary>
		StoredProcedure   = 4,
		/// <summary>
		/// Table function.
		/// </summary>
		TableFunction     = 8,
		/// <summary>
		/// Scalar function.
		/// </summary>
		ScalarFunction    = 16,
		/// <summary>
		/// Aggregate function.
		/// </summary>
		AggregateFunction = 32,
		/// <summary>
		/// Foreign key.
		/// </summary>
		ForeignKey        = 64
	}
}
