namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeParameter : ICodeElement
	{
		public CodeParameter(IType? type, CodeIdentifier name, Direction direction)
		{
			Type = type == null ? null : new (type);
			Name = name;
			Direction = direction;
		}

		public TypeToken? Type { get; }
		public CodeIdentifier Name { get; }
		public Direction Direction { get; }

		public CodeElementType ElementType => CodeElementType.Parameter;
	}

	// technically it is not parameter attribute, but type[ + custom attribute], but we don't need such details
	public enum Direction
	{
		In,
		Ref,
		Out
	}

}
