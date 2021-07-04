using System.Collections.Generic;

namespace LinqToDB.CodeGen.ContextModel
{
	public class DataContextModel
	{
		public DataContextModel(DataContextClass dataContext, string @namespace)
		{
			DataContext = dataContext;
			Namespace = @namespace;
		}

		public string Namespace { get; }
		public DataContextClass DataContext { get; }
		public List<EntityModel> Entities { get; } = new List<EntityModel>();

		public string? DatabaseName { get; set; }
		public string? DataSource { get; set; }
		public string? ServerVersion { get; set; }

		public List<Association> Associations { get; } = new ();

		public List<StoredProcedureModel> StoredProcedures { get; } = new List<StoredProcedureModel>();
		public List<ScalarFunctionModel> ScalarFunctions { get; } = new List<ScalarFunctionModel>();
		public List<TableFunctionModel> TableFunctions { get; } = new List<TableFunctionModel>();
		public List<AggregateModel> Aggregates { get; } = new List<AggregateModel>();

		public List<SchemaContextModel> SchemaContexts { get; } = new ();

		public BadNameFixOptions? EntityColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.Suffix };
		public BadNameFixOptions? ResultColumn { get; set; } = new BadNameFixOptions() { Fixer = "Column", FixType = FixType.ReplaceWithPosition };
		public BadNameFixOptions? Parameter { get; set; } = new BadNameFixOptions() { Fixer = "parameter", FixType = FixType.ReplaceWithPosition };
	}
}
