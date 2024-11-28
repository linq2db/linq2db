using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// You can apply it to inheritance root class to configure inheritance behavior.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
	public class InheritanceAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates new inheritance configuration attribute.
		/// </summary>
		public InheritanceAttribute()
		{
		}

		/// <summary>
		/// This option is applicable only when inhertance mapping doesn't have default mapping
		/// (specified by <see cref="InheritanceMappingAttribute"/> with <see cref="InheritanceMappingAttribute.IsDefault"/> set to <c>true</c>).
		/// <c>true</c> value should be used only when target table could contain unmapped records to tell Linq To DB that it should generate discriminator field
		/// filter to ignore unknown records.
		/// Default value: <c>false</c>.
		/// </summary>
		public bool IgnoreUnmappedRecords { get; set; }

		public override string GetObjectID()
		{
			return FormattableString.Invariant($".{Configuration}.{(IgnoreUnmappedRecords ? '1':'0')}.");
		}
	}
}
