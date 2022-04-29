﻿using System.Collections.Generic;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Table descriptor.
	/// </summary>
	/// <param name="Name">Name of table.</param>
	/// <param name="Description">Optional description, associated with table.</param>
	/// <param name="Columns">Ordered (by ordinal) list of table columns.</param>
	/// <param name="Identity">Optional identity column descriptor.</param>
	/// <param name="PrimaryKey">Optional primary key descriptor.</param>
	public sealed record Table(
		ObjectName                  Name,
		string?                     Description,
		IReadOnlyCollection<Column> Columns,
		Identity?                   Identity,
		PrimaryKey?                 PrimaryKey)
		: TableLikeObject(Name, Description, Columns, Identity, PrimaryKey);
}
