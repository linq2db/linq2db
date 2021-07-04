using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen.CodeModel
{

	public class CodeIdentifier : ICodeExpression, ILValue
	{
		public CodeIdentifier(string name)
		{
			Name = name;
		}

		public CodeIdentifier(string name, BadNameFixOptions? fixOptions, int? position)
		{
			Name = name;
			FixOptions = fixOptions;
			Position = position;
		}

		public string Name { get; set; }

		public BadNameFixOptions? FixOptions { get; }
		public int? Position { get; }

		// TODO:
		//public bool Protected { get; }

		public CodeElementType ElementType => CodeElementType.Identifier;
	}
}
