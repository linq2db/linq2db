using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

#pragma warning disable CA1861
#pragma warning disable RS0030
#pragma warning disable CA1305
#pragma warning disable CA1863

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public Action BeforeGenerateLinqToDBModel { get; set; } = () => {};
		public Action AfterGenerateLinqToDBModel  { get; set; } = () => {};

		public Func<ITable,MemberBase?> GenerateProviderSpecificTable { get; set; } = _ => null;
		public Func<Parameter,bool>     GenerateProcedureDbType       { get; set; } = _ => false;

		public bool   GenerateDataOptionsConstructors     { get; set; } = true;
		public bool   GenerateObsoleteAttributeForAliases { get; set; }
		public bool   GenerateFindExtensions              { get; set; } = true;
		public bool   IsCompactColumns                    { get; set; } = true;
		public bool   IsCompactColumnAliases              { get; set; } = true;
		public bool   GenerateDataTypes                   { get; set; }
		public bool?  GenerateLengthProperty              { get; set; }
		public bool?  GeneratePrecisionProperty           { get; set; }
		public bool?  GenerateScaleProperty               { get; set; }
		public bool   GenerateDbTypes                     { get; set; }
		public bool   GenerateSchemaAsType                { get; set; }
		public bool   GenerateViews                       { get; set; } = true;
		public bool   GenerateProcedureResultAsList       { get; set; }
		public bool   GenerateProceduresOnTypedContext    { get; set; } = true;
		public bool   PrefixTableMappingWithSchema        { get; set; } = true;
		public bool   PrefixTableMappingForDefaultSchema  { get; set; }
		public string SchemaNameSuffix                    { get; set; } = "Schema";
		public string SchemaDataContextTypeName           { get; set; } = "DataContext";
		public bool   GenerateNameOf                      { get; set; } = true;

		public Dictionary<string,string> SchemaNameMapping = new();

		public Func<string?,string,Func<IMethod>,IEnumerable<IMethod>> GetConstructors;

		public Func<IColumn,string,string,bool,string> BuildColumnComparison = (c, padding1, padding2, last) => $"\tt.{c.MemberName}{padding1} == {c.MemberName}{(last ? "" : padding2)}{(last ? ");" : " &&")}";

		IEnumerable<IMethod> GetConstructorsImpl(string? defaultConfiguration, string name, Func<IMethod> methodFactory)
		{
			if (defaultConfiguration == null)
			{
				var m = methodFactory();

				m.Name = name;

				if (!string.IsNullOrEmpty(GetDataOptionsMethod))
					m.AfterSignature.Add(": base(" + string.Format(CultureInfo.InvariantCulture, GetDataOptionsMethod, "") + ")");

				yield return m;
			}
			else
			{
				var m = methodFactory();

				m.Name = name;
				m.AfterSignature.Add(
					string.IsNullOrEmpty(GetDataOptionsMethod)
						? $": base(\"{defaultConfiguration}\")"
						: ": base(" + string.Format(CultureInfo.InvariantCulture, GetDataOptionsMethod, $"\"{defaultConfiguration}\"") + ")");

				yield return m;
			}

			{
				var m = methodFactory();

				m.Name = name;
				m.ParameterBuilders.Add(() => "string configuration");
				m.AfterSignature.   Add(
					string.IsNullOrEmpty(GetDataOptionsMethod)
						? ": base(configuration)"
						: ": base(" + string.Format(CultureInfo.InvariantCulture, GetDataOptionsMethod, "configuration") + ")");

				yield return m;

				if (GenerateDataOptionsConstructors)
				{
					m = methodFactory();

					m.Name = name;
					m.ParameterBuilders.Add(() => "DataOptions options");
					m.AfterSignature.   Add(": base(options)");

					yield return m;

					m = methodFactory();

					m.Name = name;
					m.ParameterBuilders.Add(() => $"DataOptions<{name}> options");
					m.AfterSignature.   Add(": base(options.Options)");

					yield return m;
				}
			}
		}


		protected void MakeTypeMembersNamesUnique(IClass type, string defaultName = "Member", params string[] exceptMethods)
		{
			var reservedNames = new [] { type.Name };

			if (exceptMethods is [_, ..])
				reservedNames = reservedNames.Concat(exceptMethods).ToArray();

			MakeMembersNamesUnique(GetAllClassMembers(type.Members, exceptMethods), defaultName, reservedNames!);
		}

		protected List<string> CreateXmlCommentFromText(string? text, string tag = "summary", string? attributes = null)
		{
			var comments = new List<string>();

			if (!string.IsNullOrWhiteSpace(text))
			{
				comments.Add($"/ <{tag}{(attributes == null ? null : " " + attributes)}>");

				foreach (var line in text!.Split('\n'))
					comments.Add("/ " + line
						.Replace("&",  "&amp;")
						.Replace("<",  "&lt;")
						.Replace(">",  "&gt;")
						.Replace("\"", "&quot;")
						.Replace("'",  "&apos;")
						.TrimEnd());

				comments.Add($"/ </{tag}>");
			}

			return comments;
		}

		protected void MakeMembersNamesUnique(IEnumerable<IClassMember> members, string defaultName, params string[] reservedNames)
		{
			Common.Utils.MakeUniqueNames(
				members,
				reservedNames,
				m => m is ITable t ? (t.Schema != null && (PrefixTableMappingForDefaultSchema || !t.IsDefaultSchema) && PrefixTableMappingWithSchema ? t.Schema + "_" : null) + t.Name : m is TypeBase tb ? tb.Name : ((MemberBase)m).Name,
				(m, newName, _) =>
				{
					if (m is TypeBase tb)
						tb.Name = newName;
					else
						((MemberBase)m).Name = newName;
				},
				defaultName);
		}

		IEnumerable<IClassMember> GetAllClassMembers(IEnumerable<IClassMember> members, params string[] exceptMethods)
		{
			foreach (var member in members)
			{
				if (member is IMemberGroup mm)
					foreach (var m in GetAllClassMembers(mm.Members, exceptMethods!))
						yield return m;
				// constructors don't have own type/flag
				else if (!(member is IMethod mt && (mt.BuildType() == null || exceptMethods != null && exceptMethods.Contains(mt.Name))))
					yield return member;
			}
		}

		// unused: left for backward API compatibility
		//
		public string NormalizeStringName(string name)
		{
			return ToStringLiteral(name);
		}
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator<TTable,TProcedure>
		where TTable     : class, ITable,      new()
		where TProcedure : IProcedure<TTable>, new()
	{
		public void GenerateTypesFromMetadata<TMemberGroup,TClass,TAttribute,TMethod,TProperty,TField>()
			where TMemberGroup : MemberGroup<TMemberGroup>, new()
			where TClass       : Class      <TClass>,       new()
			where TAttribute   : Attribute  <TAttribute>,   new()
			where TMethod      : Method     <TMethod>,      new()
			where TProperty    : Property   <TProperty>,    new()
			where TField       : Field      <TField>,       new()
		{
			BeforeGenerateLinqToDBModel();

			Model.Usings.Add("LinqToDB");
			Model.Usings.Add("LinqToDB.Mapping");
			Model.Usings.Add("LinqToDB.Configuration");

			Model.Namespace.Name ??= "DataModel";

			var schemas =
			(
				from t in Tables.Values
				where GenerateSchemaAsType && t.Schema != null && !t.TableSchema!.IsDefaultSchema
				group t by t.Schema into gr
				orderby gr.Key
				let typeName = SchemaNameMapping.TryGetValue(gr.Key, out var schemaName) ? schemaName : gr.Key
				select new
				{
					Name            = gr.Key,
					TypeName        = typeName + SchemaNameSuffix,
					PropertyName    = typeName,
					Props           = new TMemberGroup { IsCompact = true },
					Aliases         = new TMemberGroup { IsCompact = true, Region = "Alias members" },
					TableExtensions = new TMemberGroup { Region = "Table Extensions" },
					Type            = new TClass { Name = typeName + SchemaNameSuffix, IsStatic = true },
					Tables          = gr.ToList(),
					DataContext     = new TClass { Name = SchemaDataContextTypeName },
					Procedures      = new TMemberGroup(),
					Functions       = new TMemberGroup(),
					TableFunctions  = new TMemberGroup { Region = "Table Functions" },
				}
			).ToDictionary(t => t.Name);

			var procSchemas =
			(
				from p in Procedures.Values
				where GenerateSchemaAsType && p.Schema != null && !p.IsDefaultSchema && !schemas.ContainsKey(p.Schema)
				group p by p.Schema into gr
				orderby gr.Key
				let typeName = SchemaNameMapping.TryGetValue(gr.Key, out var schemaName) ? schemaName : gr.Key
				select new
				{
					Name            = gr.Key,
					TypeName        = typeName + SchemaNameSuffix,
					PropertyName    = typeName,
					Props           = new TMemberGroup { IsCompact = true },
					Aliases         = new TMemberGroup { IsCompact = true, Region = "Alias members" },
					TableExtensions = new TMemberGroup { Region = "Table Extensions" },
					Type            = new TClass { Name = typeName + SchemaNameSuffix, IsStatic = true },
					Tables          = new List<TTable>(),
					DataContext     = new TClass { Name = SchemaDataContextTypeName },
					Procedures      = new TMemberGroup(),
					Functions       = new TMemberGroup(),
					TableFunctions  = new TMemberGroup { Region = "Table Functions" },
				}
			)
			.ToDictionary(s => s.Name);

			foreach(var schema in procSchemas)
				schemas.Add(schema.Key, schema.Value);

			var defProps           = new TMemberGroup { IsCompact = true };
			var defAliases         = new TMemberGroup { IsCompact = true, Region = "Alias members" };
			var defTableExtensions = new TMemberGroup();

			if (schemas.Count > 0)
			{
				var body = new List<Func<IEnumerable<string>>>();

				var schemaGroup   = new TMemberGroup { Region = "Schemas" };
				var schemaMembers = new TMemberGroup { IsCompact = true   };

				var maxLen1 = schemas.Values.Max(schema => schema.PropertyName.Trim().Length);
				var maxLen2 = schemas.Values.Max(schema => schema.TypeName.    Trim().Length);

				foreach (var schema in schemas.Values)
				{
					schemaMembers.Members.Add(new TProperty { EnforceNotNullable = EnableNullableReferenceTypes, TypeBuilder = () => schema.TypeName + "." + SchemaDataContextTypeName, Name = schema.PropertyName });
					body.Add(() => new [] { $"{schema.PropertyName}{LenDiff(maxLen1, schema.PropertyName)} = new {schema.TypeName}.{LenDiff(maxLen2, schema.TypeName)}{SchemaDataContextTypeName}(this);" });
				}

				schemaGroup.Members.Add(schemaMembers);
				schemaGroup.Members.Add(new TMethod { TypeBuilder = () => "void", Name = "InitSchemas", BodyBuilders = [ ..body ] });

				DataContextObject?.Members.Add(schemaGroup);
			}

			if (GenerateConstructors)
			{
				foreach (var c in GetConstructors(DefaultConfiguration!, DataContextObject!.Name!, () => new TMethod()))
				{
					if (c.BodyBuilders.Count > 0)
						c.BodyBuilders.Add(() => [""]);

					if (schemas.Count > 0)
						c.BodyBuilders.Add(() => ["InitSchemas();"]);

					c.BodyBuilders.Add(() => ["InitDataContext();", "InitMappingSchema();"]);

					DataContextObject?.Members.Add(c);
				}

				DataContextObject?.Members.Add(new TMemberGroup
				{
					IsCompact = true,
					Members   =
					{
						new TMethod { TypeBuilder = () => "void", Name = "InitDataContext",   AccessModifier = AccessModifier.Partial },
						new TMethod { TypeBuilder = () => "void", Name = "InitMappingSchema", AccessModifier = AccessModifier.Partial }
					}
				});
			}

			if (Tables.Count > 0)
				DataContextObject?.Members.Insert(0, defProps);

			foreach (var schema in schemas.Values)
			{
				schema.Type.Members.Add(schema.DataContext);
				schema.DataContext.Members.Insert(0, schema.Props);

				schema.DataContext.Members.Add(new TField  { TypeBuilder = () => "IDataContext", Name = "_dataContext", AccessModifier = AccessModifier.Private, IsReadonly = true });
				schema.DataContext.Members.Add(new TMethod
				{
					TypeBuilder       = () => null,
					Name              = schema.DataContext.Name,
					ParameterBuilders = { () => "IDataContext dataContext" },
					BodyBuilders      = { () => ["_dataContext = dataContext;"] }
				});

				foreach (var t in schema.Tables)
					t.TypePrefix = $"{schema.TypeName}.";
			}

			var associationExtensions = new TMemberGroup { Region = "Associations" };

			foreach (var t in Tables.Values.OrderBy(tbl => tbl.IsProviderSpecific).ThenBy(tbl => tbl.TypeName))
			{
				var addType         = Model.Types.Add;
				var props           = defProps;
				var aliases         = defAliases;
				var tableExtensions = defTableExtensions;

				if (t.IsView && !GenerateViews)
					continue;

				var schema = t.Schema != null && schemas.TryGetValue(t.Schema ?? "", out var s) ? s : null;

				if (schema != null)
				{
					var si = schemas[t.Schema ?? ""];

					addType         = si.Type.Members.Add;
					props           = si.Props;
					aliases         = si.Aliases;
					tableExtensions = si.TableExtensions;
				}

				var dcProp = t.IsProviderSpecific ?
					GenerateProviderSpecificTable(t) :
					new TProperty
					{
						TypeBuilder     = () => $"ITable<{t.TypeName}>",
						Name            = t.DataContextPropertyName,
						GetBodyBuilders = { () => [ string.Format(CultureInfo.InvariantCulture, (schema == null ? "this" : "_dataContext") + ".GetTable<{0}>()", t.TypeName) ] },
						IsAuto          = false,
						HasGetter       = true,
						HasSetter       = false
					};

				if (dcProp == null) continue;

				t.DataContextProperty = dcProp;

				props.Members.Add(dcProp);

				TProperty? aProp = null;

				if (t.AliasPropertyName != null && t.AliasPropertyName != t.DataContextPropertyName)
				{
					aProp = new TProperty
					{
						TypeBuilder     = () => $"ITable<{t.TypeName}>",
						Name            = t.AliasPropertyName,
						GetBodyBuilders = { () => new[] { t.DataContextPropertyName! } },
						IsAuto          = false,
						HasGetter       = true,
						HasSetter       = false
					};

					if (GenerateObsoleteAttributeForAliases)
						aProp.Attributes.Add(new TAttribute { Name = "Obsolete", Parameters = { ToStringLiteral($"Use {t.DataContextPropertyName} instead.") } });

					aliases.Members.Add(aProp);
				}

				var tableAttrs = new List<string>();

				if (GenerateDatabaseNameFromTable && t.TableSchema?.CatalogName != null)
					tableAttrs.Add("Database=" + ToStringLiteral(t.TableSchema.CatalogName));
				else if (DatabaseName != null)
					tableAttrs.Add("Database=" + ToStringLiteral(DatabaseName));

				if (ServerName != null) tableAttrs.Add("Server=" + ToStringLiteral(ServerName));
				if (t.Schema   != null) tableAttrs.Add("Schema=" + ToStringLiteral(t.TableSchema?.SchemaName ?? t.Schema));

				tableAttrs.Add((tableAttrs.Count == 0 ? "" : "Name=") + ToStringLiteral(t.TableName));

				if (t.IsView)
					tableAttrs.Add("IsView=true");

				t.Attributes.Add(new TAttribute { Name = "Table", Parameters = [..tableAttrs], IsSeparated = true });

				var comments = CreateXmlCommentFromText(t.Description);

				if (comments.Count > 0)
				{
					t.     Comment.AddRange(comments);
					dcProp.Comment.AddRange(comments);

					aProp?.Comment.AddRange(comments);
				}

				var columns        = new TMemberGroup { IsCompact = IsCompactColumns };
				var columnAliases  = new TMemberGroup { IsCompact = IsCompactColumnAliases, Region = "Alias members" };
				var nPKs           = t.Columns.Values.Count(c => c.IsPrimaryKey);
				var allNullable    = t.Columns.Values.All  (c => c.IsNullable || c.IsIdentity);
				var nameMaxLen     = t.Columns.Values.Max  (c => (int?)(c.MemberName == c.ColumnName
					? 0
					: ToStringLiteral(c.ColumnName).Length)) ?? 0;
				var dbTypeMaxLen   = t.Columns.Values.Max  (c => (int?)(c.ColumnType?.Length)) ?? 0;
				var dataTypeMaxLen = t.Columns.Values.Where(c => c.DataType != null).Max  (c => (int?)(c.DataType?.Length)) ?? 0;
				var dataTypePrefix = "LinqToDB.";

				foreach (var c in t.Columns.Values)
				{
					// Column.
					//
					var ca = new TAttribute { Name = "Column" };
					var canBeReplaced = true;

					if (c.MemberName != c.ColumnName)
					{
						var columnNameInAttr = ToStringLiteral(c.ColumnName);

						var space = new string(' ', nameMaxLen - columnNameInAttr.Length);

						ca.Parameters.Add(columnNameInAttr + space);
						canBeReplaced = false;
					}
					else if (nameMaxLen > 0)
					{
						ca.Parameters.Add(new string(' ', nameMaxLen));
						canBeReplaced = false;
					}

					if (GenerateDbTypes)
					{
						var space = new string(' ', dbTypeMaxLen - (c.ColumnType?.Length ?? 0));

						ca.Parameters.Add($"DbType={ToStringLiteral(c.ColumnType)}{space}");
						canBeReplaced = false;
					}

					if (GenerateDataTypes)
					{
						var space = new string(' ', dataTypeMaxLen - (c.DataType?.Length ?? 0));
						ca.Parameters.Add($"DataType={dataTypePrefix}{c.DataType}{space}");
						canBeReplaced = false;
					}

					if (GenerateDataTypes && !GenerateLengthProperty.HasValue || GenerateLengthProperty == true)
					{
						if (c.Length != null)
							ca.Parameters.Add("Length=" + (c.Length == int.MaxValue ? "int.MaxValue" : c.Length?.ToString(CultureInfo.InvariantCulture)));
						canBeReplaced = false;
					}

					if (GenerateDataTypes && !GeneratePrecisionProperty.HasValue || GeneratePrecisionProperty == true)
					{
						if (c.Precision != null)
							ca.Parameters.Add(FormattableString.Invariant($"Precision={c.Precision}"));
						canBeReplaced = false;
					}

					if (GenerateDataTypes && !GenerateScaleProperty.HasValue || GenerateScaleProperty == true)
					{
						if (c.Scale != null)
							ca.Parameters.Add(FormattableString.Invariant($"Scale={c.Scale}"));
						canBeReplaced = false;
					}

					if (c.SkipOnInsert && !c.IsIdentity)
					{
						ca.Parameters.Add("SkipOnInsert=true");
						canBeReplaced = false;
					}

					if (c is { SkipOnUpdate: true, IsIdentity: false })
					{
						ca.Parameters.Add("SkipOnUpdate=true");
						canBeReplaced = false;
					}

					if (c.IsDiscriminator)
					{
						ca.Parameters.Add("IsDiscriminator=true");
						canBeReplaced = false;
					}

					c.Attributes.Insert(0, ca);

					// PK.
					//
					if (c.IsPrimaryKey)
					{
						var pka = new TAttribute { Name = "PrimaryKey" };

						if (nPKs > 1)
							pka.Parameters.Add(c.PrimaryKeyOrder.ToString(CultureInfo.InvariantCulture));

						if (canBeReplaced)
							c.Attributes.Remove(ca);

						c.Attributes.Add(pka);

						canBeReplaced = false;
					}

					// Identity.
					//
					if (c.IsIdentity)
					{
						var ida = new TAttribute { Name = "Identity" };

						if (canBeReplaced)
							c.Attributes.Remove(ca);

						c.Attributes.Add(ida);
					}

					// Nullable.
					//
					if (c.IsNullable)
						c.Attributes.Add(new TAttribute { Name = (allNullable ? "" : "   ") + "Nullable" });
					else if (!c.IsIdentity)
						c.Attributes.Add(new TAttribute { Name = "NotNull" });

					var columnComments = CreateXmlCommentFromText(c.Description);

					if (columnComments.Count > 0)
						c.Comment.AddRange(columnComments);

					// End line comment.
					//
					c.EndLineComment = c.ColumnType;

					SetPropertyValue(c, "IsNotifying", true);
					SetPropertyValue(c, "IsEditable",  true);

					columns.Members.Add(c);

					// Alias.
					//
					if (c.AliasName != null && c.AliasName != c.MemberName)
					{
						var caProp = new TProperty
						{
							TypeBuilder     = c.TypeBuilder,
							Name            = c.AliasName,
							GetBodyBuilders = { () => [c.MemberName!] },
							SetBodyBuilders = { () => new[] { $"{c.MemberName} = value;" } },
							IsAuto          = false,
							HasGetter       = true,
							HasSetter       = true
						};

						caProp.Comment.AddRange(columnComments);

						if (GenerateObsoleteAttributeForAliases)
							caProp.Attributes.Add(new TAttribute { Name = "Obsolete", Parameters = { ToStringLiteral($"Use {c.MemberName} instead.") } });

						caProp.Attributes.Add(new TAttribute { Name = "ColumnAlias" , Parameters = { ToStringLiteral(c.MemberName) } });

						columnAliases.Members.Add(caProp);
					}
				}

				t.Members.Add(columns);

				if (columnAliases.Members.Count > 0)
					t.Members.Add(columnAliases);

				if (GenerateAssociations || GenerateAssociationExtensions)
				{
					var keys = t.ForeignKeys.Values.ToList();

					if (!GenerateBackReferences)
						keys = keys.Where(k => k.BackReference != null).ToList();

					if (keys.Count > 0)
					{
						var associations          = new TMemberGroup { Region = "Associations" };
						var extensionAssociations = new TMemberGroup { Region = $"{t.Name} Associations" };

						foreach (var key in keys.OrderBy(k => k.MemberName))
						{
							string? otherTableName = null;

							if (key.OtherTable.TableSchema?.SchemaName != null)
								otherTableName += key.OtherTable.TableSchema.SchemaName + ".";

							otherTableName += key.OtherTable.TableSchema?.TableName;

							key.Comment.Add("/ <summary>");
							key.Comment.Add($"/ {key.KeyName} ({otherTableName})");
							key.Comment.Add("/ </summary>");

							if (key.AssociationType == AssociationType.OneToMany)
								key.TypeBuilder = () => string.Format(CultureInfo.InvariantCulture, OneToManyAssociationType, key.OtherTable?.TypePrefix + key.OtherTable!.TypeName);
							else
								key.TypeBuilder = () => new ModelType(key.OtherTable!.TypePrefix + key.OtherTable.TypeName, true, key.CanBeNull).ToTypeName();

							var aa = new TAttribute { Name = "Association", Tag = key};

							if (GenerateNameOf)
							{
								var thisSchema = "";

								if (key.ThisTable.Schema is not null && !key.ThisTable.IsDefaultSchema && schemas.ContainsKey(key.ThisTable.Schema))
									thisSchema = SchemaNameMapping.TryGetValue(key.ThisTable.Schema, out var sc) ? $"{sc}Schema." : t.Schema + ".";

								aa.Parameters.Add("ThisKey=" + string.Join(" + \", \" + ",
									key.ThisColumns
										.Select(c => GenerateAssociationExtensions
											? $"nameof({Model.Namespace.Name}.{thisSchema}{t.TypeName}.{c.MemberName})"
											: $"nameof({c.MemberName})")
										.ToArray()));

								var otherSchema = "";

								if (key.OtherTable.Schema is not null && !key.OtherTable.IsDefaultSchema && schemas.ContainsKey(key.OtherTable.Schema))
									otherSchema = SchemaNameMapping.TryGetValue(key.OtherTable.Schema, out var sc) ? $"{sc}Schema." : key.OtherTable.Schema + ".";

								aa.Parameters.Add("OtherKey=" + string.Join(" + \", \" + ",
									key.OtherColumns
										.Select(c => $"nameof({Model.Namespace.Name}.{otherSchema}{key.OtherTable?.TypeName}.{c.MemberName})")
										.ToArray()));
							}
							else
							{
								aa.Parameters.Add("ThisKey="   + ToStringLiteral(string.Join(", ", (from c in key.ThisColumns  select c.MemberName).ToArray())));
								aa.Parameters.Add("OtherKey="  + ToStringLiteral(string.Join(", ", (from c in key.OtherColumns select c.MemberName).ToArray())));
							}

							aa.Parameters.Add("CanBeNull=" + (key.CanBeNull ? "true" : "false"));

							key.Attributes.Add(aa);

							SetPropertyValue(key, "IsNotifying", true);
							SetPropertyValue(key, "IsEditable",  true);

							associations.Members.Add(key);

							var extension = new TMethod
							{
								TypeBuilder = () => $"IQueryable<{key.OtherTable!.TypePrefix}{key.OtherTable.TypeName}>",
								Name        = GetAssociationExtensionPluralName(key),
								IsStatic    = true
							};

							extension.ParameterBuilders.Add(() => $"this {t.TypePrefix}{t.TypeName} obj");

							extension.ParameterBuilders.Add(() => "IDataContext db");
							extension.Attributes.Add(aa);

							extension.Comment.Add("/ <summary>");
							extension.Comment.Add("/ " + key.KeyName);
							extension.Comment.Add("/ </summary>");

							string Builder()
							{
								var sb = new StringBuilder()
									.Append("return db.GetTable<")
									.Append(key.OtherTable?.TypePrefix + key.OtherTable!.TypeName)
									.Append(">().Where(c => ");

								for (var i = 0; i < key.OtherColumns.Count; i++)
								{
									sb.Append("c.")
										.Append(key.OtherColumns[i].MemberName)
										.Append(" == obj.")
										.Append(key.ThisColumns[i].MemberName)
										.Append(" && ");
								}

								sb.Length -= 4;
								sb.Append(");");

								return sb.ToString();
							}

							extension.BodyBuilders.Add(() => new[] { Builder() });

							extensionAssociations.Members.Add(extension);

							if (key.AssociationType != AssociationType.OneToMany)
							{
								var single = new TMethod { TypeBuilder = () => new ModelType(t.TypePrefix + t.TypeName, true, key.CanBeNull).ToTypeName(), Name = GetAssociationExtensionSingularName(key) };

								single.ParameterBuilders.Add(() => $"this {key.OtherTable!.TypePrefix}{key.OtherTable.TypeName} obj");

								single.ParameterBuilders.Add(() => "IDataContext db");
								single.Attributes.Add(aa);
								single.IsStatic = true;

								single.Comment.Add("/ <summary>");
								single.Comment.Add("/ " + key.KeyName);
								single.Comment.Add("/ </summary>");

								string BuilderSingle()
								{
									var sb = new StringBuilder()
										.Append("return db.GetTable<")
										.Append(t.TypePrefix + t.TypeName)
										.Append(">().Where(c => ");

									for (var i = 0; i < key.OtherColumns.Count; i++)
									{
										sb
											.Append("c.")
											.Append(key.ThisColumns[i].MemberName)
											.Append(" == obj.")
											.Append(key.OtherColumns[i].MemberName)
											.Append(" && ");
									}

									sb.Length -= 4;
									sb.Append(");");

									return sb.ToString();
								}

								single.BodyBuilders.Add(() =>
								{
									var sb = new StringBuilder(BuilderSingle());

									sb.Length -= 1;
									sb.Append(key.CanBeNull ? ".FirstOrDefault();" : ".First();");

									return new [] { sb.ToString() };
								});

								extensionAssociations.Members.Add(single);
							}
						}

						if (GenerateAssociations)
							t.Members.Add(associations);

						if (GenerateAssociationExtensions)
							associationExtensions.Members.Add(extensionAssociations);
					}
				}

				if (GenerateFindExtensions && nPKs > 0)
				{
					var PKs         = t.Columns.Values.Where(c => c.IsPrimaryKey).ToList()!;
					var maxNameLen1 = PKs.Max(c => (int?)c.MemberName?.Length) ?? 0;
					var maxNameLen2 = PKs.Take(nPKs - 1).Max(c => (int?)c.MemberName?.Length) ?? 0;

					tableExtensions.Members.Add(new TMethod
					{
						TypeBuilder       = () => new ModelType(t.TypeName!, true, true).ToTypeName(),
						Name              = "Find",
						ParameterBuilders =
						[
							() => $"this ITable<{t.TypeName}> table",
							..PKs.Select<IColumn,Func<string>>(c => () => $"{c.BuildType()} {c.MemberName}")
						],
						BodyBuilders      =
						{
							() => new[] { "return table.FirstOrDefault(t =>" }
								.Union(PKs.SelectMany((c, i) =>
								{
									var ss = new List<string>();

									if (c.Conditional != null)
										ss.Add("#if " + c.Conditional);

									ss.Add(BuildColumnComparison(c, LenDiff(maxNameLen1, c.MemberName!), LenDiff(maxNameLen2, c.MemberName!), i == nPKs - 1));

									if (c.Conditional != null)
									{
										if (ss[1].EndsWith(");"))
										{
											ss[1] = ss[1][..^2];
											ss.Add("#endif");
											ss.Add("\t\t);");
										}
										else
										{
											ss.Add("#endif");
										}
									}

									return ss;
								}))
						},
						IsStatic = true
					});
				}

				addType(t);

				if (!string.IsNullOrWhiteSpace(t.AliasTypeName))
				{
					var aClass = new TClass { Name = t.AliasTypeName, BaseClass = t.TypeName };

					if (comments.Count > 0)
						aClass.Comment.AddRange(comments);

					if (GenerateObsoleteAttributeForAliases)
						aClass.Attributes.Add(new TAttribute { Name = "Obsolete", Parameters = { ToStringLiteral("Use " + t.TypeName + " instead.") } });

					Model.Types.Add(aClass);
				}
			}

			if (associationExtensions.Members.Count > 0)
				defTableExtensions.Members.Add(associationExtensions);

			if (defAliases.Members.Count > 0)
				DataContextObject?.Members.Add(defAliases);

			foreach (var schema in schemas.Values)
				if (schema.Aliases.Members.Count > 0)
					schema.Type.Members.Add(defAliases);

			if (Procedures.Count > 0)
			{
				Model.Usings.Add("System.Collections.Generic");
				Model.Usings.Add("System.Data");
				Model.Usings.Add("LinqToDB.Data");
				Model.Usings.Add("LinqToDB.Common");

				if (Procedures.Values.Any(p => p.IsTableFunction))
					Model.Usings.Add("System.Reflection");

				if (Procedures.Values.Any(p => p.IsAggregateFunction))
					Model.Usings.Add("System.Linq.Expressions");

				var procs = new TMemberGroup();
				var funcs = new TMemberGroup();
				var tabfs = new TMemberGroup { Region = "Table Functions" };

				var currentContext = DataContextObject;

				foreach (var p in Procedures.Values
					.Where(proc =>
						proc.IsLoaded ||
						proc is { IsFunction     : true, IsTableFunction: false    } ||
						proc is { IsTableFunction: true, ResultException: not null })
					.OrderBy(proc => proc.Name))
				{
					Action<IMemberGroup> addProcs = procs.Members.Add;
					Action<IMemberGroup> addFuncs = funcs.Members.Add;
					Action<IMemberGroup> addTabfs = tabfs.Members.Add;

					var thisDataContext = "this";

					var schema = p.Schema != null && schemas.TryGetValue(p.Schema, out var s) ? s : null;

					if (schema != null)
					{
						var si = schemas[p.Schema!];

						addProcs        = si.Procedures.    Members.Add;
						addFuncs        = si.Functions.     Members.Add;
						addTabfs        = si.TableFunctions.Members.Add;
						thisDataContext = "_dataContext";
					}

					var proc = new TMemberGroup { Region = p.Name };

					if      (!p.IsFunction)     addProcs(proc);
					else if (p.IsTableFunction) addTabfs(proc);
					else                        addFuncs(proc);

					if (p.ResultException != null)
					{
						proc.Errors.Add(p.ResultException.Message);
						continue;
					}

					var comments  = CreateXmlCommentFromText(p.Description);

					List<string>? returnsComments = null;

					foreach (var param in p.ProcParameters)
					{
						if (param.IsResult && p.IsFunction)
							returnsComments = CreateXmlCommentFromText(param.Description, "returns");
						else
							comments.AddRange(CreateXmlCommentFromText(param.Description, "param", $"name=\"{(param.ParameterName!.StartsWith("@") ? param.ParameterName?[1..] : param.ParameterName)}\""));
					}

					if (returnsComments != null)
						comments.AddRange(returnsComments);

					if (comments.Count > 0)
						p.Comment.AddRange(comments);

					proc.Members.Add(p);

					if (p.IsTableFunction)
					{
						var tableAttrs = new List<string>();

						if (ServerName    != null) tableAttrs.Add("Server="   + ToStringLiteral(ServerName));
						if (DatabaseName  != null) tableAttrs.Add("Database=" + ToStringLiteral(DatabaseName));
						if (p.Schema      != null) tableAttrs.Add("Schema="   + ToStringLiteral(p.Schema));
						if (p.PackageName != null) tableAttrs.Add("Package="  + ToStringLiteral(p.PackageName));

						tableAttrs.Add($"Name={ToStringLiteral(p.ProcedureName)}");

						p.Attributes.Add(new TAttribute { Name = "Sql.TableFunction", Parameters = [ ..tableAttrs ] });

						p.TypeBuilder = () => $"ITable<{p.ResultTable?.TypeName}>";
					}
					else if (p.IsAggregateFunction)
					{
						p.IsStatic    = true;
						p.TypeBuilder = () =>
						{
							var resultParam = p.ProcParameters.Single(pr => pr.IsResult);
							return resultParam.Type.ToTypeName();
						};

						var paramCount   = p.ProcParameters.Count(pr => !pr.IsResult);
						var functionName = SqlBuilder?.BuildObjectName(new StringBuilder(), new SqlObjectName(p.ProcedureName!, Schema: p.Schema, Package: p.PackageName), ConvertType.NameToProcedure).ToString();

						p.Attributes.Add(new TAttribute
						{
							Name       = "Sql.Function",
							Parameters =
							{
								"Name=" + ToStringLiteral(functionName),
								"ServerSideOnly=true, IsAggregate = true" + (paramCount > 0 ? ", ArgIndices = new[] { " + string.Join(", ", Enumerable.Range(0, p.ProcParameters.Count(pr => !pr.IsResult))) + " }" : null)
							}
						});

						if (p.IsDefaultSchema || !GenerateSchemaAsType)
							p.ParameterBuilders.Add(() => "this IEnumerable<TSource> src");
						else // otherwise function will be generated in nested class, which doesn't support extension methods
							p.ParameterBuilders.Add(() => "IEnumerable<TSource> src");

						foreach (var inp in p.ProcParameters.Where(pr => !pr.IsResult))
							p.ParameterBuilders.Add(() => $"Expression<Func<TSource, {inp.Type.ToTypeName()}>> {inp.ParameterName}");

						p.Name += "<TSource>";
					}
					else if (p.IsFunction)
					{
						p.IsStatic       = true;
						p.TypeBuilder    = () => p.ProcParameters.Single(pr => pr.IsResult).Type.ToTypeName();

						var functionName = SqlBuilder?.BuildObjectName(new StringBuilder(), new SqlObjectName(p.ProcedureName!, Schema: p.Schema, Package: p.PackageName), ConvertType.NameToProcedure).ToString();

						p.Attributes.Add(new TAttribute { Name = "Sql.Function", Parameters = { "Name=" + ToStringLiteral(functionName), "ServerSideOnly=true" } });
					}
					else
					{
						p.IsStatic    = true;
						p.TypeBuilder = () => p.ResultTable == null
							? "int"
							: GenerateProcedureResultAsList
								? "List<" + p.ResultTable.TypeName + ">"
								: "IEnumerable<" + p.ResultTable.TypeName + ">";

						if (p.IsDefaultSchema || !GenerateSchemaAsType)
							p.ParameterBuilders.Add(() => $"this {(GenerateProceduresOnTypedContext ? currentContext?.Name : "DataConnection")} dataConnection");
						else
							p.ParameterBuilders.Add(() => $"{(GenerateProceduresOnTypedContext ? currentContext?.Name : "DataConnection")} dataConnection");
					}

					if (!p.IsAggregateFunction)
						foreach (var pr in p.ProcParameters.Where(par => !par.IsResult || !p.IsFunction))
							p.ParameterBuilders.Add(() => $"{(pr.IsOut || pr.IsResult ? pr.IsIn ? "ref " : "out " : "")}{pr.Type.ToTypeName()} {pr.ParameterName}");

					if (p.IsTableFunction)
					{
						p.BodyBuilders.Add(() => new[]
						{
//							$"return {thisDataContext}.GetTable<{p.ResultTable!.TypeName}>(this, (MethodInfo)MethodBase.GetCurrentMethod(){(EnableNullableReferenceTypes ? "!" : "")}{(p.ProcParameters?.Count == 0 ? ");" : ",")}",
							$"return {thisDataContext}.TableFromExpression(() => {p.Name}({string.Join(", ", p.ProcParameters.Select(par => par.ParameterName))}));"
						});

//						for (var idx = 0; idx < p.ProcParameters.Count; idx++)
//						{
//							var i = idx;
//							p.BodyBuilders.Add(() => new []{ "\t" + p.ProcParameters[i].ParameterName + (i + 1 == p.ProcParameters.Count ? ");" : ",") });
//						}
					}
					else if (p.IsFunction)
					{
						p.BodyBuilders.Add(() => ["throw new InvalidOperationException();"]);
					}
					else
					{
						var spName =
							SqlBuilder?.BuildObjectName(
								new StringBuilder(),
								new SqlObjectName(p.ProcedureName!, Server: ServerName, Database: DatabaseName, Schema: p.Schema, Package: p.PackageName),
								ConvertType.NameToProcedure
							).ToString();

						spName = ToStringLiteral(spName);

						var inputParameters      = p.ProcParameters.Where(pp => pp.IsIn).                            ToList();
						var outputParameters     = p.ProcParameters.Where(pp => pp.IsOut || pp.IsResult).            ToList();
						var inOrOutputParameters = p.ProcParameters.Where(pp => pp.IsIn  || pp.IsOut || pp.IsResult).ToList();

						var retName = "ret";
						var retNo   = 0;

						while (p.ProcParameters.Any(pp => pp.ParameterName == retName))
							retName = FormattableString.Invariant($"ret{++retNo}");

						var hasOut = outputParameters.Any(pr => pr.IsOut || pr.IsResult);
						var prefix = hasOut ? "var " + retName + " = " : "return ";

						var cnt = 0;
						var parametersVarName = "parameters";
						while (p.ProcParameters.Where(par => !par.IsResult || !p.IsFunction).Any(par => par.ParameterName == parametersVarName))
							parametersVarName = FormattableString.Invariant($"parameters{cnt++}");

						var maxLenSchema = inputParameters.Max(pr => (int?)pr.SchemaName?.   Length) ?? 0;
						var maxLenParam  = inputParameters.Max(pr => (int?)pr.ParameterName?.Length) ?? 0;
						var maxLenType   = inputParameters.Max(pr => (int?)("LinqToDB.DataType." + pr.DataType).Length) ?? 0;

						if (inOrOutputParameters.Count > 0)
						{
							p.BodyBuilders.Add(() =>
							{
								var code = new List<string>
								{
									$"var {parametersVarName} = new []",
									"{"
								};

								for (var i = 0; i < inOrOutputParameters.Count; i++)
								{
									var pr            = inOrOutputParameters[i];
									var hasInputValue = pr.IsIn || (pr.IsOut && pr.IsResult);

									var extraInitializers = new List<Tuple<string, string>>();
									if (GenerateProcedureDbType(pr))
										extraInitializers.Add(Tuple.Create("DbType", ToStringLiteral(pr.SchemaType)));

									if (pr.IsOut || pr.IsResult)
										extraInitializers.Add(Tuple.Create("Direction", pr.IsIn ? "ParameterDirection.InputOutput" : (pr.IsResult ? "ParameterDirection.ReturnValue" : "ParameterDirection.Output")));

									var size = pr.Size != null && pr.Size.Value != 0 /*&& pr.Size.Value is >= int.MinValue and <= int.MaxValue*/
										? ", " + pr.Size.Value!.ToString(CultureInfo.InvariantCulture)
										: "";

									var endLine = i < inOrOutputParameters.Count - 1 && extraInitializers.Count == 0 ? "," : "";

									if (hasInputValue)
										code.Add(string.Format(
											CultureInfo.InvariantCulture,
											"\tnew DataParameter({0}, {1}{2}, {3}{4}{5}){6}",
											ToStringLiteral(pr.SchemaName!),
											LenDiff(maxLenSchema, pr.SchemaName!),
											pr.ParameterName,
											LenDiff(maxLenParam, pr.ParameterName!),
											"LinqToDB.DataType." + pr.DataType,
											size,
											endLine));
									else
										code.Add(string.Format(
											CultureInfo.InvariantCulture,
											"\tnew DataParameter({0}, null, {1}{2}{3}){4}",
											ToStringLiteral(pr.SchemaName!),
											LenDiff(maxLenParam, pr.ParameterName!),
											"LinqToDB.DataType." + pr.DataType,
											size,
											endLine));

									if (extraInitializers.Count > 0)
									{
										code.Add("\t{");

										var maxPropertyLength = extraInitializers.Select(ei => ei.Item1.Length).Max();

										for (var j = 0; j < extraInitializers.Count; j++)
											code.Add(string.Format(
												CultureInfo.InvariantCulture,
												"\t\t{0}{1} = {2}{3}",
												extraInitializers[j].Item1,
												LenDiff(maxPropertyLength, extraInitializers[j].Item1),
												extraInitializers[j].Item2,
												j < extraInitializers.Count - 1 ? "," : ""));

										code.Add(i < inOrOutputParameters.Count - 1 ? "\t}," : "\t}");
									}
								}

								code.Add("};");
								code.Add("");

								return code;
							});
						}

						// we need to call ToList(), because otherwise output parameters will not be updated
						// with values. See https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/ms971497(v=msdn.10)#capturing-the-gazoutas
						var terminator = (GenerateProcedureResultAsList || outputParameters.Count > 0) && p.ResultTable != null ? ").ToList();" : ");";

						if (inOrOutputParameters.Count > 0)
							terminator = $", {parametersVarName}{terminator}";

						if (p.ResultTable == null)
						{
							p.BodyBuilders.Add(() => new [] { $"{prefix}dataConnection.ExecuteProc({spName}{terminator}" });
						}
						else
						{
							if (p.ResultTable.Columns.Values.Any(c => c.IsDuplicateOrEmpty))
							{
								p.BodyBuilders.Add(() => new []
								{
									"var ms = dataConnection.MappingSchema;",
									"",
									prefix + "dataConnection.QueryProc(dataReader =>",
									"\tnew " + p.ResultTable.TypeName,
									"\t{"
								});

								var n          = 0;
								var maxNameLen = p.ResultTable.Columns.Values.Max(c => (int?)c.MemberName? .Length) ?? 0;
								var maxTypeLen = p.ResultTable.Columns.Values.Max(c => (int?)c.BuildType()?.Length) ?? 0;

								foreach (var c in p.ResultTable.Columns.Values)
								{
									p.BodyBuilders.Add(() => new []
									{
										string.Format(
											CultureInfo.InvariantCulture,
											"\t\t{0}{1} = Converter.ChangeTypeTo<{2}>{3}(dataReader.GetValue({4}), ms),",
											c.MemberName,
											LenDiff(maxNameLen, c.MemberName!),
											c.BuildType(),
											LenDiff(maxTypeLen, c.BuildType()!),
											n++)
									});
								}

								p.BodyBuilders.Add(() => new [] {"\t},", "\t" + spName + terminator });
							}
							else
							{
								p.BodyBuilders.Add(() => new [] { prefix + "dataConnection.QueryProc<" + p.ResultTable.TypeName + ">(" + spName + terminator });
							}
						}

						if (hasOut)
						{
							maxLenSchema = outputParameters.Max(pr => (int?)pr.SchemaName?.   Length    ) ?? 0;
							maxLenParam  = outputParameters.Max(pr => (int?)pr.ParameterName?.Length    ) ?? 0;
							maxLenType   = outputParameters.Max(pr => (int?)pr.Type.ToTypeName().Length) ?? 0;

							p.BodyBuilders.Add(() => new [] { string.Empty });

							foreach (var pr in p.ProcParameters.Where(_ => _.IsOut || _.IsResult))
							{
								p.BodyBuilders.Add(() => new []
								{
									string.Format(
										CultureInfo.InvariantCulture,
										"{0} {1}= Converter.ChangeTypeTo<{2}>{3}({4}[{5}].Value);",
										pr.ParameterName,
										LenDiff(maxLenParam,  pr.ParameterName!),
										pr.Type.ToTypeName(),
										LenDiff(maxLenType, pr.Type.ToTypeName()),
										parametersVarName,
										inOrOutputParameters.IndexOf(pr))
								});
							}

							p.BodyBuilders.Add(() => new [] {"", "return " + retName + ";" });
						}
					}

					if (p.ResultTable != null && p.ResultTable.DataContextPropertyName == null)
					{
						var columns = new TMemberGroup { IsCompact = true };

						foreach (var c in p.ResultTable.Columns.Values)
						{
							if (c.MemberName != c.ColumnName)
								c.Attributes.Add(new TAttribute { Name = "Column", Parameters = { ToStringLiteral(c.ColumnName) } });
							columns.Members.Add(c);
						}

						p.ResultTable.Members.Add(columns);
						proc.Members.Add(p.ResultTable);
					}
				}

				if (procs.Members.Count > 0)
					Model.Types.Add(new TClass { Name = $"{DataContextObject?.Name}StoredProcedures", Members = { procs }, IsStatic = true });

				if (funcs.Members.Count > 0)
					Model.Types.Add(new TClass { Name = "SqlFunctions", Members = { funcs }, IsStatic = true });

				if (tabfs.Members.Count > 0)
					DataContextObject?.Members.Add(tabfs);

				MakeTypeMembersNamesUnique(DataContextObject!, "InitDataContext", "InitMappingSchema");
				MakeMembersNamesUnique(Model.Types, "Table");

				foreach (var type in Model.Types.OfType<IClass>())
					MakeTypeMembersNamesUnique(type, exceptMethods: ["FreeTextTable", "Find", "InitDataContext", "InitMappingSchema"]);

				foreach (var schema in schemas.Values)
				{
					if (schema.Procedures.Members.Count > 0)
						schema.Type.Members.Add(new TClass { Name = $"{DataContextObject?.Name}StoredProcedures", Members = { schema.Procedures }, IsStatic = true });

					if (schema.Functions.Members.Count > 0)
						schema.Type.Members.Add(new TClass { Name = "SqlFunctions", Members = { schema.Functions}, IsStatic = true });

					if (schema.TableFunctions.Members.Count > 0)
						schema.DataContext.Members.Add(schema.TableFunctions);

					MakeTypeMembersNamesUnique(schema.DataContext, "InitDataContext", "InitMappingSchema");

					foreach (var type in schema.Type.Members.OfType<IClass>())
						MakeTypeMembersNamesUnique(type);
				}
			}

			if (defTableExtensions.Members.Count > 0)
			{
				Model.Usings.Add("System.Linq");

				var tableExtensions = new TClass { Name = "TableExtensions", Members = { defTableExtensions }, IsStatic = true };

				Model.Types.Add(tableExtensions);

				MakeTypeMembersNamesUnique(tableExtensions, exceptMethods: ["Find", "FreeTextTable"]);
			}

			foreach (var schema in schemas.Values)
			{
				Model.Types.Add(schema.Type);

				if (schema.TableExtensions.Members.Count > 0)
				{
					Model.Usings.Add("System.Linq");
					schema.Type.Members.Add(schema.TableExtensions);
				}
			}

			Tables.    Clear();
			Procedures.Clear();

			Model.SetTree();

			AfterGenerateLinqToDBModel();
		}
	}
}
