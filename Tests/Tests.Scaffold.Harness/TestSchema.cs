using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Predefined database schema, fed to the scaffolder instead of a schema read from a database.
	/// </summary>
	public sealed class TestSchema(DatabaseOptions? databaseOptions = null, params string[] defaultSchemas) : ISchemaProvider
	{
		public List<Table>             Tables             { get; } = [];
		public List<View>              Views              { get; } = [];
		public List<ForeignKey>        ForeignKeys        { get; } = [];
		public List<StoredProcedure>   Procedures         { get; } = [];
		public List<TableFunction>     TableFunctions     { get; } = [];
		public List<ScalarFunction>    ScalarFunctions    { get; } = [];
		public List<AggregateFunction> AggregateFunctions { get; } = [];

		public ISet<string> DefaultSchemas { get; } = new HashSet<string>(defaultSchemas, StringComparer.Ordinal);

		public DatabaseOptions DatabaseOptions { get; } = databaseOptions ?? DatabaseOptions.Default;

		/// <summary>
		/// Creates column definition. Ordinal is assigned by <c>AddTable</c> from column position when not specified.
		/// </summary>
		public static Column Col(string name, string type, bool nullable = false, int? length = null, int? precision = null, int? scale = null, bool insertable = true, bool updatable = true, int? ordinal = null)
			=> new(name, null, new DatabaseType(type, length, precision, scale), nullable, insertable, updatable, ordinal);

		public static Parameter Param(string name, string type, ParameterDirection direction = ParameterDirection.Input, bool nullable = true, int? length = null, int? precision = null, int? scale = null)
			=> new(name, null, new DatabaseType(type, length, precision, scale), nullable, direction);

		public static ResultColumn ResultCol(string? name, string type, bool nullable = true, int? length = null)
			=> new(name, new DatabaseType(type, length, null, null), nullable);

		public Table AddTable(SqlObjectName name, IReadOnlyList<Column> columns, string[]? primaryKey = null, string? identity = null, SqlObjectName? identitySequence = null)
		{
			var table = new Table(
				name,
				null,
				WithOrdinals(columns),
				identity == null ? null : new Identity(identity, identitySequence == null ? null : new Sequence(identitySequence)),
				primaryKey == null ? null : new PrimaryKey(null, primaryKey));
			Tables.Add(table);
			return table;
		}

		public Table AddTable(string name, IReadOnlyList<Column> columns, string[]? primaryKey = null, string? identity = null)
			=> AddTable(new SqlObjectName(name), columns, primaryKey, identity);

		public View AddView(SqlObjectName name, IReadOnlyList<Column> columns)
		{
			var view = new View(name, null, WithOrdinals(columns), null, null);
			Views.Add(view);
			return view;
		}

		public ForeignKey AddForeignKey(string name, SqlObjectName source, SqlObjectName target, params (string Source, string Target)[] columns)
		{
			var fk = new ForeignKey(name, source, target, columns.Select(static c => new ForeignKeyColumnMapping(c.Source, c.Target)).ToArray());
			ForeignKeys.Add(fk);
			return fk;
		}

		static Column[] WithOrdinals(IReadOnlyList<Column> columns)
			=> columns.Select(static (c, i) => c.Ordinal == null ? c with { Ordinal = i } : c).ToArray();

		IEnumerable<Table>             ISchemaProvider.GetTables()                                         => Tables;
		IEnumerable<View>              ISchemaProvider.GetViews()                                          => Views;
		IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys()                                    => ForeignKeys;
		IEnumerable<StoredProcedure>   ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly) => Procedures;
		IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions()                                 => TableFunctions;
		IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions()                                => ScalarFunctions;
		IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions()                             => AggregateFunctions;
		ISet<string>                   ISchemaProvider.GetDefaultSchemas()                                 => DefaultSchemas;

		string? ISchemaProvider.DatabaseName  => null;
		string? ISchemaProvider.ServerVersion => null;
		string? ISchemaProvider.DataSource    => null;
	}
}
