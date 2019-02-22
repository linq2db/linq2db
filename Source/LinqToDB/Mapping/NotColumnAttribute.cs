using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Marks current property or column to be ignored for mapping when explicit column mapping disabled.
	/// See <see cref="TableAttribute.IsColumnAttributeRequired"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class NotColumnAttribute : ColumnAttribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		public NotColumnAttribute()
		{
			IsColumn = false;
		}
	}
}
