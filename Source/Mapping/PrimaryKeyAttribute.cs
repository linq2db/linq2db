using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public sealed class PrimaryKeyAttribute : Attribute
	{
		public PrimaryKeyAttribute()
		{
			Order = -1;
		}

		public PrimaryKeyAttribute(int order)
		{
			Order = order;
		}

		public PrimaryKeyAttribute(string configuration, int order)
		{
			Configuration = configuration;
			Order         = order;
		}

		public string Configuration { get; set; }
		public int    Order         { get; set; }
	}
}
