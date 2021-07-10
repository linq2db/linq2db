using System;

namespace Tests
{
	[AttributeUsage(AttributeTargets.All)]
	public class DataTypeAttribute : Attribute
	{
		public DataTypeAttribute(LinqToDB.DataType type)
		{
		}

		public LinqToDB.DataType DataType { get; set; }
	}
}
