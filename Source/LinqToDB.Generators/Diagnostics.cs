using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace LinqToDB.Generators
{
	internal static class Diagnostics
	{
		internal static readonly DiagnosticDescriptor ClassIsNotPartialError =
			new DiagnosticDescriptor(
				id: "LDBGEN001",
				title: "Containing class is not marked 'partial'",
				messageFormat: "Class '{0}' must be marked partial in order to build expression methods in generated files",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodHasIncorrectParameterCount =
			new DiagnosticDescriptor(
				id: "LDBGEN002",
				title: "Method has too many parameters",
				messageFormat: "Method '{0}' must have exactly one parameter",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodReturnsVoid =
			new DiagnosticDescriptor(
				id: "LDBGEN003",
				title: "Method has incorrect return type",
				messageFormat: "Method '{0}' must not return void",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor MethodHasIncorrectShape =
			new DiagnosticDescriptor(
				id: "LDBGEN004",
				title: "Method has too many statements",
				messageFormat: "Method '{0}' must consist of a single return statement",
				category: "LinqToDB.ExpressionMethodGenerator",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true);
	}
}
