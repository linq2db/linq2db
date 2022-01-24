namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeConstructor"/> builder.
	/// </summary>
	public sealed class ConstructorBuilder : MethodBaseBuilder<ConstructorBuilder, CodeConstructor>
	{
		internal ConstructorBuilder(CodeConstructor ctor)
			: base(ctor)
		{
		}

		/// <summary>
		/// Add base constructor call.
		/// </summary>
		/// <param name="parameters">Base constructor parameters.</param>
		/// <returns>Constructor builder instance.</returns>
		public ConstructorBuilder Base(params ICodeExpression[] parameters)
		{
			Method.ThisCall = false;
			Method.AddBaseParameters(parameters);
			return this;
		}
	}
}
