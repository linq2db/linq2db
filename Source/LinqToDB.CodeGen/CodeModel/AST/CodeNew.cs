using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// New object instantiation expression.
	/// </summary>
	public sealed class CodeNew : ICodeExpression
	{
		public CodeNew(CodeTypeToken type, IReadOnlyList<ICodeExpression> parameters, IReadOnlyList<CodeAssignmentStatement> initializers)
		{
			Type         = type;
			Parameters   = parameters;
			Initializers = initializers;
		}

		public CodeNew(IType type, IReadOnlyList<ICodeExpression> parameters, IReadOnlyList<CodeAssignmentStatement> initializers)
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

		IType ICodeExpression.Type => Type.Type;

		CodeElementType ICodeElement.ElementType => CodeElementType.New;
	}
}
