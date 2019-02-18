using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on insert.
	/// </summary>
	public class SkipValuesOnInsertAttribute : SkipBaseAttribute
	{
		public SkipValuesOnInsertAttribute()
		{
			Affects = SkipModification.Insert;
		}

		/// <summary>  
		/// Constructor. 
		/// </summary>
		/// <param name="values"> 
		/// Values to skip on insert operations.
		/// </param>
		public SkipValuesOnInsertAttribute(params object[] values):this()
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
		/// Gets collection with values to skip on insert.
		/// </summary>
		public HashSet<object> Values { get; }

      /// <summary>  Check if the passed value should be skipped on insert. </summary>
      /// <param name="value">   The value that is checked for inserting. </param>
      /// <returns>  True if value should be skipped on insert. </returns>
		public virtual bool Skip(object value)
		{
			return Values?.Contains(value) ?? false;
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
		public override string Configuration { get; set; }
	}
}
