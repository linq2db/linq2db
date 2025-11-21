using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.SchemaProvider;

#pragma warning disable IDE1005
#pragma warning disable MA0011
#pragma warning disable MA0076
#pragma warning disable CA1304
#pragma warning disable MA0075

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public static string? BaseDataContextClass         { get; set; }
		public static bool    EnforceModelNullability      { get; set; } = true;
		public static string? GetDataOptionsMethod         { get; set; }

		public string? ServerName                          { get; set; }
		public string? DatabaseName                        { get; set; }
		public string? DataContextName                     { get; set; }
		public string? BaseEntityClass                     { get; set; }
		public string  OneToManyAssociationType            { get; set; } = "IEnumerable<{0}>";

		public bool GenerateModelOnly
		{
			get => _generateModelOnly;
			set
			{
				_generateModelOnly = value;

				if (value)
					BaseDataContextClass = null;
			}
		}

		public bool GenerateModelInterface
		{
			get => _generateModelInterface;
			set
			{
				_generateModelInterface = value;

				if (value)
					BaseDataContextClass = "LinqToDB.IDataContext";
			}
		}

		public bool    GenerateDatabaseInfo                { get; set; } = true;
		public bool    GenerateDatabaseName                { get; set; }
		public bool    GenerateDatabaseNameFromTable       { get; set; }
		public bool    GenerateConstructors                { get; set; } = true;
		public string? DefaultConfiguration                { get; set; }
		public bool    GenerateAssociations                { get; set; } = true;
		public bool    GenerateBackReferences              { get; set; } = true;
		public bool    GenerateAssociationExtensions       { get; set; }
		public bool    ReplaceSimilarTables                { get; set; } = true;
		public bool    IncludeDefaultSchema                { get; set; } = true;

		public IClass? DataContextObject                   { get; set; }

		public bool    PluralizeClassNames                 { get; set; }
		public bool    SingularizeClassNames               { get; set; } = true;
		public bool    PluralizeDataContextPropertyNames   { get; set; } = true;
		public bool    SingularizeDataContextPropertyNames { get; set; }
		public bool    PluralizeForeignKeyNames            { get; set; } = true;
		public bool    SingularizeForeignKeyNames          { get; set; } = true;

		public bool    NormalizeParameterName              { get; set; } = true;
		public bool    NormalizeProcedureColumnName        { get; set; } = true;
		public bool    NormalizeNames                      { get; set; } = true;
		public bool    NormalizeNamesWithoutUnderscores    { get; set; }
		public bool    ConvertUpperNamesToLower            { get; set; }

		private Func<string,bool,string>? _toValidName;
		public  Func<string,bool,string>   ToValidName
		{
			get => _toValidName ?? ToValidNameDefault;
			set => _toValidName = value;
		}

		private Func<string,bool,string>? _convertToCompilable;
		public  Func<string,bool,string>   ConvertToCompilable
		{
			get => _convertToCompilable ?? ConvertToCompilableDefault;
			set => _convertToCompilable = value;
		}

		private Func<string,bool,string>? _normalizeName;
		public  Func<string,bool,string>   NormalizeName
		{
			get => _normalizeName ?? NormalizeNameDefault;
			set => _normalizeName = value;
		}

		private Func<IForeignKey,string>? _getAssociationExtensionPluralName;
		public  Func<IForeignKey,string>   GetAssociationExtensionPluralName
		{
			get => _getAssociationExtensionPluralName ?? GetAssociationExtensionPluralNameDefault;
			set => _getAssociationExtensionPluralName = value;
		}

		private Func<IForeignKey,string>? _getAssociationExtensionSingularName;
		public  Func<IForeignKey,string>   GetAssociationExtensionSingularName
		{
			get => _getAssociationExtensionSingularName ?? GetAssociationExtensionSingularNameDefault;
			set => _getAssociationExtensionSingularName = value;
		}

		public GetSchemaOptions GetSchemaOptions { get; set; } = new ();

		public static Func<DataConnection,GetSchemaOptions,DatabaseSchema> LoadDatabaseSchema = (dataConnection, schemaOptions) =>
		{
			var sp = dataConnection.DataProvider.GetSchemaProvider();
			return sp.GetSchema(dataConnection, schemaOptions);
		};

		public event Action<IProperty,string,object>? SetPropertyValueAction;

		public void SetPropertyValue(IProperty propertyObject, string propertyName, object value)
		{
			if (SetPropertyValueAction != null)
				SetPropertyValueAction(propertyObject, propertyName, value);
		}

		public static Func<string,string> ToPlural    = s => s + "s";
		public static Func<string,string> ToSingular  = s => s;
		public static Func<string,bool>   IsValueType = IsValueTypeDefault;

		public static Func<ColumnSchema,string>                 ConvertColumnMemberType          = (c) => c.MemberType;
		public static Func<TableSchema,ColumnSchema,string>     ConvertTableColumnMemberType     = (_,c) => ConvertColumnMemberType(c);
		public static Func<ProcedureSchema,ColumnSchema,string> ConvertProcedureColumnMemberType = (_,c) => ConvertColumnMemberType(c);

		public Action AfterLoadMetadata = () => {};

		static bool IsValueTypeDefault(string typeName)
		{
			switch (typeName)
			{
				case "bool":
				case "bool?":
				case "char":
				case "char?":
				case "decimal":
				case "decimal?":
				case "int":
				case "int?":
				case "uint":
				case "uint?":
				case "byte":
				case "byte?":
				case "sbyte":
				case "sbyte?":
				case "long":
				case "long?":
				case "ulong":
				case "ulong?":
				case "short":
				case "short?":
				case "ushort":
				case "ushort?":
				case "float":
				case "float?":
				case "double":
				case "double?":
				case "DateTime":
				case "DateTime?":
				case "DateTimeOffset":
				case "DateTimeOffset?":
				case "TimeSpan":
				case "TimeSpan?":
				case "Guid":
				case "Guid?":
				case "SqlHierarchyId":
				case "SqlHierarchyId?":
				case "NpgsqlDate":
				case "NpgsqlDate?":
				case "NpgsqlTimeSpan":
				case "NpgsqlTimeSpan?":
				case "NpgsqlPoint":
				case "NpgsqlPoint?":
				case "NpgsqlLSeg":
				case "NpgsqlLSeg?":
				case "NpgsqlBox":
				case "NpgsqlBox?":
				case "NpgsqlPath":
				case "NpgsqlPath?":
				case "NpgsqlPolygon":
				case "NpgsqlPolygon?":
				case "NpgsqlCircle":
				case "NpgsqlCircle?":
				case "NpgsqlLine":
				case "NpgsqlLine?":
				case "NpgsqlInet":
				case "NpgsqlInet?":
				case "NpgsqlDateTime":
				case "NpgsqlDateTime?":
				case "NpgsqlCidr":
				case "NpgsqlCidr?":
					return true;
				case "object":
				case "string":
				case "byte[]":
				case "BitArray":
				case "SqlGeography":
				case "SqlGeometry":
				case "PhysicalAddress":
				case "Array":
				case "DataTable":
					return false;
			}

			return typeName.EndsWith("?");
		}

		public HashSet<string> KeyWords =
		[
			"abstract", "as",       "base",     "bool",    "break",     "byte",      "case",       "catch",     "char",    "checked",
			"class",    "const",    "continue", "decimal", "default",   "delegate",  "do",         "double",    "else",    "enum",
			"event",    "explicit", "extern",   "false",   "finally",   "fixed",     "float",      "for",       "foreach", "goto",
			"if",       "implicit", "in",       "int",     "interface", "internal",  "is",         "lock",      "long",    "new",
			"null",     "object",   "operator", "out",     "override",  "params",    "private",    "protected", "public",  "readonly",
			"ref",      "return",   "sbyte",    "sealed",  "short",     "sizeof",    "stackalloc", "static",    "struct",  "switch",
			"this",     "throw",    "true",     "try",     "typeof",    "uint",      "ulong",      "unchecked", "unsafe",  "ushort",
			"using",    "virtual",  "volatile", "void",    "while",     "namespace", "string"
		];

		int _counter;

		public bool IsParameter;
		public bool IsProcedureColumn;

		public string ToValidNameDefault(string name, bool mayRemoveUnderscore)
		{
			var normalize = IsParameter && NormalizeParameterName || IsProcedureColumn && NormalizeProcedureColumnName || (!IsParameter && !IsProcedureColumn && NormalizeNames);

			if (normalize)
			{
				if (mayRemoveUnderscore && name.Contains("_"))
					name = SplitAndJoin(name, "", '_');
				else if (NormalizeNamesWithoutUnderscores)
					name = NormalizeFragment(name);
			}

			if (name.Contains("."))
			{
				name = SplitAndJoin(name, "", '.');
			}

			if (name.Length > 0 && char.IsDigit(name[0]))
				name = $"_{name}";

			if (string.IsNullOrEmpty(name))
				name = $"_{_counter++}";

			if (normalize)
			{
				var isAllUpper = name.All(c => char.IsDigit(c) || char.IsLetter(c) && char.IsUpper(c));

				if (IsParameter)
				{
					name = isAllUpper ? name.ToLowerInvariant() : char.ToLower(name[0]) + name[1..];
				}
				else
				{
					if (isAllUpper && name.Length > 2 && ConvertUpperNamesToLower)
						name = name.ToLowerInvariant();
					name = char.ToUpper(name[0]) + name[1..];
				}
			}

			return name;
		}

		static string SplitAndJoin(string value, string join, params char[] split)
		{
			var ss = value.Split(split, StringSplitOptions.RemoveEmptyEntries).Select(NormalizeFragment);
			return string.Join(join, ss.ToArray());
		}

		static string NormalizeFragment(string s)
		{
			return s.Length == 0 ? s : char.ToUpper(s[0]) + (s.Substring(1).All(char.IsUpper) ? s.Substring(1).ToLower() : s.Substring(1));
		}

		public string ConvertToCompilableDefault(string name, bool mayRemoveUnderscore)
		{
			var query =
				from c in name
				select char.IsLetterOrDigit(c) || c == '@' ? c : '_';

			return ToValidName(new string(query.ToArray()), mayRemoveUnderscore);
		}

		string NormalizeNameDefault(string name, bool mayRemoveUnderscore)
		{
			name = ConvertToCompilable(name, mayRemoveUnderscore);

			if (KeyWords.Contains(name))
				name = "@" + name;

			return name;
		}

		string GetAssociationExtensionPluralNameDefault(IForeignKey key)
		{
			return ToPlural(ToSingular(key.Name!));
		}

		string GetAssociationExtensionSingularNameDefault(IForeignKey key)
		{
			return ToSingular(key.Name!);
		}

		public ISqlBuilder? SqlBuilder;
		bool                _generateModelOnly;
		bool                _generateModelInterface;

		protected Dictionary<string,TR> ToDictionary<T,TR>(IEnumerable<T> source, Func<T,string> keyGetter, Func<T,TR> objGetter, Func<TR,int,string> getKeyName)
		{
			var dic     = new Dictionary<string,TR>();
			var current = 1;

			foreach (var item in source)
			{
				var key = keyGetter(item);
				var obj = objGetter(item);

				if (string.IsNullOrEmpty(key) || dic.ContainsKey(key))
					key = getKeyName(obj, current);

				dic.Add(key, obj);

				current++;
			}

			return dic;
		}

		public string? CheckType(Type? type, string? typeName)
		{
			type ??= typeof(object);

			if (Model.Usings.Contains(type.Namespace ?? "") == false)
				Model.Usings.Add(type.Namespace ?? "");

			if (type.IsGenericType)
				foreach (var argType in type.GetGenericArguments())
					CheckType(argType, null);

			return typeName;
		}

		protected string CheckColumnName(string memberName)
		{
			if (string.IsNullOrEmpty(memberName))
			{
				memberName = "Empty";
			}
			else
			{
				memberName = memberName
					.Replace("%",      "Percent")
					.Replace(">",      "Greater")
					.Replace("<",      "Lower")
					.Replace("+",      "Plus")
					.Replace('(',      '_')
					.Replace(')',      '_')
					.Replace('-',      '_')
					.Replace('|',      '_')
					.Replace(',',      '_')
					.Replace('"',      '_')
					.Replace("'",      "_")
					.Replace(".",      "_")
					.Replace("\u00A3", "Pound");

				IsProcedureColumn = true;
				memberName        = NormalizeName(memberName, false);
				IsProcedureColumn = false;
			}

			return memberName;
		}

		protected string CheckParameterName(string parameterName)
		{
			var invalidParameterNames = new List<string>
			{
				"@DataType"
			};

			var result = parameterName;

			while (invalidParameterNames.Contains(result))
				result += "_";

			return result;
		}

		protected string SuggestNoDuplicate(IEnumerable<string> currentNames, string newName, string? prefix)
		{
			var names  = new HashSet<string>(currentNames);
			var result = newName;

			if (names.Contains(result))
			{
				if (!string.IsNullOrEmpty(prefix))
					result = prefix + result;

				if (names.Contains(result))
				{
					var counter = 0;

					// get last 6 digits
					var idx = result.Length;

					while (idx > 0 && idx > result.Length - 6 && char.IsDigit(result[idx - 1]))
						idx--;

					var number = result[idx..];

					if (!string.IsNullOrEmpty(number) && int.TryParse(number, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out counter))
						result = result.Remove(result.Length - number.Length);

					do
					{
						++counter;

						if (!names.Contains(result + counter))
						{
							result += counter;
							break;
						}
					}
					while(true);
				}
			}

			return result;
		}
	}

	public partial class ModelGenerator<TTable,TProcedure> : ModelGenerator
		where TTable     : class, ITable,      new()
		where TProcedure : IProcedure<TTable>, new()
	{
		public ModelGenerator(
			IModelSource    model,
			StringBuilder   generationEnvironment,
			Action<string?> write,
			Action<string?> writeLine,
			Action<string>  pushIndent,
			Func<string>    popIndent,
			Action<string>  error)
			: base(model, generationEnvironment, write, writeLine, pushIndent, popIndent, error)
		{
		}

		public Dictionary<string,TTable>     Tables     { get; set; } = new ();
		public Dictionary<string,TProcedure> Procedures { get; set; } = new ();

		public Func<TableSchema,TTable?> LoadProviderSpecificTable = _ => null;

		public void LoadServerMetadata<TForeignKey,TColumn>(DataConnection dataConnection)
			where TForeignKey : ForeignKey<TForeignKey>, new()
			where TColumn     : IColumn,                 new()
		{
			if (DataContextObject == null)
				DataContextObject = new Class<TTable>();

			SqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, dataConnection.Options);

			var db = LoadDatabaseSchema(dataConnection, GetSchemaOptions);

			if (DatabaseName == null && GenerateDatabaseName)
				DatabaseName = db.Database;

			if (DataContextName == null)
				DataContextObject!.Name = DataContextName = ToValidName((GenerateModelInterface ? "I" : "") + db.Database, true) + "DB";

			if (GenerateDatabaseInfo)
			{
				DataContextObject!.Comment.Add("/ <summary>");
				DataContextObject!.Comment.Add("/ Database       : " + db.Database);
				DataContextObject!.Comment.Add("/ Data Source    : " + db.DataSource);
				DataContextObject!.Comment.Add("/ Server Version : " + db.ServerVersion);
				DataContextObject!.Comment.Add("/ </summary>");
			}

			var tables = db.Tables
				.Where (t => !t.IsProviderSpecific)
				.Select(t => new
				{
					t,
					key = t.IsDefaultSchema ? t.TableName : t.SchemaName + "." + t.TableName,
					table = (TTable?)new TTable
					{
						TableSchema             = t,
						IsDefaultSchema         = t.IsDefaultSchema,
						Schema                  = t.IsDefaultSchema && !IncludeDefaultSchema || string.IsNullOrEmpty(t.GroupName ?? t.SchemaName) ? null : t.GroupName ?? t.SchemaName,
						BaseClass               = BaseEntityClass,
						TableName               = t.TableName,
						TypeName                = t.TypeName,
						DataContextPropertyName = t.TypeName,
						IsView                  = t.IsView,
						IsProviderSpecific      = false,
						Description             = t.Description,
						Columns                 = t.Columns.ToDictionary(
							c => c.ColumnName,
							c =>
							{
								var type = new ModelType(ConvertTableColumnMemberType(t, c), !IsValueType(ConvertTableColumnMemberType(t, c)), c.IsNullable);

								return (IColumn)new TColumn
								{
									ModelType       = type,
									TypeBuilder     = () => type.ToTypeName(),
									ColumnName      = c.ColumnName,
									ColumnType      = c.ColumnType,
									DataType        = $"DataType.{c.DataType}",
									Length          = c.Length,
									Precision       = c.Precision,
									Scale           = c.Scale,
									IsNullable      = c.IsNullable,
									IsIdentity      = c.IsIdentity,
									IsPrimaryKey    = c.IsPrimaryKey,
									PrimaryKeyOrder = c.PrimaryKeyOrder,
									MemberName      = CheckType(c.SystemType, c.MemberName),
									SkipOnInsert    = c.SkipOnInsert,
									SkipOnUpdate    = c.SkipOnUpdate,
									Description     = c.Description,
								};
							})
					}
				})
				.ToList();

			if (PluralizeClassNames || SingularizeClassNames)
			{
				var foundNames = new HashSet<string>(tables.Select(t => t.table!.Schema + '.' + t.table.TypeName));

				foreach (var t in tables)
				{
					var newName = t.table!.TypeName;
						newName =
							PluralizeClassNames   ? ToPlural  (newName!) :
							SingularizeClassNames ? ToSingular(newName!) : newName;

					if (newName != t.table.TypeName)
					{
						if (!foundNames.Contains(t.table.Schema + '.' + newName))
						{
							t.table.TypeName = newName;
							foundNames.Add(t.table.Schema + '.' + newName);
						}
					}
				}
			}

			if (PluralizeDataContextPropertyNames || SingularizeDataContextPropertyNames)
			{
				var foundNames = new HashSet<string>(tables.Select(t => t.table!.Schema + '.' + t.table.DataContextPropertyName));

				foreach (var t in tables)
				{
					var newName = t.table!.DataContextPropertyName;
						newName =
							PluralizeDataContextPropertyNames   ? ToPlural  (newName!) :
							SingularizeDataContextPropertyNames ? ToSingular(newName!) : newName;

					if (newName != t.table.TypeName)
					{
						if (!foundNames.Contains(t.table.Schema + '.' + newName))
						{
							t.table.DataContextPropertyName = newName;
							foundNames.Add(t.table.Schema + '.' + newName);
						}
					}
				}
			}

			tables.AddRange(db.Tables
				.Where (t => t.IsProviderSpecific)
				.Select(t => new
				{
					t,
					key   = t.IsDefaultSchema ? t.TableName : t.SchemaName + "." + t.TableName,
					table = LoadProviderSpecificTable(t)
				})
				.Where(t => t.table != null));

			foreach (var t in tables)
				Tables.Add(t.key!, t.table!);

			var keys =
			(
				from t in tables
				from k in t.t.ForeignKeys
				let thisTable  = t.table
				let otherTable = tables.Where(tbl => tbl.t == k.OtherTable).Select(tbl => tbl.table).Single()
				select new
				{
					k,
					k.KeyName,
					t,
					key = new TForeignKey
					{
						KeyName         = k.KeyName,
						ThisTable       = thisTable,
						OtherTable      = otherTable,
						OtherColumns    = k.OtherColumns.Select(c => otherTable.Columns[c.ColumnName]).ToList(),
						ThisColumns     = k.ThisColumns. Select(c => t.table.   Columns[c.ColumnName]).ToList(),
						CanBeNull       = k.CanBeNull,
						MemberName      = k.MemberName,
						AssociationType = (AssociationType)(int)k.AssociationType,
					}
				}
			).ToList();

			foreach (var key in keys)
			{
				var keyName = (key.k.OtherTable.IsDefaultSchema ? null : key.k.OtherTable.SchemaName + ".")
					+ key.k.OtherTable.TableName + "."
					+ key.KeyName;

				key.t.table.ForeignKeys.Add(keyName, key.key);

				if (key.k.BackReference != null)
					key.key.BackReference = keys.First(k => k.k == key.k.BackReference).key;

				key.key.MemberName = key.key.MemberName!.Replace(".", string.Empty);

				key.key.MemberName = key.key.AssociationType == AssociationType.OneToMany
					? PluralizeForeignKeyNames   ? ToPlural  (key.key.MemberName) : key.key.MemberName
					: SingularizeForeignKeyNames ? ToSingular(key.key.MemberName) : key.key.MemberName;
			}

			var procedures = db.Procedures
				.Select(p => new
				{
					p,
					key = p.IsDefaultSchema ? (p.PackageName == null ? null : (p.PackageName + ".")) + p.ProcedureName : p.SchemaName + "." + (p.PackageName == null ? null : (p.PackageName + ".")) + p.ProcedureName,
					proc = new TProcedure
					{
						Schema              = (p.IsDefaultSchema && !IncludeDefaultSchema) || string.IsNullOrEmpty(p.SchemaName)? null : p.SchemaName,
						ProcedureName       = p.ProcedureName,
						PackageName         = p.PackageName,
						Name                = ToValidName(p.MemberName, true),
						IsFunction          = p.IsFunction,
						IsTableFunction     = p.IsTableFunction,
						IsAggregateFunction = p.IsAggregateFunction,
						IsDefaultSchema     = p.IsDefaultSchema,
						Description         = p.Description,
						IsLoaded            = p.IsLoaded,
						ResultTable         = p.ResultTable == null ? null :
							new TTable
							{
								TypeName = ToValidName(
									PluralizeClassNames   ? ToPlural  (p.ResultTable.TypeName) :
									SingularizeClassNames ? ToSingular(p.ResultTable.TypeName) : p.ResultTable.TypeName, true),
								Columns  = ToDictionary(
									p.ResultTable.Columns,
									c => c.ColumnName,
									c =>
									{
										var type = new ModelType(ConvertProcedureColumnMemberType(p, c), !IsValueType(ConvertProcedureColumnMemberType(p, c)), c.IsNullable);

										return (IColumn)new TColumn
										{
											ModelType       = type,
											TypeBuilder     = () => type.ToTypeName(),
											ColumnName      = c.ColumnName,
											ColumnType      = c.ColumnType,
											IsNullable      = c.IsNullable,
											IsIdentity      = c.IsIdentity,
											IsPrimaryKey    = c.IsPrimaryKey,
											PrimaryKeyOrder = c.PrimaryKeyOrder,
											MemberName      = CheckColumnName(CheckType(c.SystemType, c.MemberName)!),
											SkipOnInsert    = c.SkipOnInsert,
											SkipOnUpdate    = c.SkipOnUpdate,
											Description     = c.Description,
										};
									},
									(c,n) =>
									{
										c.IsDuplicateOrEmpty = true;
										return "$" + (c.MemberName = $"Column{n}");
									})
							},
						ResultException = p.ResultException,
						SimilarTables   = p.SimilarTables == null ? [] :
							p.SimilarTables
								.Select(t => tables.Single(tbl => tbl.t == t).table!)
								.ToList(),
						ProcParameters  = p.Parameters
							.Select(pr => new Parameter
							{
								SchemaName    = pr.SchemaName,
								SchemaType    = pr.SchemaType,
								IsIn          = pr.IsIn,
								IsOut         = pr.IsOut,
								IsResult      = pr.IsResult,
								Size          = pr.Size,
								ParameterName = CheckParameterName(CheckType(pr.SystemType, pr.ParameterName)!),
								ParameterType = pr.ParameterType,
								SystemType    = pr.SystemType ?? typeof(object),
								DataType      = pr.DataType.ToString(),
								IsNullable    = pr.IsNullable,
								Description   = pr.Description,
							})
							.ToList(),
					}
				})
				.ToList();

			foreach (var p in procedures)
			{
				if (ReplaceSimilarTables)
					if (p.proc.SimilarTables.Count == 1 || p.proc.SimilarTables.Count(t => !t.IsView) == 1)
						p.proc.ResultTable = p.proc.SimilarTables.Count == 1 ?
							p.proc.SimilarTables[0] :
							p.proc.SimilarTables.First(t => !t.IsView);

				Procedures[p.key] = p.proc;
			}
		}

		public TTable GetTable(string name)
		{
			if (Tables.TryGetValue(name, out var tbl))
				return tbl;

			WriteLine($"#error Table '{name}' not found.");
			WriteLine("/*");
			WriteLine("\tExisting tables:");
			WriteLine("");

			foreach (var key in Tables.Keys)
				WriteLine($"\t{key}");

			WriteLine(" */");

			Error($"Table '{name}' not found.");

			throw new ArgumentException($"Table '{name}' not found.");
		}

		public TProcedure GetProcedure(string name)
		{
			if (Procedures.TryGetValue(name, out var proc))
				return proc;

			WriteLine($"#error Procedure '{name}' not found.");
			WriteLine("");
			WriteLine("/*");
			WriteLine("\tExisting procedures:");
			WriteLine("");

			foreach (var key in Procedures.Keys)
				WriteLine($"\t{key}");

			WriteLine(" */");

			Error($"Procedure '{name}' not found.");

			throw new ArgumentException($"Procedure '{name}' not found.");
		}

		public IColumn GetColumn(string tableName, string columnName)
		{
			var tbl = GetTable(tableName);

			if (tbl.Columns.TryGetValue(columnName, out var col))
				return col;

			WriteLine($"#error Column '{tableName}'.'{columnName}' not found.");
			WriteLine("");
			WriteLine("/*");
			WriteLine($"\tExisting '{tableName}'columns:");
			WriteLine("");

			foreach (var key in tbl.Columns.Keys)
				WriteLine($"\t{key}");

			WriteLine(" */");

			Error($"Column '{tableName}'.'{columnName}' not found.");

			throw new ArgumentException($"Column '{tableName}'.'{columnName}' not found.");
		}

		public IForeignKey GetFK(string tableName, string fkName)
		{
			return GetForeignKey(tableName, fkName);
		}

		public IForeignKey GetForeignKey(string tableName, string fkName)
		{
			var tbl = GetTable(tableName);

			if (tbl.ForeignKeys.TryGetValue(fkName, out var fk))
				return fk;

			WriteLine($"#error FK '{tableName}'.'{fkName}' not found.");
			WriteLine("");
			WriteLine("/*");
			WriteLine($"\tExisting '{tableName}'FKs:");
			WriteLine("");

			foreach (var key in tbl.ForeignKeys.Keys)
				WriteLine($"\t{key}");

			WriteLine(" */");

			Error($"FK '{tableName}'.'{fkName}' not found.");

			throw new ArgumentException($"FK '{tableName}'.'{fkName}' not found.");
		}

		public TableContext<TTable,TProcedure> SetTable(
			string  tableName,
			string? TypeName                = null,
			string? DataContextPropertyName = null)
		{
			var ctx = new TableContext<TTable,TProcedure>(this, tableName);

			if (TypeName != null || DataContextPropertyName != null)
			{
				var t = GetTable(tableName);

				if (TypeName                != null) t.TypeName                = TypeName;
				if (DataContextPropertyName != null) t.DataContextPropertyName = DataContextPropertyName;
			}

			return ctx;
		}

		public void LoadMetadata<TClass,TForeignKey,TColumn>(DataConnection dataConnection)
			where TClass      : Class     <TClass>,      new()
			where TForeignKey : ForeignKey<TForeignKey>, new()
			where TColumn     : IColumn,                 new()
		{
			if (DataContextObject == null)
			{
				DataContextObject = new TClass { Name = DataContextName ?? "" };

				DataContextObject.BaseClass = BaseDataContextClass;

				Model.Types.Add(DataContextObject);
			}

			if (GenerateModelInterface)
				DataContextObject.IsInterface = true;

			LoadServerMetadata<TForeignKey,TColumn>(dataConnection);

			if (Tables.Values.SelectMany(t => t.ForeignKeys.Values).Any(t => t.AssociationType == AssociationType.OneToMany))
				Model.Usings.Add("System.Collections.Generic");

			foreach (var t in Tables.Values)
			{
				t.TypeName                = NormalizeName(t.TypeName!,                true);
				t.DataContextPropertyName = NormalizeName(t.DataContextPropertyName!, true);

				foreach (var col in t.Columns.Values)
				{
					col.MemberName = NormalizeName(col.MemberName!, true);

					if (col.MemberName == t.TypeName)
						col.MemberName += "Column";
				}

				foreach (var fk in t.ForeignKeys.Values)
				{
					fk.MemberName = NormalizeName(fk.MemberName, true);

					if (fk.MemberName == t.TypeName)
						fk.MemberName += "_FK";
				}
			}

			foreach (var t in Tables.Values)
			{
				var hasDuplicates = t.Columns.Values
					.Select(c => c.MemberName)
					.Concat(t.ForeignKeys.Values.Select(f => f.MemberName))
					.ToLookup(n => n)
					.Any(g => g.Count() > 1);

				if (hasDuplicates)
				{
					foreach (var fk in t.ForeignKeys.Values)
					{
						var mayDuplicate = t.Columns.Values
							.Select(c => c.MemberName)
							.Concat(t.ForeignKeys.Values.Where(f => f != fk).Select(f => f.MemberName));

						fk.MemberName = SuggestNoDuplicate(mayDuplicate!, fk.MemberName!, "FK");
					}

					foreach (var col in t.Columns.Values)
					{
						var mayDuplicate = t.Columns.Values
							.Where(c => c != col)
							.Select(c => c.MemberName)
							.Concat(t.ForeignKeys.Values.Select(fk => fk.MemberName));

						col.MemberName = SuggestNoDuplicate(mayDuplicate!, col.MemberName!, null);
					}
				}
			}

			foreach (var proc in Procedures.Values)
			{
				proc.Name = NormalizeName(proc.Name!, false);

				// for now indicate parameter using instance field to not break API
				// if requested, we can add enum with name types and pass it to normalization delegates API
				IsParameter = true;

				foreach (var param in proc.ProcParameters)
					param.ParameterName = NormalizeName(param.ParameterName!, true);

				IsParameter = false;
			}

			AfterLoadMetadata();
		}
	}
}
