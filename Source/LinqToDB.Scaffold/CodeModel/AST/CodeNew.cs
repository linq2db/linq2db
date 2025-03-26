using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// New object instantiation expression.
	/// </summary>
	public sealed class CodeNew : ICodeExpression
	{
		public CodeNew(CodeTypeToken type, IEnumerable<ICodeExpression> parameters, IEnumerable<CodeAssignmentStatement> initializers)
		{
			Type          = type;
			Parameters   = parameters  .ToArray() ?? [];
			Initializers = initializers.ToArray() ?? [];
		}

		public CodeNew(IType type, IEnumerable<ICodeExpression> parameters, IEnumerable<CodeAssignmentStatement> initializers)
			: this(new CodeTypeToken(type), parameters, initializers)
		{
		}

		/// <summary>
		/// Instantiated type.
		/// </summary>
		public CodeTypeToken                          Type         { get; }
		/// <summary>
		/// Constructor parameters.
		/// </summary>
		public IReadOnlyList<ICodeExpression>         Parameters   { get; }
		/// <summary>
		/// Object initializer properties.
		/// </summary>
		public IReadOnlyList<CodeAssignmentStatement> Initializers { get; }

		IType           ICodeExpression.Type        => Type.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.New;
	}
}
