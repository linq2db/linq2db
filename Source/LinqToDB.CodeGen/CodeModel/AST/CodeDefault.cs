namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Default value expression.
	/// </summary>
	public class CodeDefault : ICodeExpression
	{
		public CodeDefault(IType type, bool targetTyped)
		{
			Type        = new (type);
			TargetTyped = targetTyped;
		}

		/// <summary>
		/// Value type.
		/// </summary>
		public CodeTypeToken Type        { get; }
		/// <summary>
		/// Indicates that default value is typed by context so type could be ommited during code generation.
		/// </summary>
		public bool          TargetTyped { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Default;
	}

}
