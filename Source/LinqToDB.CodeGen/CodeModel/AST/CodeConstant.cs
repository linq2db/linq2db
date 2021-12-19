namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Constant expression. E.g. literal (including <c>null</c> literal) or enumeration value.
	/// </summary>
	public sealed class CodeConstant : ICodeExpression
	{
		internal CodeConstant(CodeTypeToken type, object? value, bool targetTyped)
		{
			Type        = type;
			Value       = value;
			TargetTyped = targetTyped;
		}

		public CodeConstant(IType type, object? value, bool targetTyped)
			: this(new CodeTypeToken(type), value, targetTyped)
		{
		}

		/// <summary>
		/// Constant type.
		/// </summary>
		public CodeTypeToken Type        { get; }
		/// <summary>
		/// Constant value.
		/// </summary>
		public object?       Value       { get; }
		/// <summary>
		/// Indicates that constant type is constrained by context (e.g. used in assignment to property of specific type)
		/// and code generator could use it to ommit type information.
		/// </summary>
		public bool          TargetTyped { get; }

		IType           ICodeExpression.Type        => Type.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Constant;
	}
}
