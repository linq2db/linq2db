namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeField"/> object builder.
	/// </summary>
	public sealed class FieldBuilder
	{
		internal FieldBuilder(CodeField field)
		{
			Field = field;
		}

		/// <summary>
		/// Built field object.
		/// </summary>
		public CodeField Field { get; }

		/// <summary>
		/// Mark field as public.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public FieldBuilder Public()
		{
			Field.Attributes |= Modifiers.Public;
			return this;
		}

		/// <summary>
		/// Mark field as private.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public FieldBuilder Private()
		{
			Field.Attributes |= Modifiers.Private;
			return this;
		}

		/// <summary>
		/// Mark field as static.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public FieldBuilder Static()
		{
			Field.Attributes |= Modifiers.Static;
			return this;
		}

		/// <summary>
		/// Mark field as readonly.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public FieldBuilder ReadOnly()
		{
			Field.Attributes |= Modifiers.ReadOnly;
			return this;
		}

		/// <summary>
		/// Add field initializer.
		/// </summary>
		/// <param name="initializer">Initializer expression.</param>
		/// <returns>Builder instance.</returns>
		public FieldBuilder AddInitializer(ICodeExpression initializer)
		{
			Field.Initializer = initializer;
			return this;
		}
	}
}
