using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	public class PropertyModel
	{
		public PropertyModel(string name)
		{
			Name = name;
		}

		public PropertyModel(string name, IType type)
		{
			Name = name;
			Type = type;
		}

		public string Name { get; set; }

		public IType? Type { get; set; }

		public string? Summary { get; set; }

		public bool IsPublic { get; set; }

		public bool IsDefault { get; set; }

		public bool HasSetter { get; set; }

		public string? TrailingComment { get; set; }
	}
}
