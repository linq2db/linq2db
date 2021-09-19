using System.Collections.Generic;

namespace LinqToDB.CodeGen.DataModel
{
	public class DataContextModel : SchemaModelBase
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

		public Dictionary<string, AdditionalSchemaModel> AdditionalSchemas { get; } = new();

		//public BadNameFixOptions? EntityColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.Suffix };
		//public BadNameFixOptions? ResultColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.ReplaceWithPosition };
		//public BadNameFixOptions? Parameter { get; set; } = new BadNameFixOptions() { Fixer = "parameter", FixType = FixType.ReplaceWithPosition };


		// find, association extensions, stored procedures
		//public string ExtensionsClassName { get; set; }
		//public string ScalarFunctionsClassName { get; set; }
	}
}
