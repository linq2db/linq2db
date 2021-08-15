namespace LinqToDB.CodeGen.Model
{
	public class ExplicitSchemaModel : SchemaModel
	{
		public ExplicitSchemaModel(string dataContextPropertyName, ClassModel wrapperClass, ClassModel contextClass)
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
