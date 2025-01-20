namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeRegion"/> object builder.
	/// </summary>
	public sealed class RegionBuilder
	{
		internal RegionBuilder(CodeRegion region)
		{
			Region = region;
		}

		/// <summary>
		/// Built region instance.
		/// </summary>
		public CodeRegion Region { get; }

		/// <summary>
		/// Add property group to region.
		/// </summary>
		/// <param name="tableLayout">group layout type.</param>
		/// <returns>Returns new property group in current region.</returns>
		public PropertyGroup Properties(bool tableLayout)
		{
			var group = new PropertyGroup(tableLayout);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add constructors group to region.
		/// </summary>
		/// <returns>Returns new constructors group in current region.</returns>
		public ConstructorGroup Constructors()
		{
			var group = new ConstructorGroup(Region.Type);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add methods group to region.
		/// </summary>
		/// <param name="tableLayout">group layout type.</param>
		/// <returns>Returns new methods group in current region.</returns>
		public MethodGroup Methods(bool tableLayout)
		{
			var group = new MethodGroup(tableLayout);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add nested classes group to region.
		/// </summary>
		/// <returns>Returns new nested classes group in current region.</returns>
		public ClassGroup Classes()
		{
			var group = new ClassGroup(Region.Type);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add regions group to region.
		/// </summary>
		/// <returns>Returns new regions group in current region.</returns>
		public RegionGroup Regions()
		{
			var group = new RegionGroup(Region.Type);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add fields group to region.
		/// </summary>
		/// <param name="tableLayout">group layout type.</param>
		/// <returns>Returns new fields group in current region.</returns>
		public FieldGroup Fields(bool tableLayout)
		{
			var group = new FieldGroup(tableLayout);
			Region.Add(group);
			return group;
		}

		/// <summary>
		/// Add pragmas group to region.
		/// </summary>
		/// <returns>Returns new pragmas group in current region.</returns>
		public PragmaGroup Pragmas()
		{
			var group = new PragmaGroup();
			Region.Add(group);
			return group;
		}
	}
}
