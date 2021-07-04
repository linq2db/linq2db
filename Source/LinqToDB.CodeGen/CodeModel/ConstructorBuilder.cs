namespace LinqToDB.CodeGen.CodeModel
{
	public class ConstructorBuilder : MethodBaseBuilder<ConstructorBuilder, CodeConstructor>
	{
		public ConstructorBuilder(CodeConstructor ctor)
			: base(ctor)
		{
		}

		public ConstructorBuilder Base(params ICodeExpression[] parameters)
		{
			Method.ThisCall = false;
			Method.BaseArguments.AddRange(parameters);
			return this;
		}
	}

}
