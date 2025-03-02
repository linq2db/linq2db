using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Queryable table-like object descriptor.
	/// </summary>
	/// <param name="Name">Name of object.</param>
	/// <param name="Description">Optional description, associated with current object.</param>
	/// <param name="Columns">Ordered (by ordinal) list of columns.</param>
	/// <param name="Identity">Optional identity column descriptor.</param>
	/// <param name="PrimaryKey">Optional primary key descriptor.</param>
	public abstract record TableLikeObject(
		SqlObjectName               Name,
		string?                     Description,
		IReadOnlyCollection<Column> Columns,
		Identity?                   Identity,
		PrimaryKey?                 PrimaryKey)
	{
		public override string ToString() => Name.ToString();
	}
}
