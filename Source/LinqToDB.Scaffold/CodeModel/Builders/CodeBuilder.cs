using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// AST builder class with helpers to create various AST nodes.
	/// </summary>
	public sealed class CodeBuilder
	{
		private readonly ILanguageProvider _languageProvider;

		internal CodeBuilder(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
		}

		/// <summary>
		/// Add new file code unit.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="imports">Using statements (imports).</param>
		/// <returns>File code-unit.</returns>
		public CodeFile File(string fileName, params CodeImport[] imports) => new (fileName, null, imports, null);

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
		public CodePragma EnableNullableReferenceTypes() => new (PragmaType.NullableEnable, []);

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
		public NamespaceBuilder Namespace(string name) => new (new(_languageProvider.TypeParser.ParseNamespaceOrRegularTypeName(name, true)));

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
		/// Create generic type descriptor from open generic type <paramref name="type"/> and type arguments <paramref name="typeArgs"/>.
		/// </summary>
		/// <param name="type">Open generic type.</param>
		/// <param name="nullable">Type nullability.</param>
		/// <param name="external">External or generated type.</param>
		/// <param name="typeArgs">Generic type arguments.</param>
		/// <returns>Type descriptor.</returns>
		public IType GenericType(IType type, bool nullable, bool external, params IType[] typeArgs) => type.Kind == TypeKind.OpenGeneric
			? new GenericType(type.Namespace, type.Name!, type.IsValueType, nullable, typeArgs, external)
			: throw new InvalidOperationException($"{type} is not open generic type");

		/// <summary>
		/// Create non-void generic method call expression.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="skipTypeArguments">Type arguments could be skipped on code-generation due to type inference.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression Call(ICodeExpression objOrType, CodeIdentifier method, IType returnType, IType[] genericArguments, bool skipTypeArguments, params ICodeExpression[] parameters) => new(false, objOrType, method, genericArguments, skipTypeArguments, parameters, null, returnType);

		/// <summary>
		/// Create non-void method call expression.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression Call(ICodeExpression objOrType, CodeIdentifier method, IType returnType, params ICodeExpression[] parameters) => new(false, objOrType, method, System.Array.Empty<IType>(), false, parameters, null, returnType);

		/// <summary>
		/// Create generic void method call statement.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="skipTypeArguments">Type arguments could be skipped on code-generation due to type inference.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement Call(ICodeExpression objOrType, CodeIdentifier method, IType[] genericArguments, bool skipTypeArguments, params ICodeExpression[] parameters) => new(false, objOrType, method, genericArguments, skipTypeArguments, parameters, null);

		/// <summary>
		/// Create void method call statement.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement Call(ICodeExpression objOrType, CodeIdentifier method, params ICodeExpression[] parameters) => new(false, objOrType, method, [], false, parameters, null);

		/// <summary>
		/// Create non-void generic extension method call expression.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="genericArguments">Type arguments for generic method.</param>
		/// <param name="skipTypeArguments">Type arguments could be skipped on code-generation due to type inference.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression ExtCall(IType type, CodeIdentifier method, IType returnType, IType[] genericArguments, bool skipTypeArguments, params ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, genericArguments, skipTypeArguments, parameters, null, returnType);

		/// <summary>
		/// Create non-void extension method call expression.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="returnType">Method return value type.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallExpression ExtCall(IType type, CodeIdentifier method, IType returnType, params ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, System.Array.Empty<IType>(), false, parameters, null, returnType);

		/// <summary>
		/// Create void generic extension method call statement.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="skipTypeArguments">Type arguments could be skipped on code-generation due to type inference.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement ExtCall(IType type, CodeIdentifier method, IType[] genericArguments, bool skipTypeArguments, params ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, genericArguments, skipTypeArguments, parameters, null);

		/// <summary>
		/// Create void extension method call statement.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCallStatement ExtCall(IType type, CodeIdentifier method, params ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, [], false, parameters, null);

		/// <summary>
		/// Create method parameter.
		/// </summary>
		/// <param name="type">Parameter type.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="direction">Parameter direction.</param>
		/// <param name="defaultValue">Parameter default value.</param>
		/// <returns>Parameter element instance.</returns>
		public CodeParameter Parameter(IType type, CodeIdentifier name, CodeParameterDirection direction, ICodeExpression? defaultValue = null) => new (type, name, direction, defaultValue);

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
		public CodeParameter LambdaParameter(CodeIdentifier name, IType type) => new(type, name, CodeParameterDirection.In, null);

		/// <summary>
		/// Creates return statement/expression.
		/// </summary>
		/// <param name="expression">Optional return value.</param>
		/// <returns>Return statement/expression instance.</returns>
		public CodeReturn Return(ICodeExpression? expression) => new(expression, null, null);

		/// <summary>
		/// Creates equality binary expression.
		/// </summary>
		/// <param name="left">Left-side argument.</param>
		/// <param name="right">Right-side argument.</param>
		/// <returns>Binary operation instance.</returns>
		public CodeBinary Equal(ICodeExpression left, ICodeExpression right) => new(left, BinaryOperation.Equal, right);

		/// <summary>
		/// Creates "+" binary expression.
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

		/// <summary>
		/// Creates member access expression (e.g. property or field accessor).
		/// </summary>
		/// <param name="obj">Member owner instance.</param>
		/// <param name="member">Member reference.</param>
		/// <returns>Member access expression.</returns>
		public CodeMember Member(ICodeExpression obj, CodeReference member) => new(obj, member);

		/// <summary>
		/// Creates static member access expression (e.g. property or field accessor).
		/// </summary>
		/// <param name="owner">Static property owner type.</param>
		/// <param name="member">Member reference.</param>
		/// <returns>Property access expression.</returns>
		public CodeMember Member(IType owner, CodeReference member) => new(owner, member);

		/// <summary>
		/// Creates new object expression.
		/// </summary>
		/// <param name="type">Object type to create.</param>
		/// <param name="parameters">Constructor parameters.</param>
		/// <param name="initializers">Field/property initializers.</param>
		/// <returns>New object expression instance.</returns>
		public CodeNew New(IType type, ICodeExpression[] parameters, params CodeAssignmentStatement[] initializers) => new(type, parameters, initializers);

		/// <summary>
		/// Creates new object expression.
		/// </summary>
		/// <param name="type">Object type to create.</param>
		/// <param name="parameters">Constructor parameters.</param>
		/// <returns>New object expression instance.</returns>
		public CodeNew New(IType type, params ICodeExpression[] parameters) => new(type, parameters, []);

		/// <summary>
		/// Creates throw statement.
		/// </summary>
		/// <param name="exception">Exception object to throw.</param>
		/// <returns>Throw statement instance.</returns>
		public CodeThrowStatement Throw(ICodeExpression exception) => new(exception, null, null);

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
		public CodeAssignmentStatement Assign(ILValue lvalue, ICodeExpression rvalue) => new(lvalue, rvalue, null, null);

		/// <summary>
		/// Creates await expression.
		/// </summary>
		/// <param name="task">Task expression to await.</param>
		/// <returns>Await expression instance.</returns>
		public CodeAwaitExpression AwaitExpression(ICodeExpression task) => new(task);

		/// <summary>
		/// Creates await statement.
		/// </summary>
		/// <param name="task">Task expression to await.</param>
		/// <returns>Await statement instance.</returns>
		public CodeAwaitStatement AwaitStatement(ICodeExpression task) => new(task, null, null);

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
		public CodeNewArray Array(IType type, bool valueTyped, bool inline, params ICodeExpression[] values) => new(type, valueTyped, values, inline);

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
		/// Creates type conversion expression using <c>as</c> operator.
		/// </summary>
		/// <param name="type">Target type.</param>
		/// <param name="expression">Casted value expression.</param>
		/// <returns><c>as</c> operator expression.</returns>
		public CodeAsOperator As(IType type, ICodeExpression expression) => new(type, expression);

		/// <summary>
		/// Creates <see langword="null"/> constant.
		/// </summary>
		/// <param name="type">Type of constant.</param>
		/// <param name="targetTyped">Indicates that constant type could be inferred from context.</param>
		/// <returns>Constant expression.</returns>
#pragma warning disable IDE0004 // Remove Unnecessary Cast : https://github.com/dotnet/roslyn/issues/55621
		public CodeConstant Null(IType type, bool targetTyped) => new (type, (object?)null, targetTyped);
#pragma warning restore IDE0004 // Remove Unnecessary Cast

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
		public CodeIdentifier Name(string name) => new(name, false);

		/// <summary>
		/// Creates identifier instance with invalid identifier fix parameters.
		/// </summary>
		/// <param name="name">Name to use as identifier.</param>
		/// <param name="fixOptions">Optional name fix instructions for if identifier name is not valid.</param>
		/// <param name="position">Optional identifier position (e.g. position of parameter for parameter name identifier) to use with invalid name fix options.</param>
		/// <returns>Identifier instance.</returns>
		public CodeIdentifier Name(string name, NameFixOptions? fixOptions, int? position) => new(name, fixOptions, position);

		/// <summary>
		/// Generate null-forgiving operator (!).
		/// </summary>
		/// <param name="value">Expression to apply operator to.</param>
		/// <returns>Operator expression.</returns>
		public CodeSuppressNull SuppressNull(ICodeExpression value) => new (value);

		/// <summary>
		/// Creates ternary expression.
		/// </summary>
		/// <param name="condition">Condition expression.</param>
		/// <param name="true">Condition true value.</param>
		/// <param name="false">Condition false value.</param>
		/// <returns>Ternary expression instance.</returns>
		public CodeTernary IIF(ICodeExpression condition, ICodeExpression @true, ICodeExpression @false) => new(condition, @true, @false);
	}
}
