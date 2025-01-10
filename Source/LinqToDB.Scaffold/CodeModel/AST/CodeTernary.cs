using System;

namespace LinqToDB.CodeModel
{
	public sealed class CodeTernary : ICodeExpression
	{
		public CodeTernary(ICodeExpression condition, ICodeExpression trueValue, ICodeExpression falseValue)
		{
			Condition = condition;
			True      = trueValue;
			False     = falseValue;
		}

		/// <summary>
		/// Condition expression.
		/// </summary>
		public ICodeExpression Condition { get; }
		/// <summary>
		/// True value expression.
		/// </summary>
		public ICodeExpression True      { get; }
		/// <summary>
		/// False value expression.
		/// </summary>
		public ICodeExpression False     { get; }

		// for now type it implicitly using True argument type
		IType           ICodeExpression.Type        => True.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.TernaryOperation;
	}
}
