using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for method-like nodes.
	/// </summary>
	public abstract class MethodBase : AttributeOwner
	{
		/// <summary>
		/// Method modifiers.
		/// </summary>
		public Modifiers           Attributes       { get; set; }
		/// <summary>
		/// Method body (top-level block).
		/// </summary>
		public CodeBlock?          Body             { get; set; }
		/// <summary>
		/// Xml-documentation comment.
		/// </summary>
		public CodeXmlComment?     XmlDoc           { get; set; }
		/// <summary>
		/// Parameters collection.
		/// </summary>
		public List<CodeParameter> Parameters       { get; } = new ();
	}
}
