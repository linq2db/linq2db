using System;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class NoopCodeModelVisitor: CodeModelVisitor
	{
		protected override void Visit(CodeElementImport import)
		{
		}

		protected override void Visit(CodeElementPragma pragma)
		{
		}

		protected override void Visit(CodeFile file)
		{
			VisitList(file);
		}

		protected override void Visit(PropertyGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(MethodGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(ConstructorGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(RegionGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(ClassGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(NewExpression expression)
		{
			Visit(expression.Type);
			VisitList(expression.Parameters);
			VisitList(expression.Initializers);
		}

		protected override void Visit(AssignExpression expression)
		{
			Visit(expression.LValue);
			Visit(expression.RValue);
		}

		protected override void Visit(CodeBinaryExpression expression)
		{
			Visit(expression.Left);
			Visit(expression.Right);
		}

		protected override void Visit(LambdaMethod method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);
			if (method.Body != null)
				VisitList(method.Body);
			VisitList(method.Parameters);
			VisitList(method.CustomAttributes);
		}

		protected override void Visit(CodeMemberExpression expression)
		{
			if (expression.Type != null)
				Visit(expression.Type);
			if (expression.Object != null)
				Visit(expression.Object);
			Visit(expression.Member);
		}

		protected override void Visit(NameOfExpression nameOf)
		{
			Visit(nameOf.Expression);
		}

		protected override void Visit(CodeRegion region)
		{
			VisitList(region.Members);
		}

		protected override void Visit(CodeConstant constant)
		{
			Visit(constant.Type);
		}

		protected override void Visit(CodeAttribute attribute)
		{
			Visit(attribute.Type);
			VisitList(attribute.Parameters);
			foreach (var (prop, value) in attribute.NamedParameters)
			{
				Visit(prop);
				Visit(value);
			}
		}

		protected override void Visit(CodeElementComment comment)
		{
		}

		protected override void Visit(CodeElementEmptyLine line)
		{
		}

		protected override void Visit(CodeMethod method)
		{
			Visit(method.Name!);
			if (method.ReturnType != null)
				Visit(method.ReturnType);

			if (method.XmlDoc != null)
				Visit(method.XmlDoc);
			VisitList(method.CustomAttributes);
			VisitList(method.TypeParameters);
			VisitList(method.Parameters);
			if (method.Body != null)
				VisitList(method.Body);
		}

		protected override void Visit(CodeParameter parameter)
		{
			if (parameter.Type != null)
				Visit(parameter.Type);
			Visit(parameter.Name);
		}

		protected override void Visit(CodeXmlComment doc)
		{
			foreach (var (param, _) in doc.Parameters)
				Visit(param);
		}

		protected override void Visit(CodeConstructor ctor)
		{
			if (ctor.XmlDoc != null)
				Visit(ctor.XmlDoc);
			if (ctor.Body != null)
				VisitList(ctor.Body);
			VisitList(ctor.BaseArguments);
			VisitList(ctor.Parameters);
			VisitList(ctor.CustomAttributes);
		}

		protected override void Visit(CodeThisExpression expression)
		{
		}

		protected override void Visit(CodeCallExpression call)
		{
			Visit(call.MethodName);
			if (call.Callee != null)
				Visit(call.Callee);
			VisitList(call.TypeArguments);
			VisitList(call.Parameters);
		}

		protected override void Visit(ReturnStatement statement)
		{
			if (statement.Expression != null)
				Visit(statement.Expression);
		}

		protected override void Visit(CodeProperty property)
		{
			Visit(property.Name!);
			Visit(property.Type!);
			if (property.Getter != null)
				VisitList(property.Getter);
			if (property.Setter != null)
				VisitList(property.Setter);
			if (property.TrailingComment != null)
				Visit(property.TrailingComment);
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);
			VisitList(property.CustomAttributes);
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			VisitList(@namespace.Name);
			VisitList(@namespace.Members);
		}

		protected override void Visit(CodeClass @class)
		{
			Visit(@class.Name);

			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);

			if (@class.Inherits != null)
				Visit(@class.Inherits);

			VisitList(@class.Implements);

			VisitList(@class.Members);
			VisitList(@class.CustomAttributes);
		}

		protected override void Visit(CodeIdentifier identifier)
		{
		}

		protected override void Visit(TypeReference type)
		{
		}

		protected override void Visit(TypeToken type)
		{
		}

		protected override void Visit(FieldGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(PragmaGroup group)
		{
			VisitList(group.Members);
		}

		protected override void Visit(CodeField field)
		{
			Visit(field.Type);
			Visit(field.Name);
			if (field.Setter != null)
				Visit(field.Setter);
		}

		protected override void Visit(CodeDefault expression)
		{
			Visit(expression.Type);
		}

		protected override void Visit(VariableExpression expression)
		{
			Visit(expression.Type);
			Visit(expression.Name);
		}

		protected override void Visit(ArrayExpression expression)
		{
			Visit(expression.Type);
			VisitList(expression.Values);
		}

		protected override void Visit(ThrowExpression expression)
		{
			Visit(expression.Exception);
		}

		protected override void Visit(IndexExpression expression)
		{
			Visit(expression.Object);
			Visit(expression.Index);
		}

		protected override void Visit(CastExpression expression)
		{
			Visit(expression.Type);
			Visit(expression.Value);
		}
	}
}
