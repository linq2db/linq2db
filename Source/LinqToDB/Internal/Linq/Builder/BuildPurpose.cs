using System;

namespace LinqToDB.Internal.Linq.Builder
{
	[Flags]
	enum BuildPurpose
	{
		None,
		Sql,
		Table,
		/// <summary>
		/// Materialization mapper expression build.
		/// </summary>
		Expression,
		Expand,
		Root,
		AssociationRoot,
		AggregationRoot,
		SubQuery,
		Extract,
		Traverse
	}
}
