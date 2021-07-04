namespace LinqToDB.CodeGen.CodeModel
{
	public class MethodBuilder : MethodBaseBuilder<MethodBuilder, CodeMethod>
	{
		public MethodBuilder(CodeMethod method)
			: base(method)
		{
		}

		public MethodBuilder Static()
		{
			Method.Attributes |= MemberAttributes.Static;
			return this;
		}

		public MethodBuilder Extension()
		{
			Method.Attributes |= MemberAttributes.Static;
			Method.Attributes |= MemberAttributes.Extension;
			return this;
		}

		public MethodBuilder TypeParameter(IType typeParameter)
		{
			Method.TypeParameters.Add(new(typeParameter));
			return this;
		}

		public MethodBuilder Returns(IType type)
		{
			Method.ReturnType = new(type);
			return this;
		}
	}

}
