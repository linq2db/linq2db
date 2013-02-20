using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class NotNullAttribute : NullableAttribute
	{
		public NotNullAttribute()
			: base(false)
		{
		}

		public NotNullAttribute(string configuration)
			: base(configuration, false)
		{
		}
	}
}
