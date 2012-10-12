using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple=true)]
	public class MapValueAttribute : Attribute
	{
		public MapValueAttribute()
		{
		}

		public MapValueAttribute(object value)
		{
			Value = value;
		}

		public object Value { get; set; }
	}
}
