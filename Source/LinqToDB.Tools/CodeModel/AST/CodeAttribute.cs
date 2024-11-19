using System.Collections.Generic;
using LinqToDB.Common;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Custom attribute declaration.
	/// </summary>
	public sealed class CodeAttribute : ITopLevelElement
	{
		public record CodeNamedParameter(CodeReference Property, ICodeExpression Value);

		private readonly List<ICodeExpression>    _parameters;
		private readonly List<CodeNamedParameter> _namedParameters;

		public CodeAttribute(
			CodeTypeToken                    type,
			IEnumerable<ICodeExpression>?    parameters,
			IEnumerable<CodeNamedParameter>? namedParameters)
		{
			Type             = type;
			_parameters      = new (parameters      ?? []);
			_namedParameters = new (namedParameters ?? []);
		}

		public CodeAttribute(IType type)
			: this(new (type), null, null)
		{
		}

		/// <summary>
		/// Attribute type.
		/// </summary>
		public CodeTypeToken                     Type            { get; }
		/// <summary>
		/// Positional attribute parameters.
		/// </summary>
		public IReadOnlyList<ICodeExpression>    Parameters      => _parameters;
		/// <summary>
		/// Named attribute parameters.
		/// </summary>
		public IReadOnlyList<CodeNamedParameter> NamedParameters => _namedParameters;

		CodeElementType ICodeElement.ElementType => CodeElementType.Attribute;

		public void AddParameter(ICodeExpression parameterValue)
		{
			_parameters.Add(parameterValue);
		}

		public void AddNamedParameter(CodeReference property, ICodeExpression value)
		{
			_namedParameters.Add(new(property, value));
		}
	}
}
