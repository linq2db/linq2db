using System;

namespace LinqToDB.Mapping
{
	public class Map
	{
		private static MappingSchemaOld _defaultSchema = new DefaultMappingSchema();
		public  static MappingSchemaOld  DefaultSchema
		{
			[System.Diagnostics.DebuggerStepThrough]
			get { return _defaultSchema;  }
			set { _defaultSchema = value; }
		}
	}
}
