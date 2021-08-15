namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// <see cref="CodeConstructor"/> builder.
	/// </summary>
	public class ConstructorBuilder : MethodBaseBuilder<ConstructorBuilder, CodeConstructor>
	{
		internal ConstructorBuilder(CodeConstructor ctor)
			: base(ctor)
		{
		}

		/// <summary>
		/// add base constructor call.
		/// </summary>
		/// <param name="parameters">Base constructor parameters.</param>
		/// <returns>Constructor builder instance.</returns>
		public ConstructorBuilder Base(params ICodeExpression[] parameters)
		{
			Method.ThisCall = false;
			Method.BaseArguments.AddRange(parameters);
			return this;
		}
	}
}
