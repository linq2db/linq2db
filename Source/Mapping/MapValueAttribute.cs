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

		public MapValueAttribute(object value, bool isDefault)
		{
			Value     = value;
			IsDefault = isDefault;
		}

		public object Value     { get; set; }
		public bool   IsDefault { get; set; }
	}
}
