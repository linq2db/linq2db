namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// <see cref="CodeMethod"/> object builder.
	/// </summary>
	public class MethodBuilder : MethodBaseBuilder<MethodBuilder, CodeMethod>
	{
		internal MethodBuilder(CodeMethod method)
			: base(method)
		{
		}

		/// <summary>
		/// Marks method with static modifier.
		/// </summary>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder Static()
		{
			Method.Attributes |= Modifiers.Static;
			return this;
		}

		/// <summary>
		/// Marks method as static extension method.
		/// </summary>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder Extension()
		{
			Method.Attributes |= Modifiers.Static;
			Method.Attributes |= Modifiers.Extension;
			return this;
		}

		/// <summary>
		/// Adds generic type parameter to method signature.
		/// </summary>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder TypeParameter(IType typeParameter)
		{
			Method.TypeParameters.Add(new(typeParameter));
			return this;
		}

		/// <summary>
		/// Adds non-void return type to method.
		/// </summary>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder Returns(IType type)
		{
			Method.ReturnType = new(type);
			return this;
		}
	}
}
