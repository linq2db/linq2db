namespace LinqToDB.CodeGen.CodeModel
{
	public class NewExpression : ICodeExpression
	{
		public NewExpression(IType type, ICodeExpression[] parameters, AssignExpression[] initializers)
		{
			Type = new (type);
			Parameters = parameters;
			Initializers = initializers;
		}

		public TypeToken Type { get; }
		public ICodeExpression[] Parameters { get; }
		public AssignExpression[] Initializers { get; }

		public CodeElementType ElementType => CodeElementType.New;

	}
}
