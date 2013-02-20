using System;

namespace LinqToDB.Mapping
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class IdentityAttribute : Attribute
	{
		public IdentityAttribute()
		{
		}

		public IdentityAttribute(string configuration)
		{
			Configuration = configuration;
		}

		public string Configuration { get; set; }
	}
}
