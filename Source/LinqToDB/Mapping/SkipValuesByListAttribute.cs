﻿using System;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Abstract Attribute to be used for skipping value for
	/// <see cref="SkipValuesOnInsertAttribute"/> based on <see cref="SkipModification.Insert"></see> or
	/// <see cref="SkipValuesOnUpdateAttribute"/> based on <see cref="SkipModification.Update"/>/> or a
	/// custom Attribute derived from this to override <see cref="SkipBaseAttribute.ShouldSkip"/>
	/// </summary>
	[CLSCompliant(false)]
	public abstract class SkipValuesByListAttribute: SkipBaseAttribute
	{
		/// <summary>
		/// Gets collection with values to skip.
		/// </summary>
		protected HashSet<object> Values { get; set; }

		protected SkipValuesByListAttribute(IEnumerable<object> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			Values = new HashSet<object>(values);
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
			return Values?.Contains(columnDescriptor.MemberAccessor.Getter(obj)) ?? false;
		}
	}
}
