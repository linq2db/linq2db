using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Implements schema provider for MS Access, that takes schema from OLE DB and ODBC providers and merge it into single schema
	/// without errors, existing in both providers.
	/// </summary>
	public sealed class MergedAccessSchemaProvider : ISchemaProvider
	{
		private readonly ISchemaProvider _oleDbSchema;
		private readonly ISchemaProvider _odbcSchema;

		/// <summary>
		/// Creates instance of <see cref="MergedAccessSchemaProvider"/>.
		/// </summary>
		/// <param name="oleDbSchema">OLE DB-based Access schema provider.</param>
		/// <param name="odbcSchema">ODBC-based Access schema provider.</param>
		public MergedAccessSchemaProvider(ISchemaProvider oleDbSchema, ISchemaProvider odbcSchema)
		{
			_oleDbSchema = oleDbSchema;
			_odbcSchema  = odbcSchema;
		}

		// both OLE DB and ODBC Access providers have errors in provided database schema, but they occur only on one provider
		// this means we can take missing information from other provider
		//
		// Known issues in schema:
		// - ODBC doesn't provide full constrains information: PK, FK, indexes and unique constrains. List of constrains with fields provided, but there is no constrain type so it is not possible to identify it
		// - ODBC doesn't provide nullability information for columns in tables and procedure schema. All columns marked as nullable except COUNTER and BIT
		// - ODBC doesn't provide type length information for procedure parameters and procedure result columns
		// - ODBC doesn't list some procedures, visible from OLE DB (no idea why)
		// - OLE DB doesn't report counter columns as identity
		// - OLE DB reports counter column in procedure result as nullable when it cannot be null
		//
		// Because OLE DB contains less errors and they are less complex, we use OLE DB schema as base for all schema provider API
		// and patch them with ODBC data for two known issues related to counter columns

		// not really of big importance, but ODBC also returns full file path instead of db name
		string?         ISchemaProvider.DatabaseName    => _oleDbSchema.DatabaseName;
		string?         ISchemaProvider.ServerVersion   => _oleDbSchema.ServerVersion;
		string?         ISchemaProvider.DataSource      => _oleDbSchema.DataSource;
		DatabaseOptions ISchemaProvider.DatabaseOptions => _oleDbSchema.DatabaseOptions;

		IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions() => _oleDbSchema.GetAggregateFunctions();
		ISet<string>                   ISchemaProvider.GetDefaultSchemas    () => _oleDbSchema.GetDefaultSchemas();
		IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys       () => _oleDbSchema.GetForeignKeys();
		IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions   () => _oleDbSchema.GetScalarFunctions();
		IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions    () => _oleDbSchema.GetTableFunctions();

		IEnumerable<StoredProcedure> ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly)
		{
			if (!withSchema/* || safeSchemaOnly*/)
			{
				foreach (var proc in _oleDbSchema.GetProcedures(withSchema, safeSchemaOnly))
					yield return proc;
			}

			var odbcProcedures = _odbcSchema.GetProcedures(withSchema, safeSchemaOnly).ToDictionary(_ => _.Name);

			foreach (var proc in _oleDbSchema.GetProcedures(withSchema, safeSchemaOnly))
			{
				if (proc.ResultSets != null && proc.ResultSets.Count == 1 && odbcProcedures.TryGetValue(proc.Name, out var odbcProc) && odbcProc.ResultSets != null && proc.ResultSets.Count == odbcProc.ResultSets.Count)
				{
					var oleDbSet = proc.ResultSets[0];
					var odbcSet  = odbcProc.ResultSets[0];
					if (oleDbSet.Count == odbcSet.Count)
					{
						List<ResultColumn>? newSet = null;
						for (var i = 0; i < oleDbSet.Count; i++)
						{
							// non-nullable COUNTER column is nullable in OLEDB in procedure results
							if (oleDbSet[i].Nullable && odbcSet[i].Type.Name == "COUNTER" && !odbcSet[i].Nullable)
							{
								if (newSet == null)
								{
									newSet = new();
									for (var j = 0; j < i; j++)
										newSet.Add(oleDbSet[j]);
								}

								newSet.Add(odbcSet[i]);
							}
							else if (newSet != null)
							{
								newSet.Add(oleDbSet[i]);
							}
						}

						if (newSet != null)
						{
							yield return new StoredProcedure(proc.Name, proc.Description, proc.Parameters, proc.SchemaError, new[] { newSet }, proc.Result);
							continue;
						}
					}
				}

				yield return proc;
			}
		}

		IEnumerable<Table> ISchemaProvider.GetTables()
		{
			var tablesWithCounters = _odbcSchema.GetTables().Where(t => t.Identity != null).ToDictionary(_ => _.Name);
			foreach (var table in _oleDbSchema.GetTables())
			{
				if (tablesWithCounters.TryGetValue(table.Name, out var odbcTable))
				{
					var identityColumn = odbcTable.Columns.Single(c => c.Name == odbcTable.Identity!.Column);
					var newColumns = new List<Column>();
					foreach (var column in table.Columns)
					{
						if (column.Name == odbcTable.Identity!.Column)
							newColumns.Add(identityColumn);
						else
							newColumns.Add(column);
					}

					yield return new Table(table.Name, table.Description, newColumns, odbcTable.Identity, table.PrimaryKey);
					continue;
				}

				yield return table;
			}
		}

		IEnumerable<View> ISchemaProvider.GetViews()
		{
			var viewsWithCounters = _odbcSchema.GetViews().Where(t => t.Identity != null).ToDictionary(_ => _.Name);
			foreach (var view in _oleDbSchema.GetViews())
			{
				if (viewsWithCounters.TryGetValue(view.Name, out var odbcView))
				{
					var identityColumn = odbcView.Columns.Single(c => c.Name == odbcView.Identity!.Column);
					var newColumns = new List<Column>();
					foreach (var column in view.Columns)
					{
						if (column.Name == odbcView.Identity!.Column)
							newColumns.Add(identityColumn);
						else
							newColumns.Add(column);
					}

					yield return new View(view.Name, view.Description, newColumns, odbcView.Identity, view.PrimaryKey);
					continue;
				}

				yield return view;
			}
		}
	}
}
