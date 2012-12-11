using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
	public class ScalarTypeAttribute : Attribute
	{
		public ScalarTypeAttribute()
		{
			IsScalar = true;
		}

		public ScalarTypeAttribute(bool isScalar)
		{
			IsScalar      = isScalar;
		}

		public ScalarTypeAttribute(string configuration)
		{
			Configuration = configuration;
			IsScalar      = true;
		}

		public ScalarTypeAttribute(string configuration, bool isScalar)
		{
			Configuration = configuration;
			IsScalar      = isScalar;
		}

		public string Configuration { get; set; }
		public bool   IsScalar      { get; set; }
	}
}
