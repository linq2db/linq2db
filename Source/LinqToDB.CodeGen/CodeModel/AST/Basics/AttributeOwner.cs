using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for elements with custom attributes.
	/// </summary>
	public abstract class AttributeOwner : ICodeElement
	{
		/// <summary>
		/// Custom attributes.
		/// </summary>
		public List<CodeAttribute> CustomAttributes { get; set; } = new();

		public abstract CodeElementType ElementType { get; }
	}
}
