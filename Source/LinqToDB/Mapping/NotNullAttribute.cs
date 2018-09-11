using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Sets nullability flag for current column to <c>false</c>.
	/// See <see cref="NullableAttribute"/> for more details.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class NotNullAttribute : NullableAttribute
	{
		/// <summary>
		/// Creates attribute isntance.
		/// </summary>
		public NotNullAttribute()
			: base(false)
		{
		}

		/// <summary>
		/// Creates attribute isntance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		public NotNullAttribute(string configuration)
			: base(configuration, false)
		{
		}
	}
}
