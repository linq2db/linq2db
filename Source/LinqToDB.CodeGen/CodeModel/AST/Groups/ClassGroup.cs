using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of classes.
	/// </summary>
	public class ClassGroup : MemberGroup<CodeClass>, ITopLevelElement
	{
		public ClassGroup(List<CodeClass>? members, ITopLevelElement? owner)
			: base(members)
		{
			Owner = owner;
		}

		public ClassGroup(ITopLevelElement? owner)
			: this(null, owner)
		{
		}

		/// <summary>
		/// Optional class parent: parent class or namespace.
		/// </summary>
		public ITopLevelElement? Owner { get; }

		public override CodeElementType ElementType => CodeElementType.ClassGroup;

		public ClassBuilder New(CodeIdentifier name)
		{
			CodeClass @class;
			if (Owner is CodeClass parentClass)
				@class = new CodeClass(parentClass, name);
			else if (Owner is CodeNamespace ns)
				@class = new CodeClass(ns.Name, name);
			else if (Owner == null)
				@class = new CodeClass((CodeIdentifier[]?)null, name);
			else
				throw new InvalidOperationException();

			Members.Add(@class);

			return new ClassBuilder(@class);
		}
	}
}
