using System.Collections.Generic;
using System.Linq;

using LinqToDB.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Describes foreign key relation between tables.
	/// </summary>
	/// <param name="Name">Name of foreign key.</param>
	/// <param name="Source">Table, that references other table.</param>
	/// <param name="Target">Table, referenced by foreign key.</param>
	/// <param name="Relation">Ordered list of source-target pairs of columns, used by foreign key relation.</param>
	public sealed record ForeignKey(
		string                                 Name,
		SqlObjectName                          Source,
		SqlObjectName                          Target,
		IReadOnlyList<ForeignKeyColumnMapping> Relation)
	{
		public override string ToString() => $"{Name}: {Source}({string.Join(", ", Relation.Select(_ => _.SourceColumn))}) => {Target}({string.Join(", ", Relation.Select(_ => _.TargetColumn))})";
	}
}
