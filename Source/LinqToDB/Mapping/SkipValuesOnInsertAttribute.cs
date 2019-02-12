using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on insert.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = false, Inherited = true)]
	public class SkipValuesOnInsertAttribute : Attribute
	{
		public SkipValuesOnInsertAttribute() { }

		/// <summary>  
		/// Constructor. 
		/// </summary>
		/// <param name="values"> 
		/// Values to skip on insert operations.
		/// </param>
		public SkipValuesOnInsertAttribute(params object[] values)
		{
			var valuesToSkip = values;
			if (valuesToSkip == null)
			{
				Values = new HashSet<object>(){null};
			}
			else if(valuesToSkip.Length > 0)
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
		/// Gets collection with values to skip on insert.
		/// </summary>
		public HashSet<object> Values { get; }
	}
}
