using System;
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	public class LinqServiceResult
	{
		public int            FieldCount   { get; set; }
		public int            RowCount     { get; set; }
		public Guid           QueryID      { get; set; }
		public string[]       FieldNames   { get; set; } = null!;
		public Type[]         FieldTypes   { get; set; } = null!;
		public List<string[]> Data         { get; set; } = null!;
	}
}
