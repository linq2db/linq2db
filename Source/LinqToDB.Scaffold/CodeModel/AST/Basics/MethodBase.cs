using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for method-like nodes.
	/// </summary>
	public abstract class MethodBase : AttributeOwner
	{
		private readonly List<CodeParameter> _parameters;

		protected MethodBase(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeBlock?                  body,
			CodeXmlComment?             xmlDoc,
			IEnumerable<CodeParameter>? parameters)
			: base(customAttributes)
		{
			Attributes  = attributes;
			Body        = body;
			XmlDoc      = xmlDoc;
			_parameters = [.. parameters ?? []];
		}

		/// <summary>
		/// Method modifiers.
		/// </summary>
		public Modifiers           Attributes          { get; internal set; }
		/// <summary>
		/// Method body (top-level block).
		/// </summary>
		public CodeBlock?          Body                { get; internal set; }
		/// <summary>
		/// Xml-documentation comment.
		/// </summary>
		public CodeXmlComment?     XmlDoc              { get; internal set; }
		/// <summary>
		/// Parameters collection.
		/// </summary>
		public IReadOnlyList<CodeParameter> Parameters => _parameters;

		internal void AddParameter(CodeParameter parameter)
		{
			_parameters.Add(parameter);
		}
	}
}
