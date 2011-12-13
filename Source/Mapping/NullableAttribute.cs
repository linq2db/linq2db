using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(
		AttributeTargets.Property | AttributeTargets.Field |
		AttributeTargets.Class | AttributeTargets.Interface)]
	public class NullableAttribute : Attribute
	{
		public NullableAttribute()
		{
			IsNullable = true;
		}

		public NullableAttribute(bool isNullable)
		{
			IsNullable = isNullable;
		}

		public NullableAttribute(Type type)
		{
			Type       = type;
			IsNullable = true;
		}

		public NullableAttribute(Type type, bool isNullable)
		{
			Type       = type;
			IsNullable = isNullable;
		}

		public bool IsNullable { get; set; }
		public Type Type       { get; private set; }
	}
}
