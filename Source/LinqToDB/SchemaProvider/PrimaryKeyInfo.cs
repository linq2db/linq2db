﻿using System.Diagnostics;

namespace LinqToDB.SchemaProvider
{
	[DebuggerDisplay("TableID = {TableID}, PrimaryKeyName = {PrimaryKeyName}, ColumnName = {ColumnName}, Ordinal = {Ordinal}")]
	public class PrimaryKeyInfo
	{
		public string  TableID    = null!;
		public string? PrimaryKeyName;
		public string  ColumnName = null!;
		public int     Ordinal;
	}
}
