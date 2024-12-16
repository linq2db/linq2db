namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeClass"/> object builder.
	/// </summary>
	public sealed class ClassBuilder : TypeBuilder<ClassBuilder, CodeClass>
	{
		internal ClassBuilder(CodeClass @class, ClassGroup group)
			: base(@class, group)
		{
		}

		/// <summary>
		/// Add base type to inherit.
		/// </summary>
		/// <param name="baseClass">Base class descriptor.</param>
		/// <returns>Class builder instance.</returns>
		public ClassBuilder Inherits(IType baseClass)
		{
			Type.Inherits = new(baseClass);
			return this;
		}

		/// <summary>
		/// Add implemented interface to class.
		/// </summary>
		/// <param name="interface">Implemented interface descriptor.</param>
		/// <returns>Class builder instance.</returns>
		public ClassBuilder Implements(IType @interface)
		{
			Type.AddInterface(new (@interface));
			return this;
		}

		/// <summary>
		/// Add properties group.
		/// </summary>
		/// <param name="tableLayout">Group layout.</param>
		/// <returns>New group instance.</returns>
		public PropertyGroup Properties(bool tableLayout)
		{
			var group = new PropertyGroup(tableLayout);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add constructors group.
		/// </summary>
		/// <returns>New group instance.</returns>
		public ConstructorGroup Constructors()
		{
			var group = new ConstructorGroup(Type);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add fields group.
		/// </summary>
		/// <param name="tableLayout">Group layout.</param>
		/// <returns>New group instance.</returns>
		public FieldGroup Fields(bool tableLayout)
		{
			var group = new FieldGroup(tableLayout);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add methods group.
		/// </summary>
		/// <param name="tableLayout">Group layout.</param>
		/// <returns>New group instance.</returns>
		public MethodGroup Methods(bool tableLayout)
		{
			var group = new MethodGroup(tableLayout);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add regions group.
		/// </summary>
		/// <returns>New group instance.</returns>
		public RegionGroup Regions()
		{
			var group = new RegionGroup(Type);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add nested classes group group.
		/// </summary>
		/// <returns>New group instance.</returns>
		public ClassGroup Classes()
		{
			var group = new ClassGroup(Type);
			Type.AddMemberGroup(group);
			return group;
		}

		/// <summary>
		/// Add static constructor to class.
		/// </summary>
		/// <returns>Constructor builder instance.</returns>
		public TypeInitializerBuilder TypeInitializer()
		{
			var cctor = new CodeTypeInitializer(Type);
			Type.TypeInitializer = cctor;
			return new TypeInitializerBuilder(cctor);
		}
	}
}
