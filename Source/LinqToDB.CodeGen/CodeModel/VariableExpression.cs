namespace LinqToDB.CodeGen.CodeModel
{
	public class VariableExpression : ILValue
	{
		public VariableExpression(CodeIdentifier name, IType type, bool rvalueTyped)
		{
			Name = name;
			Type = new (type);
			RValueTyped = rvalueTyped;
		}

		public CodeIdentifier Name { get; }
		public TypeToken Type { get; }
		public bool RValueTyped { get; }

		public CodeElementType ElementType => CodeElementType.Variable;
	}
}
