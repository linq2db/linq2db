namespace LinqToDB.DataModel
{
	/// <summary>
	/// Non-default schema model.
	/// </summary>
	public sealed class AdditionalSchemaModel : SchemaModelBase
	{
		public AdditionalSchemaModel(string dataContextPropertyName, ClassModel wrapperClass, ClassModel contextClass)
		{
			DataContextPropertyName = dataContextPropertyName;
			WrapperClass            = wrapperClass;
			ContextClass            = contextClass;
		}

		/// <summary>
		/// Name of property in main data context with reference to this schema class instance.
		/// </summary>
		public string     DataContextPropertyName { get; set; }

		/// <summary>
		/// Schema wrapper class descriptor.
		/// </summary>
		public ClassModel WrapperClass            { get; }
		/// <summary>
		/// Schema context class descriptor (nested class of wrapper, defined by <see cref="WrapperClass"/>).
		/// </summary>
		public ClassModel ContextClass            { get; }
	}
}
