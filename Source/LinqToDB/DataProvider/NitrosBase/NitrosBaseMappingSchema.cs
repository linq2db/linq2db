namespace LinqToDB.DataProvider.NitrosBase
{
	using LinqToDB.Common;
	using Mapping;

	/// <summary>
	/// NitrosBase provider schema.
	/// </summary>
	public class NitrosBaseMappingSchema : MappingSchema
	{
		/// <summary>
		/// Default schema constructor.
		/// Provider mapping schema should always have default constructor, because it is required for remote context to function properly.
		/// </summary>
		public NitrosBaseMappingSchema() : this(ProviderName.NitrosBase)
		{
		}

		/// <summary>
		/// Combining constructor to merge default schema with additional schemas, provided by user.
		/// </summary>
		/// <param name="schemas">Additional schemas.</param>
		internal NitrosBaseMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.NitrosBase, Array<MappingSchema>.Append(schemas, Instance))
		{
		}

		protected NitrosBaseMappingSchema(string configuration) : base(configuration)
		{
			// TODO: add/override default schema configuration
		}

		/// <summary>
		/// Default provider instance. It is not recommended to edit this schema. Instead of it you should create
		/// new <see cref="MappingSchema"/> instance, pass this instance to it and configure your schema object.
		/// </summary>
		internal static MappingSchema Instance { get; } = new NitrosBaseMappingSchema();
	}
}
