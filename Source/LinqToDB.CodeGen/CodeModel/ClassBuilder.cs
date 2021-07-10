namespace LinqToDB.CodeGen.CodeModel
{

	public class ClassBuilder : TypeBuilder<ClassBuilder, CodeClass>
	{
		public ClassBuilder(CodeClass @class)
			: base(@class)
		{
		}

		public ClassBuilder Static()
		{
			Type.Attributes |= MemberAttributes.Static;
			return this;
		}

		public ClassBuilder Partial()
		{
			Type.Attributes |= MemberAttributes.Partial;
			return this;
		}

		public ClassBuilder Inherits(IType baseClass)
		{
			Type.Inherits = new(baseClass);
			return this;
		}

		public ClassBuilder Implements(IType @interface)
		{
			Type.Implements.Add(new (@interface));
			return this;
		}

		public PropertyGroup Properties(bool tableLayot)
		{
			var group = new PropertyGroup(tableLayot);
			Type.Members.Add(group);
			return group;
		}

		public ConstructorGroup Constructors()
		{
			var group = new ConstructorGroup(Type);
			Type.Members.Add(group);
			return group;
		}

		public FieldGroup Fields(bool tableLayot)
		{
			var group = new FieldGroup(tableLayot);
			Type.Members.Add(group);
			return group;
		}

		public MethodGroup Methods(bool tableLayout)
		{
			var group = new MethodGroup(tableLayout);
			Type.Members.Add(group);
			return group;
		}

		public RegionGroup Regions()
		{
			var group = new RegionGroup(Type);
			Type.Members.Add(group);
			return group;
		}

		public ClassGroup Classes()
		{
			var group = new ClassGroup(Type);
			Type.Members.Add(group);
			return group;
		}
	}
}
