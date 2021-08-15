using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// AST builder class with helpers to create various AST nodes.
	/// </summary>
	public class CodeBuilder
	{
		public List<CodeFile> Files { get; } = new();

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
		/// Get <c>this</c> parameter reference.
		/// </summary>
		public ICodeExpression This => CodeThis.Instance;

		/// <summary>
		/// Add new file code unit.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <returns>File code-unit.</returns>
		public CodeFile File(string fileName)
		{
			var file = new CodeFile(fileName);
			Files.Add(file);
			return file;
		}

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
		public CodeImport Import(CodeIdentifier[] @namespace) => new(@namespace);

		/// <summary>
		/// Create namespace definition.
		/// </summary>
		/// <param name="name">Namespace name.</param>
		/// <returns>Namespace builder instance.</returns>
		public NamespaceBuilder Namespace(string name) => new (new(_languageProvider.TypeParser.ParseNamespace(name)));

		/// <summary>
		/// Create namespace definition.
		/// </summary>
		/// <param name="name">Namespace name.</param>
		/// <returns>Namespace builder instance.</returns>
		public NamespaceBuilder Namespace(CodeIdentifier[] name) => new(new (name));

		/// <summary>
		/// Create top-level or namespace-scoped class.
		/// </summary>
		/// <param name="namespace">Optional namespace.</param>
		/// <param name="name">Class name.</param>
		/// <returns>Class builder instance.</returns>
		public ClassBuilder Class(CodeIdentifier[]? @namespace, string name) => new(new (@namespace, new(name)));

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
		/// Create generic type descriptor from <see cref="System.Type"/> instance of open-generic type.
		/// </summary>
		/// <param name="type"><see cref="System.Type"/> instance of open generic type.</param>
		/// <param name="nullable">Type nullability.</param>
		/// <param name="typeArguments">Generic type arguments.</param>
		/// <returns>Type descriptor.</returns>
		public IType Type(Type type, bool nullable, params IType[] typeArguments)
		{
			if (!type.IsGenericTypeDefinition
				|| typeArguments.Length != type.GetGenericArguments().Length)
				throw new InvalidOperationException();

			return Type(type, nullable).WithTypeArguments(typeArguments);
		}

		/// <summary>
		/// Create type descriptor from type name.
		/// </summary>
		/// <param name="typeName">Type name to parse.</param>
		/// <param name="valueType">Indicate that type is value type of reference type.</param>
		/// <param name="typeArguments">Type arguments if <paramref name="typeName"/> defines open generic type.</param>
		/// <returns>Type descriptor.</returns>
		public IType Type(string typeName, bool valueType, params IType[] typeArguments)
		{
			var type = _languageProvider.TypeParser.Parse(typeName, valueType);

			if (typeArguments != null)
				type = type.WithTypeArguments(typeArguments);

			return type;
		}

		/// <summary>
		/// Create method call element.
		/// </summary>
		/// <param name="objOrType">Callee object or type in case of static method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Generic method type arguments.</param>
		/// <param name="parameters">Method parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCall Call(ICodeExpression objOrType, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters) => new(false, objOrType, method, genericArguments, parameters);

		/// <summary>
		/// Create extension method call.
		/// </summary>
		/// <param name="type">Type, containing extension method.</param>
		/// <param name="method">Called method name.</param>
		/// <param name="genericArguments">Type arguments for generic method.</param>
		/// <param name="parameters">Call parameters.</param>
		/// <returns>Call element instance.</returns>
		public CodeCall ExtCall(IType type, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters) => new(true, new CodeTypeReference(type), method, genericArguments, parameters);

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
		/// <returns>Parameter instance.</returns>
		public CodeParameter LambdaParameter(CodeIdentifier name) => new(null, name, ParameterDirection.In);

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
		/// <param name="memberName">Member name.</param>
		/// <returns>Member access expression.</returns>
		public CodeMember Member(ICodeExpression obj, CodeIdentifier memberName) => new(obj, memberName);

		/// <summary>
		/// Creates new object expression.
		/// </summary>
		/// <param name="type">Object type to create.</param>
		/// <param name="parameters">Constructor parameters.</param>
		/// <param name="initializers">Field/property initializers.</param>
		/// <returns>New object expression instance.</returns>
		public CodeNew New(IType type, ICodeExpression[] parameters, CodeAssignment[] initializers) => new(type, parameters, initializers);

		/// <summary>
		/// Creates throw expression/statement.
		/// </summary>
		/// <param name="exception">Exception object to throw.</param>
		/// <returns>Throw expression/statement instance.</returns>
		public CodeThrow Throw(ICodeExpression exception) => new(exception);

		/// <summary>
		/// Creates generic type parameter descriptor.
		/// </summary>
		/// <param name="name">Type parameter name.</param>
		/// <returns>Type parameter descriptor instance.</returns>
		public IType TypeParameter(CodeIdentifier name) => new TypeArgument(name, false);

		/// <summary>
		/// Creates assignment statement/expression.
		/// </summary>
		/// <param name="lvalue">Assignee.</param>
		/// <param name="rvalue">Assigned value.</param>
		/// <returns>Assignment statement/expression instance.</returns>
		public CodeAssignment Assign(ILValue lvalue, ICodeExpression rvalue) => new(lvalue, rvalue);

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
		/// <returns>Indexed access expression.</returns>
		public CodeIndex Index(ICodeExpression obj, ICodeExpression index) => new(obj, index);

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
		public CodeConstant Null(IType type, bool targetTyped) => new (type, (object?)null, targetTyped);

		/// <summary>
		/// Creates static member access expression (e.g. property or field accessor).
		/// </summary>
		/// <param name="type">Member owning type.</param>
		/// <param name="memberName">Member name.</param>
		/// <returns>Member access expression.</returns>
		public CodeMember Member(IType type, CodeIdentifier memberName) => new(type, memberName);

		/// <summary>
		/// Creates static member access expression (e.g. property or field accessor).
		/// </summary>
		/// <param name="type">Member owning type.</param>
		/// <param name="memberName">Member name.</param>
		/// <returns>Member access expression.</returns>
		public CodeMember Member(IType type, string memberName) => new(type, new(memberName));

		/// <summary>
		/// Creates lambda method builder.
		/// </summary>
		/// <returns>Lambda method builder instance.</returns>
		public LambdaMethodBuilder Lambda() => new(new());

		/// <summary>
		/// Creates code block builder.
		/// </summary>
		/// <returns>Code block builder instance.</returns>
		public BlockBuilder Block() => new(new());

		/// <summary>
		/// Creates <see cref="string"/> literal constant expression.
		/// </summary>
		/// <param name="value">String value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(string value, bool targetTyped) => new(Type(typeof(string), false), value, targetTyped);

		/// <summary>
		/// Creates delayed <see cref="string"/> literal constant expression.
		/// </summary>
		/// <param name="delayedValue">Delayed value provider.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(Func<string> delayedValue, bool targetTyped) => new(Type(typeof(string), false), delayedValue, targetTyped);

		/// <summary>
		/// Creates <see cref="bool"/> constant expression.
		/// </summary>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(bool value, bool targetTyped) => new(Type(typeof(bool), false), value, targetTyped);

		/// <summary>
		/// Creates <see cref="int"/> constant expression.
		/// </summary>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(int value, bool targetTyped) => new(Type(typeof(int), false), value, targetTyped);

		/// <summary>
		/// Creates <see cref="long"/> constant expression.
		/// </summary>
		/// <param name="value">Constant value.</param>
		/// <param name="targetTyped">Indicates that value is target-typed.</param>
		/// <returns>Constant expression instance.</returns>
		public CodeConstant Constant(long value, bool targetTyped) => new(Type(typeof(long), false), value, targetTyped);

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
		/// Creates nameof(<paramref name="type"/>) expression.
		/// </summary>
		/// <param name="type">Type used as nameof argument.</param>
		/// <returns>Nameof expression instance.</returns>
		public CodeNameOf NameOf(IType type)
		{
			if (type.Kind != TypeKind.Regular
				&& type.Kind != TypeKind.Generic
				&& type.Kind != TypeKind.TypeArgument)
				throw new InvalidOperationException();

			return new (new CodeTypeReference(type));
		}

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
		public CodeIdentifier Identifier(string name) => new(name);

		/// <summary>
		/// Creates identifier instance with invalid identifier fix parameters.
		/// </summary>
		/// <param name="name">Name to use as identifier.</param>
		/// <param name="fixOptions">Optional name fix instructions for if identifier name is not valid.</param>
		/// <param name="position">Optional identifier position (e.g. position of parameter for parameter name identifier) to use with invalid name fix options.</param>
		/// <returns>Identifier instance.</returns>
		public CodeIdentifier Identifier(string name, NameFixOptions? fixOptions, int? position) => new(name, fixOptions, position);
	}
}
