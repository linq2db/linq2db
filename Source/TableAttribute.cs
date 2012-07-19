using System;

namespace LinqToDB
{
	[AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class TableAttribute : Attribute
	{
		public TableAttribute()
		{
			IsColumnAttributeRequired = true;
		}

		public string Config                    { get; set; }
		public string Name                      { get; set; }
		public string Schema                    { get; set; }
		public string Database                  { get; set; }
		public bool   IsColumnAttributeRequired { get; set; }
	}}
