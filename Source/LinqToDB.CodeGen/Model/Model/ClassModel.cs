namespace LinqToDB.CodeGen.Model
{
	public class ClassModel
	{
		public ClassModel(string name)
		{
			Name = name;
		}

		public string? Summary { get; set; }

		public string? Namespace { get; set; }
		public string Name { get; set; }
		public IType? BaseType { get; set; }

		public bool IsPublic { get; set; }
		public bool IsStatic { get; set; }
		public bool IsPartial { get; set; }
	}
}
