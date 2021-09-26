using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for elements with custom attributes.
	/// </summary>
	public abstract class AttributeOwner : ICodeElement
	{
		protected AttributeOwner(List<CodeAttribute>? customAttributes)
		{
			CustomAttributes = customAttributes ?? new ();
		}

		/// <summary>
		/// Custom attributes.
		/// </summary>
		public          List<CodeAttribute> CustomAttributes { get; set; }

		public abstract CodeElementType     ElementType      { get; }
	}
}
