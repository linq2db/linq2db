namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Assignment expression or statement.
	/// </summary>
	public abstract class CodeAssignmentBase
	{
		protected CodeAssignmentBase(ILValue lvalue, ICodeExpression rvalue)
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
