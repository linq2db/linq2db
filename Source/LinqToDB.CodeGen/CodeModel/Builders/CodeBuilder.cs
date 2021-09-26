using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// AST builder class with helpers to create various AST nodes.
	/// </summary>
	public class CodeBuilder
	{
		private readonly ILanguageProvider _languageProvider;

		public CodeBuilder(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
		}

		/// <summary>
		/// Get new line element.
		/// </summary>
		public ITopLevelElement NewLine => CodeEmptyLine.Instance;

		/// <summary>
		/// Add new file code unit.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <returns>File code-unit.</returns>
		public CodeFile File(string fileName) => new (fileName);

		/// <summary>
		/// Create code comment.
		/// </summary>
		/// <param name="text">Comment text.</param>
		/// <param name="inline">Comment type: inline or single-line.</param>
		/// <returns>Comment instance.</returns>
		public CodeComment Commentary(string text, bool inline) => new (text, inline);

		/// <summary>
		/// Create error compiler pragma.
		/// </summary>
		/// <param name="errorMessage">Error message.</param>
		/// <returns>Pragma instance.</returns>
		public CodePragma Error(string errorMessage) => new(PragmaType.Error, new[] { errorMessage });

		/// <summary>
		/// Create suppress warning(s) pragma.
		/// </summary>
		/// <param name="warnings">Warnings to suppress.</param>
		/// <returns>Pragma instance.</returns>
		public CodePragma DisableWarnings(params string[] warnings) => new(PragmaType.DisableWarning, warnings);

		/// <summary>
		/// Create NRT enable pragma.
		/// </summary>
		/// <returns>Pragma instance.</returns>
		public CodePragma EnableNullableReferenceTypes() => new (PragmaType.NullableEnable, System.Array.Empty<string>());

		/// <summary>
		/// Create import statement.
		/// </summary>
		/// <param name="namespace">Namespace to import.</param>
		/// <returns>Import statement.</returns>
		public CodeImport Import(IReadOnlyList<CodeIdentifier> @namespace) => new(@namespace);

		/// <summary>
		/// Create namespace definition.
		/// </summary>
		/// <param name="name">Namespace name.</param>
		/// <returns>Namespace builder instance.</returns>
		public NamespaceBuilder Namespace(string name) => new (new(_languageProvider.TypeParser.ParseNamespace(name)));

		/// <summary>
		/// Create one-dimensional array type.
		/// </summary>
		/// <param name="elementType">Array element type.</param>
		/// <param name="nullable">Type nullability.</param>
		/// <returns>Array type descriptor.</returns>
		public IType ArrayType(IType elementType, bool nullable) => new ArrayType(elementType, new int?[1], nullable);

		/// <summary>
		/// Create type descriptor from <see cref="System.Type"/> instance.
		/// </summary>
		/// <param name="type"><see cref="System.Type"/> instance.</param>
		/// <param name="nullable">Type nullability.</param>
		/// <returns>Type descriptor.</returns>
		public IType Type(Type type, bool nullable) => _languageProvider.TypeParser.Parse(type).WithNullability(nullable);

		/// <summary>
		/// Create method call expression.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression Call(ICodeExpression objOrType, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters, IType returnType) => new(false, objOrType, method, genericArguments, parameters, returnType);

		/// <summary>
		/// Create method call expression.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression Call(ICodeExpression objOrType, CodeIdentifier method, IType returnType, IType[] genericArguments, params ICodeExpression[] parameters) => new(false, objOrType, method, genericArguments, parameters, returnType);

		/// <summary>
		/// Create method call statement.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement Call(ICodeExpression objOrType, CodeIdentifier method, IType[] genericArguments, params ICodeExpression[] parameters) => new(false, objOrType, method, genericArguments, parameters);

		/// <summary>
		/// Create method call statement.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement Call(ICodeExpression objOrType, CodeIdentifier method) => new(false, objOrType, method, System.Array.Empty<IType>(), System.Array.Empty<ICodeExpression>());

		/// <summary>
		/// Create extension method call expression.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Type arguments for generic method.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression ExtCall(IType type, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters, IType returnType) => new(true, new CodeTypeReference(type), method, genericArguments, parameters, returnType);

		/// <summary>
		/// Create extension method call expression.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="genericArguments">Type arguments for generic method.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression ExtCall(IType type, CodeIdentifier method, IType returnType, IType[] genericArguments, params ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, genericArguments, parameters, returnType);

		/// <summary>
		/// Create method parameter.
		/// </summary>
		/// <param name="type">Parameter type.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="direction">Parameter direction.</param>
		/// <returns>Parameter element instance.</returns>
		public CodeParameter Parameter(IType type, CodeIdentifier name, ParameterDirection direction) => new (type, name, direction);

		/// <summary>
		/// Create default expression.
		/// </summary>
		/// <param name="type">Expression type.</param>
		/// <param name="targetTyped">Indicate that expression is target-typed and could ommit type name in generated code.</param>
		/// <returns>Default expression instance.</returns>
		public CodeDefault Default(IType type, bool targetTyped) => new(type, targetTyped);

		/// <summary>
		/// Creates lambda method parameter without explicit type.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="type">Parameter type.</param>
		/// <returns>Parameter instance.</returns>
		public CodeParameter LambdaParameter(CodeIdentifier name, IType type) => new(type, name, ParameterDirection.In);

		/// <summary>
		/// Creates return statement/expression.
		/// </summary>
		/// <param name="expression">Optional return value.</param>
		/// <returns>Return statement/expression instance.</returns>
		public CodeReturn Return(ICodeExpression? expression) => new(expression);

		/// <summary>
		/// Creates equality binary expression.
		/// </summary>
		/// <param name="left">Left-side argument.</param>
		/// <param name="right">Right-side argument.</param>
		/// <returns>Binary operation instance.</returns>
		public CodeBinary Equal(ICodeExpression left, ICodeExpression right) => new(left, BinaryOperation.Equal, right);

		/// <summary>
		/// Creates + binary expression.
		/// </summary>
		/// <param name="left">Left-side argument.</param>
		/// <param name="right">Right-side argument.</param>
		/// <returns>Binary operation instance.</returns>
		public CodeBinary Add(ICodeExpression left, ICodeExpression right) => new(left, BinaryOperation.Add, right);

		/// <summary>
		/// Creates logical AND binary operation.
		/// </summary>
		/// <param name="left">Left-side argument.</param>
		/// <param name="right">Right-side argument.</param>
		/// <returns>Binary operation instance.</returns>
		public CodeBinary And(ICodeExpression left, ICodeExpression right) => new(left, BinaryOperation.And, right);

		///// <summary>
		///// Creates member access expression (e.g. property or field accessor).
		///// </summary>
		///// <param name="obj">Member owner instance.</param>
		///// <param name="memberName">Member name.</param>
		///// <param name="memberType">Member type.</param>
		///// <returns>Member access expression.</returns>
		//public CodeMember Member(ICodeExpression obj, CodeIdentifier memberName, IType memberType) => new(obj, memberName, memberType);

		/// <summary>
		/// Creates member access expression (e.g. property or field accessor).
		/// </summary>
		/// <param name="obj">Member owner instance.</param>
		/// <param name="member">Member reference.</param>
		/// <returns>Member access expression.</returns>
		public CodeMember Member(ICodeExpression obj, CodeReference member) => new(obj, member);

		/// <summary>
		/// Creates property access expression.
		/// </summary>
		/// <param name="obj">Property owner instance.</param>
		/// <param name="property">Property node.</param>
		/// <returns>Property access expression.</returns>
		public CodeMember Member(ICodeExpression obj, CodeProperty property) => new(obj, property.Reference);

		/// <summary>
		/// Creates static property access expression.
		/// </summary>
		/// <param name="owner">Static property owner type.</param>
		/// <param name="property">Property node.</param>
		/// <returns>Property access expression.</returns>
		public CodeMember Member(IType owner, CodeProperty property) => new(owner, property.Reference);

		/// <summary>
		/// Creates field access expression.
		/// </summary>
		/// <param name="obj">Field owner instance.</param>
		/// <param name="field">Field node.</param>
		/// <returns>Field access expression.</returns>
		public CodeMember Member(ICodeExpression obj, CodeField field) => new(obj, field.Reference);

		/// <summary>
		/// Creates new object expression.
		/// </summary>
		/// <param name="type">Object type to create.</param>
		/// <param name="parameters">Constructor parameters.</param>
		/// <param name="initializers">Field/property initializers.</param>
		/// <returns>New object expression instance.</returns>
		public CodeNew New(IType type, ICodeExpression[] parameters, CodeAssignmentStatement[] initializers) => new(type, parameters, initializers);

		/// <summary>
		/// Creates new object expression.
		/// </summary>
		/// <param name="type">Object type to create.</param>
		/// <param name="parameters">Constructor parameters.</param>
		/// <returns>New object expression instance.</returns>
		public CodeNew New(IType type, params ICodeExpression[] parameters) => new(type, parameters, System.Array.Empty<CodeAssignmentStatement>());

		/// <summary>
		/// Creates throw statement.
		/// </summary>
		/// <param name="exception">Exception object to throw.</param>
		/// <returns>Throw statement instance.</returns>
		public CodeThrowStatement Throw(ICodeExpression exception) => new(exception);

		/// <summary>
		/// Creates generic type parameter descriptor.
		/// </summary>
		/// <param name="name">Type parameter name.</param>
		/// <returns>Type parameter descriptor instance.</returns>
		public IType TypeParameter(CodeIdentifier name) => new TypeArgument(name, false);

		/// <summary>
		/// Creates assignment statement.
		/// </summary>
		/// <param name="lvalue">Assignee.</param>
		/// <param name="rvalue">Assigned value.</param>
		/// <returns>Assignment statement/expression instance.</returns>
		public CodeAssignmentStatement Assign(ILValue lvalue, ICodeExpression rvalue) => new(lvalue, rvalue);

		/// <summary>
		/// Creates variable declaration.
		/// </summary>
		/// <param name="name">Variable name.</param>
		/// <param name="type">Variable type.</param>
		/// <param name="rvalueTyped">Indicates that variable is typed by assigned value and could ommit type name during code generation.</param>
		/// <returns>Variable declaration.</returns>
		public CodeVariable Variable(CodeIdentifier name, IType type, bool rvalueTyped) => new(name, type, rvalueTyped);

		/// <summary>
		/// Creates new array instance creation expression.
		/// </summary>
		/// <param name="type">Array element type.</param>
		/// <param name="valueTyped">Indicate that array type could be infered from values, so it is not necessary to generate array element type name.</param>
		/// <param name="values">Array values.</param>
		/// <param name="inline">Indicates that array should be generated on single line if possible.</param>
		/// <returns>Array instance creation expression.</returns>
		public CodeNewArray Array(IType type, bool valueTyped, ICodeExpression[] values, bool inline) => new(type, valueTyped, values, inline);

		/// <summary>
		/// Single-parameter indexed access expression.
		/// </summary>
		/// <param name="obj">Indexed object.</param>
		/// <param name="index">Index parameter.</param>
		/// <param name="returnType">Return value type.</param>
		/// <returns>Indexed access expression.</returns>
		public CodeIndex Index(ICodeExpression obj, ICodeExpression index, IType returnType) => new(obj, index, returnType);

		/// <summary>
		/// Creates type cast expression.
		/// </summary>
		/// <param name="type">Target type.</param>
		/// <param name="value">Casted value expression.</param>
		/// <returns>Cast expression.</returns>
		public CodeTypeCast Cast(IType type, ICodeExpression value) => new(type, value);

		/// <summary>
		/// Creates <c>null</c> constant.
		/// </summary>
		/// <param name="type">Type of constant.</param>
		/// <param name="targetTyped">Indicates that constant type could be inferred from context.</param>
		/// <returns>Constant expression.</returns>
#pragma warning disable IDE0004 // Remove Unnecessary Cast : https://github.com/dotnet/roslyn/issues/55621
		public CodeConstant Null(IType type, bool targetTyped) => new (type, (object?)null, targetTyped);
#pragma warning restore IDE0004 // Remove Unnecessary Cast

		///// <summary>
		///// Creates static property access expression.
		///// </summary>
		///// <param name="type">Property owner type.</param>
		///// <param name="property">Property.</param>
		///// <returns>Property access expression.</returns>
		//public CodeMember Member(IType type, CodeProperty property) => new(type, property.Name, property.Type.Type);

		/// <summary>
		/// Creates lambda method builder.
		/// </summary>
		/// <param name="lambdaType">Lambda method type (delegate).</param>
		/// <param name="ommitTypes">Lambda method could skip generation of types for parameters.</param>
		/// <returns>Lambda method builder instance.</returns>
		public LambdaMethodBuilder Lambda(IType lambdaType, bool ommitTypes) => new(new(lambdaType, ommitTypes));

		/// <summary>
		/// Creates <see cref="string"/> literal constant expression.
		/// </summary>
		/// <param name="value">String value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(string value, bool targetTyped) => new(WellKnownTypes.System.String, value, targetTyped);

		/// <summary>
		/// Creates delayed <see cref="string"/> literal constant expression.
		/// </summary>
		/// <param name="delayedValue">Delayed value provider.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(Func<string> delayedValue, bool targetTyped) => new(WellKnownTypes.System.String, delayedValue, targetTyped);

		/// <summary>
		/// Creates <see cref="bool"/> constant expression.
		/// </summary>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(bool value, bool targetTyped) => new(WellKnownTypes.System.Boolean, value, targetTyped);

		/// <summary>
		/// Creates <see cref="int"/> constant expression.
		/// </summary>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(int value, bool targetTyped) => new(WellKnownTypes.System.Int32, value, targetTyped);

		/// <summary>
		/// Creates enum constant expression of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Enum type.</typeparam>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant<T>(T value, bool targetTyped)
			where T : Enum
			=> new(Type(typeof(T), false), value, targetTyped);

		/// <summary>
		/// Creates nameof(<paramref name="member"/>) expression.
		/// </summary>
		/// <param name="member">Member access expression, used as nameof argument.</param>
		/// <returns>Nameof expression instance.</returns>
		public CodeNameOf NameOf(ICodeExpression member) => new(member);

		/// <summary>
		/// Creates identifier instance.
		/// </summary>
		/// <param name="name">Name to use as identifier.</param>
		/// <returns>Identifier instance.</returns>
		public CodeIdentifier Name(string name) => new(name);

		/// <summary>
		/// Creates identifier instance with invalid identifier fix parameters.
		/// </summary>
		/// <param name="name">Name to use as identifier.</param>
		/// <param name="fixOptions">Optional name fix instructions for if identifier name is not valid.</param>
		/// <param name="position">Optional identifier position (e.g. position of parameter for parameter name identifier) to use with invalid name fix options.</param>
		/// <returns>Identifier instance.</returns>
		public CodeIdentifier Name(string name, NameFixOptions? fixOptions, int? position) => new(name, fixOptions, position);
	}
}
