using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		None            = 0x00,

		SQL        = 1 << 0,
		Expression = 1 << 1,
		Root       = 1 << 2,
		/// <summary>
		/// Forces expanding associations and GroupJoin into query expression
		/// </summary>
		ExtractProjection = 1 << 3,

		AggregationRoot = 1 << 4,
		/// <summary>
		/// Specify that from whole context we need just key fields.
		/// </summary>
		Keys            = 1 << 5,
		AssociationRoot = 1 << 7,
		/// <summary>
		/// Specify that we are looking for a table
		/// </summary>
		Table = 1 << 8,
		/// <summary>
		/// Specify that we associations should not filter out recordset
		/// </summary>
		ForceOuterAssociation = 1 << 9,
		/// <summary>
		/// Specify that we expect real expression under hidden by Selects chain
		/// </summary>
		Traverse = 1 << 10,

		Subquery = 1 << 11,

		/// <summary>
		/// Indicates that generated SQL is for extension method.
		/// </summary>
		ForExtension = 1 << 12,

		Expand = 1 << 13,

		MemberRoot = 1 << 14
	}
}
