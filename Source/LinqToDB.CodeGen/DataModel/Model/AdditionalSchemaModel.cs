namespace LinqToDB.CodeGen.DataModel
{
	public class AdditionalSchemaModel : SchemaModelBase
	{
		public AdditionalSchemaModel(string dataContextPropertyName, ClassModel wrapperClass, ClassModel contextClass)
		{
			DataContextPropertyName = dataContextPropertyName;
			WrapperClass = wrapperClass;
			ContextClass = contextClass;
		}

		public string DataContextPropertyName { get; set; }

		public ClassModel WrapperClass { get; }
		public ClassModel ContextClass { get; }
	}
}
