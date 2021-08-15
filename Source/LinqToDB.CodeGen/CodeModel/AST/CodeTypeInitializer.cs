namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type initializer (static constructor).
	/// </summary>
	public class CodeTypeInitializer : MethodBase
	{
		public CodeTypeInitializer(CodeClass type)
		{
			Type = type;
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass Type { get; }

		public override CodeElementType ElementType => CodeElementType.TypeConstructor;
	}
}
