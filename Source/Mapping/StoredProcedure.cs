using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class StoredProcedure : Attribute
	{
		public StoredProcedure()
		{
		}

		public StoredProcedure(string name)
		{
			Name = name;
		}

		public string Name     { get; set; }
		public string Schema   { get; set; }
		public string Database { get; set; }
	}
}
