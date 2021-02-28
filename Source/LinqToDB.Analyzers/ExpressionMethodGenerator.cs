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
			public List<MemberDeclarationSyntax> CandidateMethods { get; } = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax
					&& methodDeclarationSyntax.AttributeLists.Any()
					&& methodDeclarationSyntax.AttributeLists
						.SelectMany(a => a.Attributes)
						.Any(a => a.Name.ToString().Contains("GenerateExpressionMethod")))
				{
					CandidateMethods.Add(methodDeclarationSyntax);
				}

				if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax
					&& propertyDeclarationSyntax.AttributeLists.Any()
					&& propertyDeclarationSyntax.AttributeLists
						.SelectMany(a => a.Attributes)
						.Any(a => a.Name.ToString().Contains("GenerateExpressionMethod")))
				{
					CandidateMethods.Add(propertyDeclarationSyntax);
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			// switch to DEBUG to test
			// NB: This _will_ piss you off when turned on. Fair warning...
#if false
			Debugger.Launch();
#endif

			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is not SyntaxReceiver receiver)
				return;
			if (!receiver.CandidateMethods.Any())
				return;

			var compilation = context.Compilation;
			if (compilation.GetTypeByMetadataName("LinqToDB.Mapping.GenerateExpressionMethodAttribute") is not { } attributeSymbol)
				return;

			var fileIncrement = new Dictionary<string, int>();
			// false warning RS1024: https://github.com/dotnet/roslyn-analyzers/issues/4568
#pragma warning disable RS1024 // Compare symbols correctly
			var processedClasses = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

			foreach (var m in receiver.CandidateMethods)
			{
				var model = context.Compilation.GetSemanticModel(m.SyntaxTree);
				var symbol = model.GetDeclaredSymbol(m);
				if (symbol == null) continue;

				var methodName = symbol.Name;
				if (!fileIncrement.TryGetValue(methodName, out var increment))
					increment = 0;
				var fileName = $"{methodName}.{fileIncrement[methodName] = increment + 1}.cs";

				var (valid, containingTypes) = ValidateMethod(ref context, m, symbol, processedClasses);
				if (!valid) continue;

				var newMethod = GetNewMethodName(ref context, attributeSymbol!, m, model, methodName, ref valid);
				if (!valid) continue;

				var method = BuildMethod(model, symbol, m, newMethod);

				var ns = containingTypes.Peek().ContainingNamespace
					.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

				// build containing class definitions
				MemberDeclarationSyntax parent = method;
				do
				{
					parent = BuildContainingClass(parent, containingTypes.Dequeue());
				} while (containingTypes.Count > 0);

				var usings = ((CompilationUnitSyntax)m.SyntaxTree.GetRoot()).Usings;
				// in case user did not include them in original file
				if (!usings.Any(u => u.Name.ToString() == "System"))
					usings = usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System").WithLeadingTrivia(SyntaxFactory.Space)));
				if (!usings.Any(u => u.Name.ToString() == "System.Linq.Expressions"))
					usings = usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Linq.Expressions").WithLeadingTrivia(SyntaxFactory.Space)));

				var unit = SyntaxFactory.CompilationUnit()
					.WithUsings(usings)
					.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
						SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(ns).WithLeadingTrivia(SyntaxFactory.Space))
							.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(parent))))
					.NormalizeWhitespace();

				context.AddSource(fileName, unit.GetText(Encoding.UTF8));
			}
		}

		private static (bool valid, Queue<INamedTypeSymbol> containingTypes) ValidateMethod(ref GeneratorExecutionContext context, MemberDeclarationSyntax m, ISymbol symbol, HashSet<ISymbol> processedClasses)
		{
			var methodName = symbol.Name;
			var valid = true;
			if (m is MethodDeclarationSyntax mds)
			{
				if (mds.ReturnType.ToString() == "void")
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.MethodReturnsVoidError,
							mds.ReturnType.GetLocation(),
							methodName));
					valid = false;
				}

				// if mds.Body == null, then we've got an arrow syntax, and there's nothing else we need to check
				if (mds.Body != null
					&& mds.Body.Statements.Count > 1
					&& mds.Body.Statements.FirstOrDefault() is not ReturnStatementSyntax)
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.MethodHasIncorrectShapeError,
							mds.Body.Statements[1].GetLocation(),
							methodName));
					valid = false;
				}
			}
			else if (m is PropertyDeclarationSyntax pds)
			{
				// if this is false, then it's an arrow property, and there's nothing else we need to check.
				if (pds.AccessorList != null
					&& pds.AccessorList.Accessors.Any())
				{
					var getAccessor = pds.AccessorList.Accessors
						.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
					if (getAccessor == null)
					{
						context.ReportDiagnostic(
							Diagnostic.Create(
								Diagnostics.PropertyDoesNotHaveGetWarning,
								pds.Identifier.GetLocation(),
								methodName));
						valid = false;
					}
					// same as above: if Body == null, then we've got an arrow syntax and there's nothing else we need to check
					else if (getAccessor.Body != null
						&& getAccessor.Body.Statements.Count > 0
						&& getAccessor.Body.Statements.FirstOrDefault() is not ReturnStatementSyntax)
					{
						context.ReportDiagnostic(
							Diagnostic.Create(
								Diagnostics.MethodHasIncorrectShapeError,
								getAccessor.Body.Statements[1].GetLocation(),
								methodName));
						valid = false;
					}
				}
			}
			else
				valid = false; // unknown what happened here, since should be limited by syntaxreceiver

			// handle nested classes. unlimited levels as far as we're concerned.
			var containingTypes = new Queue<INamedTypeSymbol>();
			var cType = symbol.ContainingType;
			while (cType != null)
			{
				containingTypes.Enqueue(cType);
				if (cType.DeclaringSyntaxReferences.First().GetSyntax() is not ClassDeclarationSyntax syntax)
					break; // shouldn't be possible...

				if (!syntax.Modifiers.Any(m => m.Kind() == SyntaxKind.PartialKeyword))
				{
					if (!processedClasses.Contains(cType))
					{
						context.ReportDiagnostic(
							Diagnostic.Create(
								Diagnostics.ClassIsNotPartialError,
								syntax.Identifier.GetLocation(),
								cType.Name));
						processedClasses.Add(cType);
					}
					valid = false;
				}

				cType = cType.ContainingType;
			}

			return (valid, containingTypes);
		}

		private static (string name, bool isPublic) GetNewMethodName(
			ref GeneratorExecutionContext context,
			INamedTypeSymbol attributeSymbol,
			MemberDeclarationSyntax m,
			SemanticModel model,
			string methodName,
			ref bool valid)
		{
			var attribute = m.AttributeLists
				.SelectMany(al => al.Attributes)
				.First(a => attributeSymbol.Equals(model.GetTypeInfo(a).Type, SymbolEqualityComparer.Default));
			if (attribute.ArgumentList != null)
			{
				foreach (var a in attribute.ArgumentList.Arguments)
				{
					if (a.NameEquals == null) continue;
					var ne = a.NameEquals;
					var id = ne.Name.Identifier;
					if (id.ValueText == "MethodName")
					{
						if (a.Expression is LiteralExpressionSyntax les
							&& les.Kind() == SyntaxKind.StringLiteralExpression)
						{
							return (
								name: les.Token.ValueText.Trim('"'),
								isPublic: true);
						}
						else
						{
							context.ReportDiagnostic(
								Diagnostic.Create(
									Diagnostics.MethodNameIsInvalidError,
									a.Expression.GetLocation()));
							valid = false;
						}
					}
				}
			}

			var name = $"__Expression_{methodName}";
			if (m is MethodDeclarationSyntax mds)
			{
				name = name + "_" + string.Join("_",
					mds.ParameterList.Parameters
						.Select(s => s.Type
							?.ToString()
							.Replace("<", "_")
							.Replace(">", "_")
							.Replace(",", "_")
							.Replace(" ", "")));
			}

			return (name, isPublic: false);
		}

		private static MemberDeclarationSyntax BuildContainingClass(MemberDeclarationSyntax parent, INamedTypeSymbol cls)
		{
			var syntax = (ClassDeclarationSyntax)cls.DeclaringSyntaxReferences.First().GetSyntax();
			parent = SyntaxFactory.ClassDeclaration(cls.Name)
				.WithMembers(SyntaxFactory.SingletonList(parent))
				.WithModifiers(syntax.Modifiers);
			return parent;
		}

		private static MethodDeclarationSyntax BuildMethod(SemanticModel model, ISymbol symbol, MemberDeclarationSyntax m, (string name, bool isPublic) newMethod)
		{
			var expression =
				m switch
				{
					PropertyDeclarationSyntax _pds => GetPropertyExpression(_pds),
					MethodDeclarationSyntax _mds => GetMethodExpression(_mds),
					_ => throw new InvalidOperationException("By this point, only PDS and MDS are possible... how did we get here?"),
				};

			var parameters = SyntaxFactory.ParameterList();
			if (!symbol.IsStatic)
			{
				var cType = symbol.ContainingType;
				parameters = parameters.AddParameters(
					SyntaxFactory.Parameter(
						SyntaxFactory.List<AttributeListSyntax>(),
						SyntaxFactory.TokenList(),
						SyntaxFactory.ParseTypeName(cType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
						SyntaxFactory.Identifier("@this").WithLeadingTrivia(SyntaxFactory.Space),
						default));

				var rewriter = new ThisRewriter(model, cType);
				expression = (ExpressionSyntax)rewriter.Visit(expression);
			}

			if (m is MethodDeclarationSyntax mds)
			{
				parameters = parameters.AddParameters(
					mds.ParameterList.Parameters.ToArray());
			}

			var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression()
				.WithParameterList(parameters)
				.WithExpressionBody(expression);

			var funcType =
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier("Func"),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SeparatedList(
								parameters.Parameters.Select(p => p.Type!))
							.Add(m switch
							{
								PropertyDeclarationSyntax _pds => _pds.Type,
								MethodDeclarationSyntax _mds => _mds.ReturnType,
								_ => throw new InvalidOperationException("By this point, only PDS and MDS are possible... how did we get here?"),
							})));

			var returnType =
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier("Expression"),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SingletonSeparatedList<TypeSyntax>(funcType)));

			var method = SyntaxFactory.MethodDeclaration(returnType, newMethod.name)
				.WithModifiers(SyntaxFactory.TokenList(
					SyntaxFactory.Token(newMethod.isPublic ? SyntaxKind.PublicKeyword : SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxFactory.Space),
					SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space)))
				.WithBody(SyntaxFactory.Block(
					SyntaxFactory.SingletonList(
						SyntaxFactory.ReturnStatement(lambdaExpression))));
			return method;
		}

		private static ExpressionSyntax GetPropertyExpression(PropertyDeclarationSyntax pds)
		{
			// arrow expression (int Property => 1;)
			if (pds.ExpressionBody != null)
				return pds.ExpressionBody.Expression;

			var getAccessor = pds.AccessorList!.Accessors
				.First(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
			// get arrow expression (int Property { get => 1; })
			if (getAccessor.ExpressionBody != null)
				return getAccessor.ExpressionBody.Expression;

			// get method (int Property { get { return 1; } })
			return GetExpressionFromReturn(getAccessor.Body!);
		}

		private static ExpressionSyntax GetMethodExpression(MethodDeclarationSyntax mds)
		{
			// arrow expression (int Method() => 1;)
			if (mds.ExpressionBody != null)
				return mds.ExpressionBody.Expression;

			// regular method (int Method() { return 1; }
			return GetExpressionFromReturn(mds.Body!);
		}

		private static ExpressionSyntax GetExpressionFromReturn(BlockSyntax blockSyntax)
		{
			var retStatement = (ReturnStatementSyntax)blockSyntax.Statements[0];
			return retStatement.Expression!;
		}

		private class ThisRewriter : CSharpSyntaxRewriter
		{
			private readonly SemanticModel model;
			private readonly ISymbol typeSymbol;

			public ThisRewriter(SemanticModel model, ISymbol typeSymbol)
			{
				this.model = model;
				this.typeSymbol = typeSymbol;
			}

			// don't rewrite left side of equals statement in cases of self-mapping
			public override SyntaxNode? VisitNameEquals(NameEqualsSyntax node) => node;

			// rewrite this.Member to @this.Member
			public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				// other ways to do this, this is simple
				if (node.Expression is ThisExpressionSyntax)
					return node.WithExpression(SyntaxFactory.IdentifierName("@this").WithLeadingTrivia(SyntaxFactory.Space));
				// if not this.Member, then don't check identifiers underneath
				return node;
			}

			public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
			{
				var nodeSymbol = model.GetSymbolInfo(node).Symbol;
				if (nodeSymbol == null) return node; // wtf??

				var nodeSymbolType = nodeSymbol.ContainingType;
				// check naked names, do they belong to the owning class? then add @this
				if (nodeSymbolType.Equals(typeSymbol, SymbolEqualityComparer.Default)
					&& !nodeSymbol.IsStatic)
					return SyntaxFactory.QualifiedName(
						SyntaxFactory.IdentifierName("@this"),
						node);
				else
					return node;
			}
		}
	}
}
