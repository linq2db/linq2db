namespace LinqToDB.CodeGen.ContextModel
{
	public class MethodModel
	{
		public MethodModel(string name)
		{
			Name = name;
		}

		public string? Summary { get; set; }
		public string Name { get; set; }
		public bool Public { get; set; }
		public bool Static { get; set; }
		public bool Partial { get; set; }
	}
}
