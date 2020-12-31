using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace LinqToDB.Analyzers
{
	internal static class Diagnostics
	{
		internal static readonly DiagnosticDescriptor ClassIsNotPartialError =
			new DiagnosticDescriptor(
				id: "LDBGEN001",
				title: "Containing class is not marked 'partial'",
				messageFormat: "Class '{0}' must be marked partial",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodReturnsVoidError =
			new DiagnosticDescriptor(
				id: "LDBGEN002",
				title: "Method has incorrect return type",
				messageFormat: "Method '{0}' must not return void",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodHasIncorrectShapeError =
			new DiagnosticDescriptor(
				id: "LDBGEN003",
				title: "Method has too many statements",
				messageFormat: "Method '{0}' must consist of a single return statement",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor PropertyDoesNotHaveGetWarning =
			new DiagnosticDescriptor(
				id: "LDBGEN004",
				title: "Property does not have a get accessor",
				messageFormat: "GenerateExpressionMethod applied to property '{0}', but it does not have a get accessor. No expression method will be generated for this property.",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Warning,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodNameIsInvalidError =
			new DiagnosticDescriptor(
				id: "LDBGEN005",
				title: "GenerateExpressionMethod Argument 'MethodName' is invalid",
				messageFormat: "The MethodName argument for the GenerateExpressionMethod attribute must be a simple string",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Warning,
				isEnabledByDefault: true);
	}
}
