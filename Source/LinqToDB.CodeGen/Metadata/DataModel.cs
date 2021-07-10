using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public class DataModel
	{
		public string? DatabaseName { get; set; }
		// TODO:  remove?
		public string? DataSource { get; set; }
		// TODO: remove?
		public string? ServerVersion { get; set; }

		public IList<Table> Tables { get; } = new List<Table>();
		public IList<View> Views { get; } = new List<View>();
		public IList<ForeignKey> ForeignKeys { get; } = new List<ForeignKey>();

		public IList<StoredProcedure> StoredProcedures { get; } = new List<StoredProcedure>();
		public IList<ScalarFunction> ScalarFunctions { get; } = new List<ScalarFunction>();
		public IList<TableFunction> TableFunctions { get; } = new List<TableFunction>();
		public IList<Aggregate> Aggregates { get; } = new List<Aggregate>();

		// TODO: temporary
		public ISet<string> Schemas { get; } = new HashSet<string>();
		public ISet<string> DefaultSchemas { get; } = new HashSet<string>();
		public IDictionary<DbType, (DataType dataType, Type type, string? providerType)> TypeMap { get; } = new Dictionary<DbType, (DataType dataType, Type type, string? providerType)>();
	}
}
