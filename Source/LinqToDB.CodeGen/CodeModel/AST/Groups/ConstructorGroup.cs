using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of constructors.
	/// </summary>
	public class ConstructorGroup : MemberGroup<CodeConstructor>
	{
		public ConstructorGroup(List<CodeConstructor>? members, CodeClass owner)
			: base(members)
		{
			Class = owner;
		}

		public ConstructorGroup(CodeClass owner)
			: this(null, owner)
		{
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass Class { get; }

		public override CodeElementType ElementType => CodeElementType.ConstructorGroup;

		public ConstructorBuilder New()
		{
			var ctor = new CodeConstructor(Class);
			Members.Add(ctor);
			return new ConstructorBuilder(ctor);
		}
	}
}
