using System;
using System.Collections.Generic;

namespace LinqToDB.LINQPad.UI;

internal sealed class SchemaModel : OptionalTabModelBase
{
	public SchemaModel(ConnectionSettings settings, bool enabled)
		: base(settings, enabled)
	{
		Schemas = new UniqueStringListModel()
		{
			Title   = "Include/Exclude Schemas (Users)",
			ToolTip = "Include or exclude objects from specified schemas",
			Include = Settings.Schema.IncludeSchemas,
		};

		if (Settings.Schema.Schemas != null)
			foreach (var schema in Settings.Schema.Schemas)
				Schemas.Items.Add(schema);

		Catalogs = new UniqueStringListModel()
		{
			Title   = "Include/Exclude Catalogs (Databases)",
			ToolTip = "Include or exclude objects from specified catalogs",
			Include = Settings.Schema.IncludeSchemas,
		};

		if (Settings.Schema.Catalogs != null)
			foreach (var catalog in Settings.Schema.Catalogs)
				Catalogs.Items.Add(catalog);
	}

	public UniqueStringListModel Schemas  { get; }
	public UniqueStringListModel Catalogs { get; }

	public bool LoadForeignKeys
	{
		get => Settings.Schema.LoadForeignKeys;
		set => Settings.Schema.LoadForeignKeys = value;
	}

	public bool LoadProcedures
	{
		get => Settings.Schema.LoadProcedures;
		set => Settings.Schema.LoadProcedures = value;
	}

	public bool LoadTableFunctions
	{
		get => Settings.Schema.LoadTableFunctions;
		set => Settings.Schema.LoadTableFunctions = value;
	}

	public bool LoadScalarFunctions
	{
		get => Settings.Schema.LoadScalarFunctions;
		set => Settings.Schema.LoadScalarFunctions = value;
	}

	public bool LoadAggregateFunctions
	{
		get => Settings.Schema.LoadAggregateFunctions;
		set => Settings.Schema.LoadAggregateFunctions = value;
	}

	public void Save()
	{
		Settings.Schema.IncludeSchemas  = Schemas .Include;
		Settings.Schema.IncludeCatalogs = Catalogs.Include;

		Settings.Schema.Schemas  = Schemas .Items.Count == 0 ? null : new HashSet<string>(Schemas .Items, StringComparer.Ordinal).AsReadOnly();
		Settings.Schema.Catalogs = Catalogs.Items.Count == 0 ? null : new HashSet<string>(Catalogs.Items, StringComparer.Ordinal).AsReadOnly();
	}
}
