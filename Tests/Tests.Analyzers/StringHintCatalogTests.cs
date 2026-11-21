using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Shouldly;

namespace Tests.Analyzers
{
	/// <summary>
	/// Checks L2DB1005's typed-hint lookup against the real linq2db assembly rather than against snippets: every hint
	/// constant must be found by the naming convention (so a template change breaks this test instead of silently
	/// narrowing the rule), and every string-to-typed mapping the rule reports must produce the same SQL.
	/// </summary>
	[TestFixture]
	public sealed class StringHintCatalogTests
	{
		// Hint constants with no parameterless typed helper in any scope their class serves. Each needs a value, an
		// index name or a table id, or has no helper at all; the rule has nothing to suggest for them. An entry that
		// starts to map fails the test, so the list cannot go stale.
		static readonly string[] Unmapped =
		[
			// Plain text options with no Option* helper in the SQL Server template.
			"SqlServer.Query.ParameterizationSimple",
			"SqlServer.Query.ParameterizationForced",
			// Its helper, WithOwnerAccessOption(), is a sub-query hint: not what QueryHint emits.
			"Access.Query.WithOwnerAccessOption",
		];

		// Providers whose ADO.NET client cannot be loaded by this test process, so SQL cannot be generated for them.
		// SQL Server CE's client needs the native Microsoft.SqlServer.Compact binaries, which run on Windows only. Its
		// typed helpers come from the same template as SQL Server's and both providers emit table hints through the same
		// builder.
		static readonly string[] ProvidersWithoutSqlGeneration =
		{
			"SqlCe",
		};

		static readonly string[] ConstantClasses = { "Table", "Query", "Hint" };

		// Hint constants the rule deliberately leaves out. Join hints (SQL Server, ClickHouse) attach to the join's table
		// or to the query by position, and MySQL's sub-query locks go through a builder of their own that comments some of
		// them out on MariaDB, so each needs its own SQL-equivalence check before it can be offered. PostgreSQL and YDB
		// declare their constants on the *Hints type itself, and their helpers take arguments. A new class of constants
		// fails EveryHintConstantClassIsInOrOutOfScope until it is put in one list or the other.
		static readonly string[] OutOfScopeConstantClasses = { "Join", "SubQuery" };

		static readonly string[] ProvidersWithTopLevelConstants = { "PostgreSQL", "Ydb" };

		[Table("Row")]
		public sealed class Row
		{
			[Column] public int Id { get; set; }
		}

		sealed record Constant(string Provider, string ClassName, string Name, string Value)
		{
			public string Id => Provider + "." + ClassName + "." + Name;
		}

		sealed record Mapping(string Scope, string Value, string Provider, string Namespace, string AsMethod, string Helper);

		static List<Constant> GetConstants()
		{
			var result = new List<Constant>();

			foreach (var type in typeof(Sql).Assembly.GetExportedTypes())
			{
				if (type.Namespace is null
					|| !type.Namespace.StartsWith("LinqToDB.DataProvider.", StringComparison.Ordinal)
					|| !type.IsAbstract || !type.IsSealed
					|| !type.Name.EndsWith("Hints", StringComparison.Ordinal))
					continue;

				var provider = type.Namespace.Substring("LinqToDB.DataProvider.".Length);

				foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
				{
					if (!ConstantClasses.Contains(nested.Name, StringComparer.Ordinal))
						continue;

					foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
						if (field.IsLiteral && field.FieldType == typeof(string))
							result.Add(new Constant(provider, nested.Name, field.Name, (string)field.GetRawConstantValue()!));
				}
			}

			return result;
		}

		static IEnumerable<string> ScopesOf(string className)
		{
			return className switch
			{
				"Table" => new[] { "TableHint", "TablesInScopeHint" },
				"Query" => new[] { "QueryHint" },
				_       => new[] { "TableHint", "TablesInScopeHint", "QueryHint" },
			};
		}

		static string Literal(string value)
		{
			return SymbolDisplay.FormatLiteral(value, quote: true);
		}

		// One generic-receiver call per hint text and scope, analysed in a single compilation; returns every mapping
		// the rule reported, read back from the diagnostics' properties.
		static async Task<List<Mapping>> RunAnalyzerAsync(IEnumerable<(string Scope, string Value)> calls)
		{
			var lines  = new List<(string Scope, string Value)>();
			var source = new StringBuilder();

			source.AppendLine("using System.Linq;");
			source.AppendLine("using LinqToDB;");
			source.AppendLine("class Row { }");
			source.AppendLine("class C");
			source.AppendLine("{");
			source.AppendLine("\tvoid M(ITable<Row> t, IQueryable<Row> q)");
			source.AppendLine("\t{");

			var firstLine = 7;

			foreach (var call in calls)
			{
				var receiver = call.Scope == "TableHint" ? "t" : "q";

				source
					.Append("\t\t")
					.Append(receiver)
					.Append('.')
					.Append(call.Scope)
					.Append('(')
					.Append(Literal(call.Value))
					.AppendLine(");");
				lines.Add(call);
			}

			source.AppendLine("\t}");
			source.AppendLine("}");

			var references = await TestReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);

			var compilation = CSharpCompilation.Create(
				"HintCatalog",
				new[] { CSharpSyntaxTree.ParseText(source.ToString()) },
				references.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			var errors = compilation
				.GetDiagnostics()
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.ToList();

			errors.ShouldBeEmpty("catalog snippet must compile");

			var diagnostics = await compilation
				.WithAnalyzers([new StringHintAnalyzer()])
				.GetAnalyzerDiagnosticsAsync(CancellationToken.None);

			var mappings = new List<Mapping>();

			foreach (var diagnostic in diagnostics)
			{
				diagnostic.Id.ShouldBe(StringHintAnalyzer.DiagnosticId);

				var (scope, value) = lines[diagnostic.Location.GetLineSpan().StartLinePosition.Line - firstLine];
				var count = int.Parse(diagnostic.Properties[StringHintAnalyzer.FixCountKey]!, CultureInfo.InvariantCulture);

				for (var i = 0; i < count; i++)
				{
					var prefix = "Fix" + i.ToString(CultureInfo.InvariantCulture);

					mappings.Add(new Mapping(
						scope,
						value,
						diagnostic.Properties[prefix + StringHintAnalyzer.FixProviderKey]!,
						diagnostic.Properties[prefix + StringHintAnalyzer.FixNamespaceKey]!,
						diagnostic.Properties[prefix + StringHintAnalyzer.FixAsMethodKey]!,
						diagnostic.Properties[prefix + StringHintAnalyzer.FixHelperKey]!));
				}
			}

			return mappings;
		}

		static IEnumerable<(string Scope, string Value)> AllCalls(IEnumerable<Constant> constants)
		{
			return constants
				.SelectMany(c => ScopesOf(c.ClassName).Select(s => (Scope: s, c.Value)))
				.Distinct()
				.ToList();
		}

		[Test]
		public async Task EveryHintConstantMapsToATypedHelper()
		{
			var constants = GetConstants();

			constants.Count.ShouldBeGreaterThan(100, "hint constants found by reflection");

			var mappings = await RunAnalyzerAsync(AllCalls(constants));
			var gated    = GetVersionGatedMappings(constants);
			var problems = new List<string>();

			foreach (var constant in constants)
			{
				var ns     = "LinqToDB.DataProvider." + constant.Provider;
				var scopes = ScopesOf(constant.ClassName)
					.ToList();
				var mapped = mappings.Any(m =>
					string.Equals(m.Namespace, ns, StringComparison.Ordinal)
					&& scopes.Contains(m.Scope, StringComparer.Ordinal)
					&& string.Equals(m.Value, constant.Value, StringComparison.Ordinal));

				// A parameterised constant (an index, a value, a table id) has a helper with arguments only; the
				// rule rightly leaves it alone. Recognised by the helper's own shape, not by a list.
				// A constant whose helper emits only from some server version on is withheld on purpose.
				if (!mapped && gated.Any(g => string.Equals(g.Namespace, ns, StringComparison.Ordinal) && string.Equals(g.Value, constant.Value, StringComparison.Ordinal)))
					continue;

				if (!mapped && !Unmapped.Contains(constant.Id, StringComparer.Ordinal) && !HasParameterisedHelper(constant))
					problems.Add(constant.Id + " = \"" + constant.Value + "\": no typed helper found by the naming convention");

				if (mapped && Unmapped.Contains(constant.Id, StringComparer.Ordinal))
					problems.Add(constant.Id + ": listed as unmapped but now maps; remove it from the list");
			}

			// Every helper the rule names must exist, be public and take the receiver only.
			var named = mappings
				.Select(m => (m.Namespace, m.Helper, m.AsMethod))
				.Distinct();

			foreach (var (ns, helper, asMethod) in named)
			{
				if (FindHelper(ns, helper) is null)
					problems.Add(ns + "." + helper + ": named by the rule but not found");

				var asMethods = asMethod.Length > 0
					? FindMethods(ns, asMethod)
					: null;

				if (asMethods is not null && !asMethods.Any())
					problems.Add(ns + "." + asMethod + ": named by the rule but not found");
			}

			problems.ShouldBeEmpty();

			// The well-known cases, as a guard against the lookup going quiet as a whole.
			mappings.ShouldSatisfyAllConditions(
				() => mappings.ShouldContain(m => m.Helper == "WithNoLock"        && m.Provider == "SQL Server"),
				() => mappings.ShouldContain(m => m.Helper == "WithNoLock"        && m.Provider == "SQL Server CE"),
				() => mappings.ShouldContain(m => m.Helper == "WithNoLockInScope" && m.Provider == "SQL Server"),
				() => mappings.ShouldContain(m => m.Helper == "OptionRecompile"   && m.Provider == "SQL Server"));
		}

		// A helper named by the convention that takes more than the receiver, e.g. SqlServerHints.WithIndex(table, name).
		static bool HasParameterisedHelper(Constant constant)
		{
			var ns    = "LinqToDB.DataProvider." + constant.Provider;
			var names = new[]
			{
				"With" + constant.Name, constant.Name + "Hint", "With" + constant.Name + "InScope", constant.Name + "InScopeHint", "Option" + constant.Name,
			};

			return names
				.SelectMany(n => FindMethods(ns, n))
				.Any(m => m.GetParameters().Length > 1);
		}

		static IEnumerable<MethodInfo> FindMethods(string ns, string name)
		{
			return typeof(Sql).Assembly.GetExportedTypes()
				.Where(t => string.Equals(t.Namespace, ns, StringComparison.Ordinal) && t.IsAbstract && t.IsSealed)
				.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
				.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal));
		}

		static MethodInfo? FindHelper(string ns, string name, Type? receiverDefinition = null)
		{
			return FindMethods(ns, name)
				.FirstOrDefault(m =>
					m.IsGenericMethodDefinition
					&& m.GetParameters().Length == 1
					&& (receiverDefinition is null || m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == receiverDefinition));
		}

		// Every dialect version of a provider: a typed helper can be gated on the server version while the string
		// overload always emits, and only a comparison on each version shows it.
		static readonly Dictionary<string, (string Version, Func<IDataProvider> Create)[]> DataProviders = new(StringComparer.Ordinal)
		{
			["SqlServer"] = Enum.GetValues<SqlServerVersion>()
				.Where(v => v != SqlServerVersion.AutoDetect)
				.Select(v => (v.ToString(), (Func<IDataProvider>)(() => SqlServerTools.GetDataProvider(v, SqlServerProvider.MicrosoftDataSqlClient))))
				.ToArray(),
			["MySql"] = Enum.GetValues<MySqlVersion>()
				.Where(v => v != MySqlVersion.AutoDetect)
				.Select(v => (v.ToString(), (Func<IDataProvider>)(() => MySqlTools.GetDataProvider(v, MySqlProvider.MySqlConnector))))
				.ToArray(),
			["Oracle"] = Enum.GetValues<OracleVersion>()
				.Where(v => v != OracleVersion.AutoDetect)
				.Select(v => (v.ToString(), (Func<IDataProvider>)(() => OracleTools.GetDataProvider(v, OracleProvider.Managed))))
				.ToArray(),
			["ClickHouse"] = [("default", () => ClickHouseTools.GetDataProvider(ClickHouseProvider.MySqlConnector))],
			["SQLite"]     = [("default", () => SQLiteTools    .GetDataProvider(SQLiteProvider.Microsoft))],
			["Access"]     = [("Jet", () => AccessTools.GetDataProvider(AccessVersion.Jet, AccessProvider.ODBC)), ("Ace", () => AccessTools.GetDataProvider(AccessVersion.Ace, AccessProvider.ODBC))],
		};

		// The mapping each version-gated helper would have had: its scope and constant from the naming convention.
		static List<Mapping> GetVersionGatedMappings(List<Constant> constants)
		{
			var result = new List<Mapping>();

			foreach (var entry in StringHintAnalyzer.VersionGatedHelpers)
			{
				var dot      = entry.IndexOf('.', StringComparison.Ordinal);
				var provider = entry.Substring(0, dot);
				var name     = entry.Substring(dot + 1);
				var ns       = "LinqToDB.DataProvider." + provider;
				var helper   = FindHelper(ns, name);

				helper.ShouldNotBeNull(entry + ": listed as version-gated but not found");

				var receiver = helper
					.GetParameters()[0]
					.ParameterType
					.GetGenericTypeDefinition();

				var isTable = receiver
					.GetInterfaces()
					.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITable<>));

				string scope, constantName;

				if (name.StartsWith("Option", StringComparison.Ordinal))
					(scope, constantName) = ("QueryHint", name.Substring("Option".Length));
				else if (name.EndsWith("InScopeHint", StringComparison.Ordinal))
					(scope, constantName) = ("TablesInScopeHint", name.Substring(0, name.Length - "InScopeHint".Length));
				else if (name.StartsWith("With", StringComparison.Ordinal) && name.EndsWith("InScope", StringComparison.Ordinal))
					(scope, constantName) = ("TablesInScopeHint", name.Substring(4, name.Length - 4 - "InScope".Length));
				else if (name.StartsWith("With", StringComparison.Ordinal))
					(scope, constantName) = ("TableHint", name.Substring(4));
				else
					(scope, constantName) = (isTable ? "TableHint" : "QueryHint", name.Substring(0, name.Length - "Hint".Length));

				var constant = constants.FirstOrDefault(c => string.Equals(c.Provider, provider, StringComparison.Ordinal) && string.Equals(c.Name, constantName, StringComparison.Ordinal));

				constant.ShouldNotBeNull(entry + ": no constant " + constantName);

				var asMethod = FindMethods(ns, "As" + provider)
					.First(m => m.ReturnType.GetGenericTypeDefinition() == receiver);

				result.Add(new Mapping(scope, constant.Value, provider, ns, asMethod.Name, name));
			}

			return result;
		}

		// The SQL of the string call on a generic receiver, of the provider's own string overload (where it has one) and
		// of the typed helper, for one mapping on one provider version.
		static (string StringSql, string? SpecificSql, string? TypedSql) GenerateSql(Mapping mapping, Func<IDataProvider> factory)
		{
			using var db = new DataConnection(new DataOptions()
				.UseConnectionString(factory(), "Server=fake"));

			var tableScope  = mapping.Scope == "TableHint";
			var receiverDef = tableScope ? typeof(ITable<>) : typeof(IQueryable<>);
			object source   = tableScope
				? db.GetTable<Row>()
				: db
					.GetTable<Row>()
					.Where(r => r.Id > 0);

			var stringQuery = GenericStringOverload(mapping.Scope, receiverDef)
				.MakeGenericMethod(typeof(Row))
				.Invoke(null, new[] { source, mapping.Value })!;

			var stringSql = Sql(stringQuery);

			var asMethod = FindMethods(mapping.Namespace, mapping.AsMethod)
				.Single(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == receiverDef)
				.MakeGenericMethod(typeof(Row));

			var specific = asMethod.Invoke(null, new[] { source })!;
			var helper   = FindHelper(mapping.Namespace, mapping.Helper, asMethod.ReturnType.GetGenericTypeDefinition());

			if (helper is null)
				return (stringSql, null, null);

			var typedQuery = helper
				.MakeGenericMethod(typeof(Row))
				.Invoke(null, new[] { specific })!;

			var typedSql = Sql(typedQuery);

			var specificOverload = FindMethods(mapping.Namespace, mapping.Scope)
				.FirstOrDefault(m =>
					m.GetGenericArguments().Length == 1
					&& m.GetParameters().Length == 2
					&& m.GetParameters()[1].ParameterType == typeof(string)
					&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == asMethod.ReturnType.GetGenericTypeDefinition());

			var specificSql = specificOverload is null
				? null
				: Sql(specificOverload
					.MakeGenericMethod(typeof(Row))
					.Invoke(null, new[] { specific, mapping.Value })!);

			return (stringSql, specificSql, typedSql);
		}

		static string ProviderOf(Mapping mapping)
		{
			return mapping.Namespace.Substring("LinqToDB.DataProvider.".Length);
		}

		static MethodInfo GenericStringOverload(string name, Type receiverDefinition)
		{
			return typeof(LinqExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m =>
					string.Equals(m.Name, name, StringComparison.Ordinal)
					&& m.GetGenericArguments().Length == 1
					&& m.GetParameters().Length == 2
					&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == receiverDefinition
					&& m.GetParameters()[1].ParameterType == typeof(string));
		}

		static string Escape(string sql)
		{
			return sql
				.Replace("\r", "\\r", StringComparison.Ordinal)
				.Replace("\n", "\\n\n", StringComparison.Ordinal)
				.Replace("\t", "\\t", StringComparison.Ordinal);
		}

		static string Sql(object query)
		{
			return ((IQueryable<Row>)query)
				.ToSqlQuery()
				.Sql;
		}

		// For every mapping the rule reports: the string hint on a generic receiver, the same string on the provider's
		// own receiver, and the typed helper must all produce the same SQL on that provider - that is what makes
		// "has a typed equivalent" true. The string is the constant's own text; matching ignores case and surrounding
		// blanks, which hint keywords on every provider here do too.
		[Test]
		public async Task TypedHelperProducesTheSameSqlAsTheString()
		{
			var mappings = await RunAnalyzerAsync(AllCalls(GetConstants()));

			var providers = mappings
				.Select(ProviderOf)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(p => p, StringComparer.Ordinal)
				.ToList();

			// A provider that gains typed hints must be added to DataProviders (or, if its client cannot load here,
			// to ProvidersWithoutSqlGeneration) rather than be skipped quietly.
			var uncovered = providers
				.Where(p => !DataProviders.ContainsKey(p) && !ProvidersWithoutSqlGeneration.Contains(p, StringComparer.Ordinal))
				.ToList();

			uncovered.ShouldBeEmpty("providers with typed hints but no SQL generation in this test");

			var problems = new List<string>();
			var checkedCount = 0;

			foreach (var mapping in mappings)
			{
				var provider = ProviderOf(mapping);

				if (!DataProviders.TryGetValue(provider, out var versions))
					continue;

				foreach (var (version, factory) in versions)
				{
					var (stringSql, specificSql, typedSql) = GenerateSql(mapping, factory);
					var label = $"{provider} {version}";

					if (typedSql is null)
					{
						problems.Add($"{label}: {mapping.Namespace}.{mapping.Helper}: no helper on the provider's receiver");
						continue;
					}

					// Equal SQL proves nothing if neither carries the hint.
					if (typedSql.IndexOf(mapping.Value, StringComparison.OrdinalIgnoreCase) < 0)
						problems.Add($"{label}: {mapping.AsMethod}().{mapping.Helper}() does not emit \"{mapping.Value}\"\n{Escape(typedSql)}");

					if (!string.Equals(stringSql, typedSql, StringComparison.Ordinal))
						problems.Add($"{label}: {mapping.Scope}(\"{mapping.Value}\") vs {mapping.AsMethod}().{mapping.Helper}()\n--- string:\n{Escape(stringSql)}\n--- typed:\n{Escape(typedSql)}");

					if (specificSql is not null && !string.Equals(specificSql, typedSql, StringComparison.Ordinal))
						problems.Add($"{label}: {mapping.AsMethod}().{mapping.Scope}(\"{mapping.Value}\") vs {mapping.Helper}()\n--- string:\n{Escape(specificSql)}\n--- typed:\n{Escape(typedSql)}");

					checkedCount++;
				}
			}

			var groups = mappings
				.GroupBy(ProviderOf)
				.OrderBy(g => g.Key, StringComparer.Ordinal);

			foreach (var group in groups)
				TestContext.Out.WriteLine($"{group.Key}: {group.Count()} mappings{(DataProviders.ContainsKey(group.Key) ? "" : " (SQL not generated)")}");

			problems.ShouldBeEmpty();
			checkedCount.ShouldBeGreaterThan(500, "mapping x version comparisons");
		}

		// The other direction: every helper withheld as version-gated must really differ from the string on some
		// version, or the exclusion is stale and costs a diagnostic for nothing.
		[Test]
		public void VersionGatedHelpersDifferOnSomeVersion()
		{
			var gated = GetVersionGatedMappings(GetConstants());

			gated.Count.ShouldBe(StringHintAnalyzer.VersionGatedHelpers.Length);

			foreach (var mapping in gated)
			{
				var differs = DataProviders[ProviderOf(mapping)].Any(v =>
				{
					var (stringSql, _, typedSql) = GenerateSql(mapping, v.Create);
					return !string.Equals(stringSql, typedSql, StringComparison.Ordinal);
				});

				differs.ShouldBeTrue($"{mapping.Namespace}.{mapping.Helper} emits the same SQL as the string on every version");
			}
		}

		[Test]
		public async Task VersionGatedHelpersAreNotOffered()
		{
			var mappings = await RunAnalyzerAsync([("TableHint", "FORCESCAN"), ("QueryHint", "NO_PERFORMANCE_SPOOL"), ("TablesInScopeHint", "SNAPSHOT")]);

			mappings.ShouldBeEmpty();
		}

		// A table receiver needs AsQueryable() before the provider call to reach the query form of a scope helper;
		// the rewrite the rule offers for that case must still produce the string hint's SQL.
		[Test]
		public void InScopeRewriteOnATableReceiverProducesTheSameSql()
		{
			using var db = new DataConnection(new DataOptions()
				.UseConnectionString(SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient), "Server=fake"));

			var stringSql = db
				.GetTable<Row>()
				.TablesInScopeHint("NoLock")
				.ToSqlQuery()
				.Sql;

			var typedSql  = db
				.GetTable<Row>()
				.AsQueryable()
				.AsSqlServer()
				.WithNoLockInScope()
				.ToSqlQuery()
				.Sql;

			typedSql.ShouldBe(stringSql);
		}

		// The rule's scope is the constant classes above; the readme states the same boundary.
		[Test]
		public void EveryHintConstantClassIsInOrOutOfScope()
		{
			var unlisted      = new List<string>();
			var topLevelOwner = new List<string>();

			foreach (var type in typeof(Sql).Assembly.GetExportedTypes())
			{
				if (type.Namespace is null
					|| !type.Namespace.StartsWith("LinqToDB.DataProvider.", StringComparison.Ordinal)
					|| !type.IsAbstract || !type.IsSealed
					|| !type.Name.EndsWith("Hints", StringComparison.Ordinal))
					continue;

				var provider = type.Namespace.Substring("LinqToDB.DataProvider.".Length);

				if (HasStringConstants(type))
					topLevelOwner.Add(provider);

				foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
				{
					if (HasStringConstants(nested)
						&& !ConstantClasses.Contains(nested.Name, StringComparer.Ordinal)
						&& !OutOfScopeConstantClasses.Contains(nested.Name, StringComparer.Ordinal))
						unlisted.Add(provider + "." + nested.Name);
				}
			}

			var owners = topLevelOwner
				.Order(StringComparer.Ordinal)
				.ToList();

			unlisted.ShouldBeEmpty("hint constant classes in neither ConstantClasses nor OutOfScopeConstantClasses");
			owners.ShouldBe(ProvidersWithTopLevelConstants);
		}

		static bool HasStringConstants(Type type)
		{
			return type
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.Any(static f => f.IsLiteral && f.FieldType == typeof(string));
		}

		// The message cites the hints guide by its path inside the package; the skill folder in the source tree is
		// what is packed under skills/, so the file must exist there.
		[Test]
		public void GuidePathInTheMessageExistsInTheSkillFolder()
		{
			var match = Regex.Match(StringHintAnalyzer.GuideSentence, @"skills/(?<path>[A-Za-z0-9_./-]+\.md)");

			match.Success.ShouldBeTrue("the message cites a skill file");
			match.Value.ShouldBe(StringHintAnalyzer.GuidePath);

			var root = FindRepositoryRoot();
			var parts = new[] { root, "Source", "Skills" }
				.Concat(match.Groups["path"].Value.Split('/'))
				.ToArray();

			var file = Path.Combine(parts);

			File
				.Exists(file)
				.ShouldBeTrue(file);
		}

		static string FindRepositoryRoot()
		{
			var directory = new DirectoryInfo(AppContext.BaseDirectory);

			while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "linq2db.slnx")))
				directory = directory.Parent;

			directory.ShouldNotBeNull("repository root (linq2db.slnx) above " + AppContext.BaseDirectory);

			return directory.FullName;
		}
	}
}
