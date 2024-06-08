namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Default value expression.
	/// </summary>
	public sealed class CodeDefault : ICodeExpression
	{
		public CodeDefault(CodeTypeToken type, bool targetTyped)
		{
			Type        = type;
			TargetTyped = targetTyped;
		}

		public CodeDefault(IType type, bool targetTyped)
			: this(new CodeTypeToken(type), targetTyped)
		{
		}

		/// <summary>
		/// Value type.
		/// </summary>
		public CodeTypeToken Type        { get; }
		/// <summary>
		/// Indicates that default value is typed by context so type could be ommited during code generation.
		/// </summary>
		public bool          TargetTyped { get; }

		IType           ICodeExpression.Type        => Type.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Default;
	}
}
