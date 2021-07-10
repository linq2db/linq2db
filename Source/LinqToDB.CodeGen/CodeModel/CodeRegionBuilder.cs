namespace LinqToDB.CodeGen.CodeModel
{

	public class CodeRegionBuilder
	{
		public CodeRegionBuilder(CodeRegion region)
		{
			Region = region;
		}

		public CodeRegion Region { get; }

		public PropertyGroup Properties(bool tableLayot)
		{
			var group = new PropertyGroup(tableLayot);
			Region.Members.Add(group);
			return group;
		}

		public ConstructorGroup Constructors()
		{
			var group = new ConstructorGroup(Region.Type!);
			Region.Members.Add(group);
			return group;
		}

		public MethodGroup Methods(bool tableLayout)
		{
			var group = new MethodGroup(tableLayout);
			Region.Members.Add(group);
			return group;
		}

		public ClassGroup Classes()
		{
			var group = new ClassGroup(Region.Type!);
			Region.Members.Add(group);
			return group;
		}

		public RegionGroup Regions()
		{
			var group = new RegionGroup(Region.Type!);
			Region.Members.Add(group);
			return group;
		}

		public FieldGroup Fields(bool tableLayot)
		{
			var group = new FieldGroup(tableLayot);
			Region.Members.Add(group);
			return group;
		}

		public PragmaGroup Pragmas()
		{
			var group = new PragmaGroup();
			Region.Members.Add(group);
			return group;
		}
	}
}
