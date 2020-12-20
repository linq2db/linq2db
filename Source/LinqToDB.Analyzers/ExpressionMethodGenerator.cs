using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LinqToDB.Analyzers
{
	[Generator]
	public class ExpressionMethodGenerator : ISourceGenerator
	{
		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				// any field with at least one attribute is a candidate for property generation
				if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax
					&& methodDeclarationSyntax.AttributeLists.Any()
					&& methodDeclarationSyntax.AttributeLists
						.SelectMany(a => a.Attributes)
						.Any(a => a.Name.ToString().Contains("GenerateExpressionMethod")))
				{
					CandidateMethods.Add(methodDeclarationSyntax);
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			// switch to DEBUG to test
#if false
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();
			}
#endif

			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;
			if (!receiver.CandidateMethods.Any()) return;

			var compilation = context.Compilation;
			if (compilation.GetTypeByMetadataName("LinqToDB.Mapping.GenerateExpressionMethodAttribute") is not { } attributeSymbol) return;

			foreach (var c in GetCandidates(context, receiver).GroupBy(g => g.containingType))
			{
				var containingType = c.Key;
				if (containingType.DeclaringSyntaxReferences.First().GetSyntax() is not ClassDeclarationSyntax containingTypeSyntax)
					continue;

				if (!containingTypeSyntax.Modifiers.Any(m => m.Text == "partial"))
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.ClassIsNotPartialError,
							containingTypeSyntax.Identifier.GetLocation(),
							containingType.Name));
					continue;
				}

				if (c.Any(f => !f.valid)) continue;

				var displayParts = containingType.ContainingNamespace.ToDisplayParts();
				var names = displayParts
					.Select(p => p.Symbol?.Name)
					.Where(n => !string.IsNullOrWhiteSpace(n));
				var fileName = string.Join(".", names) + "." + containingType.Name + ".ExpressionMethod.cs";

				context.AddSource(fileName, BuildMethods(containingType.ContainingNamespace, containingTypeSyntax, c.Select(f => f.method)));
			}
		}

		private static IEnumerable<(INamedTypeSymbol containingType, MethodDeclarationSyntax method, bool valid)> GetCandidates(
			GeneratorExecutionContext context, SyntaxReceiver receiver)
		{
			foreach (var m in receiver.CandidateMethods)
			{
				var model = context.Compilation.GetSemanticModel(m.SyntaxTree);
				var symbol = model.GetDeclaredSymbol(m);
				if (symbol == null) continue;

				var methodName = symbol.Name;
				var containingType = symbol.ContainingType;
				var valid = true;

				if (!m.Modifiers.Any(m => m.Text == "partial"))
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.MethodIsNotPartialError,
							m.Identifier.GetLocation(),
							methodName));
					valid = false;
				}

				if (m.ReturnType.ToString() == "void")
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.MethodReturnsVoidError,
							m.ReturnType.GetLocation(),
							methodName));
					valid = false;
				}

				if (m.Body != null
					&& (m.Body.Statements.Count > 1
						|| m.Body.Statements[0] is not ReturnStatementSyntax))
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.MethodHasIncorrectShapeError,
							m.Body.Statements[1].GetLocation(),
							methodName));
					valid = false;
				}

				yield return (containingType, m, valid);
			}
		}

		private static SourceText BuildMethods(INamespaceSymbol ns, ClassDeclarationSyntax classSyntax, IEnumerable<MethodDeclarationSyntax> methods)
		{
			var arrowMethodsSyntax = methods
				.Where(m => m.ExpressionBody != null)
				.SelectMany(BuildArrowMethod);
			var bodyMethodsSyntax = methods
				.Where(m => m.Body != null)
				.SelectMany(BuildBodyMethod);

			var unit = (CompilationUnitSyntax)classSyntax.SyntaxTree.GetRoot();
			classSyntax = classSyntax
				.WithMembers(new SyntaxList<MemberDeclarationSyntax>(
					arrowMethodsSyntax.Concat(bodyMethodsSyntax)));

			var newFile = SyntaxFactory.CompilationUnit()
				.WithUsings(unit.Usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Linq.Expressions").WithLeadingTrivia(SyntaxFactory.Space))))
				.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
					SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(ns.ToString()).WithLeadingTrivia(SyntaxFactory.Space))
						.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classSyntax))));

			return newFile.GetText(Encoding.UTF8);
		}

		private static IEnumerable<MethodDeclarationSyntax> BuildArrowMethod(MethodDeclarationSyntax m)
		{
			var newMethodName = $"__{m.Identifier.Text}Expression";
			yield return BuildPartialOriginalMethod(m, newMethodName);

			var expression = SyntaxFactory.ParenthesizedLambdaExpression()
				.WithParameterList(m.ParameterList)
				.WithExpressionBody(m.ExpressionBody!.Expression);

			yield return BuildNewMethod(m, newMethodName, expression);
		}

		private static MethodDeclarationSyntax BuildNewMethod(MethodDeclarationSyntax m, string newMethodName, ParenthesizedLambdaExpressionSyntax expression)
		{
			var funcType =
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier("Func"),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SeparatedList(
							m.ParameterList.Parameters
								.Select(p => p.Type!))
							.Add(m.ReturnType)));

			var returnType =
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier("Expression"),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SingletonSeparatedList<TypeSyntax>(funcType)));

			var method =  SyntaxFactory.MethodDeclaration(returnType, newMethodName)
				.WithModifiers(SyntaxFactory.TokenList(
					SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxFactory.Space),
					SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space)))
				.WithBody(SyntaxFactory.Block(
					SyntaxFactory.SingletonList(
						SyntaxFactory.ReturnStatement(expression))));
			return method;
		}

		private static MethodDeclarationSyntax BuildPartialOriginalMethod(MethodDeclarationSyntax m, string newMethodName) =>
			SyntaxFactory.MethodDeclaration(m.ReturnType, m.Identifier)
				.WithParameterList(m.ParameterList)
				.WithModifiers(m.Modifiers)
				.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
				.WithAttributeLists(
					SyntaxFactory.SingletonList(
						SyntaxFactory.AttributeList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.Attribute(
									SyntaxFactory.IdentifierName("LinqToDB.ExpressionMethodAttribute"),
									SyntaxFactory.AttributeArgumentList(
										SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(
											SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newMethodName))))))))));

		private static IEnumerable<MethodDeclarationSyntax> BuildBodyMethod(MethodDeclarationSyntax m)
		{
			var newMethodName = $"__{m.Identifier.Text}Expression";
			yield return BuildPartialOriginalMethod(m, newMethodName);

			var retStatement = (ReturnStatementSyntax) m.Body!.Statements[0];
			var expression = SyntaxFactory.ParenthesizedLambdaExpression()
				.WithParameterList(m.ParameterList)
				.WithExpressionBody(retStatement.Expression);

			yield return BuildNewMethod(m, newMethodName, expression);
		}
	}
}
