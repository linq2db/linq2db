using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Abstract Attribute to be used for skipping value for
	/// <see cref="SkipValuesOnInsertAttribute"/> based on <see cref="SkipModification.Insert"></see> or 
	/// <see cref="SkipValuesOnUpdateAttribute"/> based on <see cref="SkipModification.Update"/>/> or a
	/// custom Attribute derived from this to override <see cref="SkipBaseAttribute.ShouldSkip"/>
	/// </summary>
	public abstract class SkipValuesByListAttribute: SkipBaseAttribute
	{
		/// <summary>
		/// Gets collection with values to skip.
		/// </summary>
		protected HashSet<object> Values { get; set; }

		protected SkipValuesByListAttribute(params object[] values)
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
		/// <param name="obj">The object to check.</param>
		/// <param name="entityDescriptor">The entity descriptor.</param>
		/// <param name="columnDescriptor">The column descriptor.</param>
		/// <returns><c>true</c> if object should be skipped for the operation.</returns>
		public override bool ShouldSkip(object obj, EntityDescriptor entityDescriptor, ColumnDescriptor columnDescriptor)
		{
			// todo: replace MemberAccessor.Getter() with GetMemberValue
			return Values?.Contains(columnDescriptor.MemberAccessor.Getter(obj)) ?? false;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public override string Configuration { get; set; }

	}
}
