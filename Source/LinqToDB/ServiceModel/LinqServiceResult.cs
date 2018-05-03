using System;
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	public class LinqServiceResult
	{
		public int            FieldCount   { get; set; }
		public int            RowCount     { get; set; }
		public Guid           QueryID      { get; set; }
		public string[]       FieldNames   { get; set; }
		public Type[]         FieldTypes   { get; set; }
		public Type[]         VaryingTypes { get; set; }
		public List<string[]> Data         { get; set; }
	}
}
