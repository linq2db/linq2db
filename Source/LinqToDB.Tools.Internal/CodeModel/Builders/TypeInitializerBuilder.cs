namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeTypeInitializer"/> method builder.
	/// </summary>
	public sealed class TypeInitializerBuilder : MethodBaseBuilder<TypeInitializerBuilder, CodeTypeInitializer>
	{
		internal TypeInitializerBuilder(CodeTypeInitializer cctor)
			: base(cctor)
		{
		}
	}
}
