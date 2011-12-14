using System;

namespace LinqToDB.Mapping
{
	public class Map
	{
		private static MappingSchema _defaultSchema = new DefaultMappingSchema();
		public  static MappingSchema  DefaultSchema
		{
			[System.Diagnostics.DebuggerStepThrough]
			get { return _defaultSchema;  }
			set { _defaultSchema = value; }
		}
	}
}
