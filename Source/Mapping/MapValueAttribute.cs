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

		public MapValueAttribute(string configuration, object value)
		{
			Configuration = configuration;
			Value         = value;
		}

		public MapValueAttribute(object value, bool isDefault)
		{
			Value     = value;
			IsDefault = isDefault;
		}

		public MapValueAttribute(string configuration, object value, bool isDefault)
		{
			Configuration = configuration;
			Value         = value;
			IsDefault     = isDefault;
		}

		public string Configuration { get; set; }
		public object Value         { get; set; }
		public bool   IsDefault     { get; set; }
	}
}
