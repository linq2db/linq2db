using System;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of classes.
	/// </summary>
	public class ClassGroup : MemberGroup<CodeClass>, ITopLevelElement
	{
		private readonly CodeClass?     _class;
		private readonly CodeNamespace? _namespace;

		public ClassGroup(ITopLevelElement? owner)
		{
			if (owner is CodeClass @class)
				_class = @class;
			else if (owner is CodeNamespace ns)
				_namespace = ns;
			else if (owner != null)
				throw new InvalidOperationException();
		}

		public override CodeElementType ElementType => CodeElementType.ClassGroup;

		public ClassBuilder New(CodeIdentifier name)
		{
			var @class = _class != null ? new CodeClass(_class, name) : new CodeClass(_namespace?.Name, name);
			Members.Add(@class);
			return new ClassBuilder(@class);
		}
	}
}
