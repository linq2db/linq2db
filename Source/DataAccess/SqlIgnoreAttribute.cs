using System;

namespace LinqToDB.DataAccess
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SqlIgnoreAttribute : Attribute
	{
		public SqlIgnoreAttribute()
		{
			Ignore = true;
		}

		public SqlIgnoreAttribute(bool ignore)
		{
			Ignore = ignore;
		}

		public bool Ignore { get; set; }
	}
}
