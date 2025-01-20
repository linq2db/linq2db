namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeMethod"/> object builder.
	/// </summary>
	public sealed class MethodBuilder : MethodBaseBuilder<MethodBuilder, CodeMethod>
	{
		internal MethodBuilder(CodeMethod method)
			: base(method)
		{
		}

		/// <summary>
		/// Adds generic type parameter to method signature.
		/// </summary>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder TypeParameter(IType typeParameter)
		{
			Method.AddGenericParameter(new(typeParameter));
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

		/// <summary>
		/// Add custom attribute to method.
		/// </summary>
		/// <param name="attribute">Custom attribute.</param>
		/// <returns>Method builder instance.</returns>
		public MethodBuilder AddAttribute(CodeAttribute attribute)
		{
			Method.AddAttribute(attribute);
			return this;
		}
	}
}
