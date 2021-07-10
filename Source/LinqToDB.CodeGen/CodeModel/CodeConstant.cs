namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeConstant : ICodeExpression
	{
		public CodeConstant(IType type, object? value, bool targetTyped)
		{
			Type = new(type);
			Value = value;
			TargetTyped = targetTyped;
		}

		public TypeToken Type { get; }
		public object? Value { get; }
		public bool TargetTyped { get; }

		public CodeElementType ElementType => CodeElementType.Constant;
	}

}
