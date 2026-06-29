using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CodeGenerators
{
	/// <summary>
	/// Guards the SQL render path: inside a <c>BasicSqlBuilder</c>-derived type a node's
	/// <c>Alias</c> property is not the finalized alias - the finalized names live in
	/// <c>AliasesContext</c> (non-mutating aliasing keeps them off the AST nodes so a cached
	/// statement can be rendered concurrently). A builder must therefore resolve aliases through
	/// <c>AliasesContext.GetColumnAlias</c> / <c>GetTableAlias</c>; reading the node's own
	/// <c>SqlColumn.Alias</c> / <c>SqlTableSource.Alias</c> bypasses the context and renders a
	/// stale/raw name. (<c>RawAlias</c> remains the un-flagged escape hatch.)
	/// </summary>
	/// <remarks>
	/// <c>SqlTable.Alias</c> is intentionally not guarded: SqlTable aliases are not migrated into
	/// <c>AliasesContext</c> - they stay on the node (<c>AliasesContext.GetTableAlias</c> itself
	/// falls back to <c>SqlTable.Alias</c>), so for SqlTable the node is the source of truth and a
	/// raw render-path read is correct. If SqlTable aliasing is ever moved into the context, add
	/// <c>SqlTable</c> to the guarded owner set below.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class SqlBuilderAliasAnalyzer : DiagnosticAnalyzer
	{
		const string BuilderTypeName     = "LinqToDB.Internal.SqlProvider.BasicSqlBuilder";
		const string SqlColumnTypeName   = "LinqToDB.Internal.SqlQuery.SqlColumn";
		const string SqlTableSourceTypeName = "LinqToDB.Internal.SqlQuery.SqlTableSource";

		static readonly DiagnosticDescriptor Rule = new(
			id:                 "LINQ2DB0001",
			title:              "Read the finalized alias via AliasesContext",
			messageFormat:      "Read the finalized {0} alias via AliasesContext.{1}(...) instead of the node's Alias property: inside a SQL builder the node's Alias is not the finalized name (it lives in AliasesContext)",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "Non-mutating aliasing stores finalized names in AliasesContext, not on the AST nodes. SQL builders must resolve aliases through AliasesContext.GetColumnAlias / GetTableAlias; RawAlias is the escape hatch for the raw stored value.");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				var builderType  = startContext.Compilation.GetTypeByMetadataName(BuilderTypeName);
				if (builderType == null)
					return;

				var columnType    = startContext.Compilation.GetTypeByMetadataName(SqlColumnTypeName);
				var tableSrcType  = startContext.Compilation.GetTypeByMetadataName(SqlTableSourceTypeName);
				if (columnType == null && tableSrcType == null)
					return;

				startContext.RegisterOperationAction(opContext =>
				{
					var op = (IPropertyReferenceOperation)opContext.Operation;

					if (op.Property.Name != "Alias")
						return;

					// reads only - writing the alias is not a render-path concern.
					if (op.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, op))
						return;

					var owner = op.Property.ContainingType;

					string kind;
					string accessor;
					if (columnType != null && SymbolEqualityComparer.Default.Equals(owner, columnType))
					{
						kind     = "column";
						accessor = "GetColumnAlias";
					}
					else if (tableSrcType != null && SymbolEqualityComparer.Default.Equals(owner, tableSrcType))
					{
						kind     = "table-source";
						accessor = "GetTableAlias";
					}
					else
						return;

					var enclosingType = opContext.ContainingSymbol as INamedTypeSymbol
						?? opContext.ContainingSymbol?.ContainingType;

					if (!DerivesFrom(enclosingType, builderType))
						return;

					opContext.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation(), kind, accessor));
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
