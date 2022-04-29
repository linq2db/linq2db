﻿using System.Collections.Generic;
using LinqToDB.Common;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for elements with custom attributes.
	/// </summary>
	public abstract class AttributeOwner : ICodeElement
	{
		private readonly List<CodeAttribute> _customAttributes;

		protected AttributeOwner(IEnumerable<CodeAttribute>? customAttributes)
		{
			_customAttributes = new (customAttributes ?? Array<CodeAttribute>.Empty);
		}

		/// <summary>
		/// Custom attributes.
		/// </summary>
		public IReadOnlyList<CodeAttribute> CustomAttributes => _customAttributes;

		public abstract CodeElementType     ElementType      { get; }

		internal void AddAttribute(CodeAttribute attribute)
		{
			_customAttributes.Add(attribute);
		}
	}
}
