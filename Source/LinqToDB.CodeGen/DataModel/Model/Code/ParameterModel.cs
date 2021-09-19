using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	public class ParameterModel
	{
		public ParameterModel(string name, IType type, ParameterDirection direction)
		{
			Name = name;
			Type = type;
			Direction = direction;
		}
		public string Name { get; set; }
		public IType Type { get; set; }
		
		public string? Description { get; set; }

		public ParameterDirection Direction { get; set; }
	}
}
