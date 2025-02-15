using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// XML-doc commentary.
	/// </summary>
	public sealed class CodeXmlComment : ICodeElement
	{
		private readonly List<ParameterComment> _parameters;

		internal CodeXmlComment(string? summary, IEnumerable<ParameterComment>? parameters)
		{
			Summary     = summary;
			_parameters = new (parameters ?? []);
		}

		public CodeXmlComment()
			: this(null, null)
		{
		}

		/// <summary>
		/// Summary documentation element.
		/// </summary>
		public string?                         Summary    { get; internal set; }
		/// <summary>
		/// Documentation for method/constructor parameters.
		/// </summary>
		public IReadOnlyList<ParameterComment> Parameters => _parameters;

		CodeElementType ICodeElement.ElementType => CodeElementType.XmlComment;

		internal void AddParameter(CodeIdentifier parameter, string comment)
		{
			_parameters.Add(new (parameter, comment));
		}

		// who needs tuples when we have records
		public record ParameterComment(CodeIdentifier Parameter, string Comment);
	}
}
