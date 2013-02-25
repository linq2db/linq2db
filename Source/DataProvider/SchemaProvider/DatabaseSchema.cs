using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class DatabaseSchema
	{
		public string            DataSource    { get; set; }
		public string            Database      { get; set; }
		public string            ServerVersion { get; set; }
		public List<TableSchema> Tables        { get; set; }
	}
}
