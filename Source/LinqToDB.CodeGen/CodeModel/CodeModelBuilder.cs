using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeModelBuilder
	{
		public List<CodeFile> Files { get; } = new();

		private readonly ILanguageServices _langServices;

		public CodeModelBuilder(ILanguageServices langServices)
		{
			_langServices = langServices;
		}

		public CodeFile File(string fileName, string folder)
		{
			var file = new CodeFile(fileName, folder);
			Files.Add(file);
			return file;
		}

		public CodeElementComment Commentary(string text, bool inline)
		{
			return new CodeElementComment(text, inline);
		}

		public CodeElementEmptyLine NewLine()
		{
			return CodeElementEmptyLine.Instance;
		}

		public CodeElementPragma Error(string errorMessage)
		{
			return new CodeElementPragma(PragmaType.Error, new[] { errorMessage });
		}

		public CodeElementPragma DisableWarnings(params string[] warnings)
		{
			return new CodeElementPragma(PragmaType.DisableWarning, warnings);
		}

		public CodeElementPragma EnableNullableReferenceTypes()
		{
			return new CodeElementPragma(PragmaType.NullableEnable, System.Array.Empty<string>());
		}

		public CodeElementImport Import(CodeIdentifier[] @namespace)
		{
			return new CodeElementImport(@namespace);
		}

		public NamespaceBuilder Namespace(string name)
		{
			var parts = name.Split('.');
			var identifiers = new CodeIdentifier[parts.Length];

			for (var i = 0; i < identifiers.Length; i++)
				identifiers[i] = new CodeIdentifier(parts[i]);
			return new NamespaceBuilder(new CodeElementNamespace(identifiers));
		}

		public ClassBuilder Class(CodeIdentifier[]? @namespace, string name)
		{
			var identifier = new CodeIdentifier(name);
			return new ClassBuilder(new CodeClass(@namespace, identifier));
		}

		public IType Type(string typeName, bool nullable)
		{
			throw new NotImplementedException();
		}

		public IType Type(IType type, bool nullable)
		{
			return type.WithNullability(nullable);
		}

		public IType ArrayType(IType type, bool nullable)
		{
			return new ArrayType(type, new int?[1], nullable);
		}

		public IType Type(Type type, bool nullable)
		{
			return TypeBuilder.FromType(type, _langServices).WithNullability(nullable);
		}

		public IType Type(Type type, bool nullable, params IType[] typeArguments)
		{
			if (!type.IsGenericTypeDefinition
				|| typeArguments.Length != type.GetGenericArguments().Length)
				throw new InvalidOperationException();

			return Type(type, nullable).WithTypeArguments(typeArguments);
		}

		public IType Type(string typeName, bool valueType, params IType[] typeArguments)
		{
			var type = TypeBuilder.ParseType(_langServices, typeName, valueType);
			if (typeArguments != null)
				type = type.WithTypeArguments(typeArguments);

			return type;
		}

		public CodeThisExpression This() => CodeThisExpression.Instance;

		public CodeCallExpression Call(ICodeExpression? objOrType, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters)
		{
			return new CodeCallExpression(false, objOrType, method, genericArguments, parameters);
		}

		public CodeCallExpression ExtCall(IType type, CodeIdentifier method, IType[] genericArguments, ICodeExpression[] parameters)
		{
			return new CodeCallExpression(true, new TypeReference(type), method, genericArguments, parameters);
		}

		public CodeParameter Parameter(IType type, CodeIdentifier name, Direction direction)
		{
			return new CodeParameter(type, name, direction);
		}

		public CodeDefault Default(IType type, bool targetTyped)
		{
			return new CodeDefault(type, targetTyped);
		}

		public CodeParameter LambdaParameter(CodeIdentifier name)
		{
			return new CodeParameter(null, name, Direction.In);
		}

		public ReturnStatement Return(ICodeExpression? expression)
		{
			return new ReturnStatement(expression);
		}

		public CodeBinaryExpression Equal(ICodeExpression left, ICodeExpression right)
		{
			return new CodeBinaryExpression(left, BinaryOperation.Equal, right);
		}

		public CodeBinaryExpression And(ICodeExpression left, ICodeExpression right)
		{
			return new CodeBinaryExpression(left, BinaryOperation.And, right);
		}

		public CodeMemberExpression Member(ICodeExpression obj, CodeIdentifier memberName)
		{
			return new CodeMemberExpression(obj, memberName);
		}

		public NewExpression New(IType type, ICodeExpression[] parameters, AssignExpression[] initializers)
		{
			return new NewExpression(type, parameters, initializers);
		}

		public ThrowExpression Throw(ICodeExpression exception)
		{
			return new ThrowExpression(exception);
		}

		public IType TypeParameter(CodeIdentifier name)
		{
			return new TypeArgument(name, false);
		}

		public AssignExpression Assign(ILValue lvalue, ICodeExpression rvalue)
		{
			return new AssignExpression(lvalue, rvalue);
		}

		public VariableExpression Variable(CodeIdentifier name, IType type, bool rvalueTyped)
		{
			return new VariableExpression(name, type, rvalueTyped);
		}

		public ArrayExpression Array(IType type, bool valueTyped, ICodeExpression[] values, bool inline)
		{
			return new ArrayExpression(type, valueTyped, values, inline);
		}

		public IndexExpression Index(ICodeExpression obj, ICodeExpression index)
		{
			return new IndexExpression(obj, index);
		}

		public CastExpression Cast(IType type, ICodeExpression value)
		{
			return new CastExpression(type, value);
		}

		public CodeConstant Null(IType type, bool targetTyped)
		{
			return new CodeConstant(type, null, targetTyped);
		}

		public CodeMemberExpression Member(IType type, CodeIdentifier memberName)
		{
			return new CodeMemberExpression(type, memberName);
		}

		public CodeMemberExpression Member(IType type, string memberName)
		{
			return new CodeMemberExpression(type, new CodeIdentifier(memberName));
		}

		public LambdaMethodBuilder Lambda()
		{
			var lambda = new LambdaMethod();
			return new LambdaMethodBuilder(lambda);
		}

		public CodeBlockBuilder Block()
		{
			return new CodeBlockBuilder(new CodeBlock());
		}

		public CodeConstant Constant(string value, bool targetTyped)
		{
			return new CodeConstant(Type(typeof(string), false), value, targetTyped);
		}

		public CodeConstant Constant(bool value, bool targetTyped)
		{
			return new CodeConstant(Type(typeof(bool), false), value, targetTyped);
		}

		public CodeConstant Constant(int value, bool targetTyped)
		{
			return new CodeConstant(Type(typeof(int), false), value, targetTyped);
		}

		public CodeConstant Constant(long value, bool targetTyped)
		{
			return new CodeConstant(Type(typeof(long), false), value, targetTyped);
		}

		public CodeConstant Constant<T>(T value, bool targetTyped)
			where T: Enum
		{
			return new CodeConstant(Type(typeof(T), false), value, targetTyped);
		}

		public NameOfExpression NameOf(IType type)
		{
			if (type.Kind != TypeKind.Regular
				&& type.Kind != TypeKind.Generic
				&& type.Kind != TypeKind.TypeArgument)
				throw new InvalidOperationException();

			return new NameOfExpression(new TypeReference(type));
		}

		public NameOfExpression NameOf(ICodeExpression member)
		{
			return new NameOfExpression(member);
		}

		public CodeIdentifier Identifier(string name)
		{
			return new CodeIdentifier(name);
		}

		public CodeIdentifier Identifier(string name, BadNameFixOptions? fixOptions, int? position)
		{
			return new CodeIdentifier(name, fixOptions, position);
		}
	}
}
