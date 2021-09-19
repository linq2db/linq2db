namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Assignment expression or statement.
	/// </summary>
	public abstract class CodeAssignmentBase
	{
		public CodeAssignmentBase(ILValue lvalue, ICodeExpression rvalue)
		{
			LValue = lvalue;
			RValue = rvalue;
		}

		/// <summary>
		/// Assignment target.
		/// </summary>
		public ILValue         LValue { get; }
		/// <summary>
		/// Assigned value.
		/// </summary>
		public ICodeExpression RValue { get; }
	}
}
