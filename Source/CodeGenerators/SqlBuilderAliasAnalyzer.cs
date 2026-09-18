using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CodeGenerators
{
	/// <summary>
	/// Guards the SQL render path: inside a <c>BasicSqlBuilder</c>-derived type the finalized identifiers
	/// (table / column aliases and CTE field names) live in <c>AliasesContext</c>, not on the AST nodes -
	/// non-mutating aliasing keeps them off the nodes so a cached statement can be aliased / rendered
	/// concurrently. A builder must therefore resolve them through the context:
	/// <list type="bullet">
	/// <item><c>SqlColumn.Alias</c>       -&gt; <c>AliasesContext.GetColumnAlias</c></item>
	/// <item><c>SqlTableSource.Alias</c>  -&gt; <c>AliasesContext.GetTableAlias</c></item>
	/// <item><c>SqlCteTableField.Name</c> -&gt; <c>AliasesContext.GetFieldName</c></item>
	/// <item><c>SqlCteField.Name</c>      -&gt; <c>AliasesContext.GetFieldName</c></item>
	/// </list>
	/// Reading the node's own property bypasses the context and renders a stale/raw name.
	/// (<c>RawAlias</c> remains the un-flagged escape hatch for the raw stored alias.)
	/// </summary>
	/// <remarks>
	/// <c>SqlTable.Alias</c> and <c>SqlField.PhysicalName</c> are intentionally not guarded: they are the
	/// source of truth on the node. SqlTable aliases are not migrated into <c>AliasesContext</c>
	/// (<c>GetTableAlias</c> itself falls back to <c>SqlTable.Alias</c>), and <c>SqlField.PhysicalName</c>
	/// is the physical column name used verbatim in DDL as well as - via <c>GetFieldName</c> - in the
	/// render path, so a blanket guard would produce false positives. Only the CTE field types
	/// (<c>SqlCteTableField</c> / <c>SqlCteField</c>), which exist solely in the query/CTE render path and
	/// whose <c>Name</c> always delegates to the un-renamed definition, are guarded for their name.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class SqlBuilderAliasAnalyzer : DiagnosticAnalyzer
	{
		const string BuilderTypeName = "LinqToDB.Internal.SqlProvider.BasicSqlBuilder";

		// Guarded (owner type, property) -> the AliasesContext accessor that must be used instead.
		static readonly (string TypeName, string Property, string Kind, string Accessor)[] Guards =
		[
			("LinqToDB.Internal.SqlQuery.SqlColumn",        "Alias", "column alias",       "GetColumnAlias"),
			("LinqToDB.Internal.SqlQuery.SqlTableSource",   "Alias", "table-source alias", "GetTableAlias"),
			("LinqToDB.Internal.SqlQuery.SqlCteTableField", "Name",  "CTE field name",     "GetFieldName"),
			("LinqToDB.Internal.SqlQuery.SqlCteField",      "Name",  "CTE field name",     "GetFieldName"),
		];

		static readonly DiagnosticDescriptor Rule = new(
			id:                 "LINQ2DB0001",
			title:              "Read the finalized alias / name via AliasesContext",
			messageFormat:      "Read the finalized {0} via AliasesContext.{1}(...) instead of the node's {2} property: inside a SQL builder the node's {2} is not the finalized name (it lives in AliasesContext)",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "Non-mutating aliasing stores finalized names in AliasesContext, not on the AST nodes. SQL builders must resolve aliases / CTE field names through AliasesContext.GetColumnAlias / GetTableAlias / GetFieldName; RawAlias is the escape hatch for the raw stored value.");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				var builderType = startContext.Compilation.GetTypeByMetadataName(BuilderTypeName);
				if (builderType == null)
					return;

				// Resolve each guarded owner type once; skip any absent from this compilation.
				var guards = new List<(INamedTypeSymbol Type, string Property, string Kind, string Accessor)>();
				foreach (var (typeName, property, kind, accessor) in Guards)
				{
					var type = startContext.Compilation.GetTypeByMetadataName(typeName);
					if (type != null)
						guards.Add((type, property, kind, accessor));
				}

				if (guards.Count == 0)
					return;

				startContext.RegisterOperationAction(opContext =>
				{
					var op = (IPropertyReferenceOperation)opContext.Operation;

					// reads only - writing the alias / name is not a render-path concern.
					if (op.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, op))
						return;

					var owner = op.Property.ContainingType;

					string? kind = null, accessor = null, property = null;
					foreach (var (type, prop, gKind, gAccessor) in guards)
					{
						if (op.Property.Name == prop && SymbolEqualityComparer.Default.Equals(owner, type))
						{
							kind     = gKind;
							accessor = gAccessor;
							property = prop;
							break;
						}
					}

					if (kind == null)
						return;

					var enclosingType = opContext.ContainingSymbol as INamedTypeSymbol
						?? opContext.ContainingSymbol?.ContainingType;

					if (!DerivesFrom(enclosingType, builderType))
						return;

					opContext.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation(), kind, accessor, property));
				}, OperationKind.PropertyReference);
			});
		}

		static bool DerivesFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
		{
			for (var current = type; current != null; current = current.BaseType)
				if (SymbolEqualityComparer.Default.Equals(current, baseType))
					return true;

			return false;
		}
	}
}
