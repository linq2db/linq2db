using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
   /// <summary>  Attribute for skipping specific values to be inserted. </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = false, Inherited = true)]
	public class SkipValuesOnInsertAttribute : Attribute
	{
		private readonly HashSet<object> _valuesToSkip = null;

		public SkipValuesOnInsertAttribute() { }
		public SkipValuesOnInsertAttribute(params object[] values)
		{
			var valuesToSkip = values;
			if (valuesToSkip == null)
			{
				_valuesToSkip = new HashSet<object>(){null};
			}
			else if(valuesToSkip.Length > 0)
			{
				_valuesToSkip = new HashSet<object>(values);
			}
		}

		public HashSet<object> Values
		{
			get => _valuesToSkip;
		}

	}
}
