namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeAttribute"/> custom attribute object builder.
	/// </summary>
	public sealed class AttributeBuilder
	{
		internal AttributeBuilder(CodeAttribute attribute)
		{
			Attribute = attribute;
		}

		/// <summary>
		/// Built custom attribute.
		/// </summary>
		public CodeAttribute Attribute { get; }

		/// <summary>
		/// Add positional parameter value.
		/// </summary>
		/// <param name="value">Parameter value.</param>
		/// <returns>Builder instance.</returns>
		public AttributeBuilder Parameter(ICodeExpression value)
		{
			Attribute.AddParameter(value);
			return this;
		}

		/// <summary>
		/// Add named parameter value.
		/// </summary>
		/// <param name="property">Attribute property name.</param>
		/// <param name="value">Parameter value.</param>
		/// <returns>Builder instance.</returns>
		public AttributeBuilder Parameter(CodeReference property, ICodeExpression value)
		{
			Attribute.AddNamedParameter(property, value);
			return this;
		}
	}
}
