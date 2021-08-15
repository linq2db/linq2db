using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	public class DataContextModel : SchemaModel
	{
		public DataContextModel(ClassModel classModel)
		{
			Class = classModel;
		}

		public ClassModel Class { get; set; }

		public bool HasDefaultConstructor { get; set; }
		public bool HasConfigurationConstructor { get; set; }
		public bool HasUntypedOptionsConstructor { get; set; }
		public bool HasTypedOptionsConstructor { get; set; }

		public Dictionary<string, ExplicitSchemaModel> AdditionalSchemas { get; } = new();

		//public BadNameFixOptions? EntityColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.Suffix };
		//public BadNameFixOptions? ResultColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.ReplaceWithPosition };
		//public BadNameFixOptions? Parameter { get; set; } = new BadNameFixOptions() { Fixer = "parameter", FixType = FixType.ReplaceWithPosition };
	}
}
