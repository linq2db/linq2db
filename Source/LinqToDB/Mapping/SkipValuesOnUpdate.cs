using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on update.
	/// </summary>
	public class SkipValuesOnUpdateAttribute : SkipBaseAttribute
	{
		public SkipValuesOnUpdateAttribute()
		{
			Affects = SkipModification.Update;
		}

		/// <summary>  
		/// Constructor. 
		/// </summary>
		/// <param name="values"> 
		/// Values to skip on update operations.
		/// </param>
		public SkipValuesOnUpdateAttribute(params object[] values) : this()
		{
			var valuesToSkip = values;
			if (valuesToSkip == null)
			{
				Values = new HashSet<object>() { null };
			}
			else if (valuesToSkip.Length > 0)
			{
				Values = new HashSet<object>(values);
			}
		}

		/// <summary>  
		/// Check if object contains values that should be skipped.
		/// </summary>
		/// <param name="obj">        The object. </param>
		/// <param name="entityDescriptor"> The entity descriptor. </param>
		/// <param name="columnDescriptor"> The column descriptor. </param>
		/// <returns>  True if it succeeds, false if it fails. </returns>
		public override bool ShouldSkip(object obj, EntityDescriptor entityDescriptor, ColumnDescriptor columnDescriptor)
		{
			// todo: replace MemberAccessor.Getter() with GetMemberValue
			return Values?.Contains(columnDescriptor.MemberAccessor.Getter(obj)) ?? false;
		}

		public override SkipModification Affects { get; }

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public override string Configuration { get; set; }

		/// <summary>
		/// Gets collection with values to skip on update.
		/// </summary>
		public HashSet<object> Values { get; }

	}
}
