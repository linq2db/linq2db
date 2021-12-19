using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	
	/// <summary>
	/// Custom attribute declaration.
	/// </summary>
	public sealed class CodeAttribute : ITopLevelElement
	{
		private readonly List<ICodeExpression>    _parameters;
		private readonly List<CodeNamedParameter> _namedParameters;

		internal CodeAttribute(
			CodeTypeToken                    type,
			IEnumerable<ICodeExpression>?    parameters,
			IEnumerable<CodeNamedParameter>? namedParameters)
		{
			Type             = type;
			_parameters      = new (parameters      ?? Array.Empty<ICodeExpression>());
			_namedParameters = new (namedParameters ?? Array.Empty<CodeNamedParameter>());
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

		// use pretty record instead of ugly tuple
		public record CodeNamedParameter(CodeIdentifier Property, ICodeExpression Value);

		internal void AddParameter(ICodeExpression parameterValue)
		{
			_parameters.Add(parameterValue);
		}

		internal void AddNamedParameter(CodeIdentifier property, ICodeExpression value)
		{
			_namedParameters.Add(new(property, value));
		}
	}
}
