using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on update.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SkipValuesOnUpdateAttribute : Attribute
	{
		public SkipValuesOnUpdateAttribute() { }

		/// <summary>  
		/// Constructor. 
		/// </summary>
		/// <param name="values"> 
		/// Values to skip on update operations.
		/// </param>
		public SkipValuesOnUpdateAttribute(params object[] values)
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
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets collection with values to skip on update.
		/// </summary>
		public HashSet<object> Values { get; }

		/// <summary>  Check if the passed value should be skipped on update. </summary>
		/// <param name="value">   The value that is checked for updateing. </param>
		/// <returns>  True if value should be skipped on update. </returns>
		public virtual bool Skip(object value)
		{
			return Values?.Contains(value) ?? false;
		}
	}
}
