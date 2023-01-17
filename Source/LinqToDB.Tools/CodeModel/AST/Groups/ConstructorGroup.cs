using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of constructors.
	/// </summary>
	public sealed class ConstructorGroup : MemberGroup<CodeConstructor>
	{
		public ConstructorGroup(IEnumerable<CodeConstructor>? members, CodeClass owner)
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
		public CodeClass Class { get; set; }

		public override CodeElementType ElementType => CodeElementType.ConstructorGroup;

		public ConstructorBuilder New()
		{
			return new ConstructorBuilder(AddMember(new CodeConstructor(Class)));
		}
	}
}
