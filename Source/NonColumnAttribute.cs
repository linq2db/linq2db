using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class NonColumnAttribute : ColumnAttribute
	{
		public NonColumnAttribute()
		{
			IsColumn = false;
		}
	}
}
