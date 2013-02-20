using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class NotColumnAttribute : ColumnAttribute
	{
		public NotColumnAttribute()
		{
			IsColumn = false;
		}
	}
}
