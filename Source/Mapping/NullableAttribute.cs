using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=true)]
	public class NullableAttribute : Attribute
	{
		public NullableAttribute()
		{
			CanBeNull = true;
		}

		public NullableAttribute(bool isNullable)
		{
			CanBeNull = isNullable;
		}

		public NullableAttribute(string configuration, bool isNullable)
		{
			Configuration = configuration;
			CanBeNull     = isNullable;
		}

		public string Configuration { get; set; }
		public bool   CanBeNull     { get; set; }
	}
}
