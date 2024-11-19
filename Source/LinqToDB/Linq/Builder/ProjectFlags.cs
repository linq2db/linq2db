using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		None            = 0x00,

		SQL             = 0x01,
		Expression      = 0x02,
		Root            = 0x04,
		/// <summary>
		/// Forces expanding associations and GroupJoin into query expression
		/// </summary>
		ExtractProjection = 0x08,

		AggregationRoot = 0x10,
		/// <summary>
		/// Specify that from whole context we need just key fields.
		/// </summary>
		Keys            = 0x20,
		/// <summary>
		/// Validates that expression can be converted to the SQL. Returned value cannot be used.
		/// </summary>
		Test            = 0x40,
		AssociationRoot = 0x80,
		/// <summary>
		/// Specify that we are looking for a table
		/// </summary>
		Table = 0x100,
		/// <summary>
		/// Specify that we associations should not filter out recordset
		/// </summary>
		ForceOuterAssociation = 0x200,
		/// <summary>
		/// Specify that we expect real expression under hidden by Selects chain
		/// </summary>
		Traverse = 0x800,

		Subquery = 0x1000,

		/// <summary>
		/// Indicates that generated SQL is for extension method.
		/// </summary>
		ForExtension = 0x2000,
	}
}
