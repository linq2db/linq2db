namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for AST visitors with default implementation for all nodes.
	/// </summary>
	public abstract class NoopCodeModelVisitor: CodeModelVisitor
	{
		protected override void Visit(CodeImport import        ) { }
		protected override void Visit(CodePragma pragma        ) { }
		protected override void Visit(CodeComment comment      ) { }
		protected override void Visit(CodeEmptyLine line       ) { }
		protected override void Visit(CodeIdentifier identifier) { }
		protected override void Visit(CodeTypeReference type   ) { }
		protected override void Visit(CodeTypeToken type       ) { }
		protected override void Visit(CodeThis expression      ) { }

		protected override void Visit(CodeFile file)
		{
			VisitList(file.Header);
			VisitList(file.Imports);
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

		protected override void Visit(CodeNew expression)
		{
			Visit(expression.Type);
			VisitList(expression.Parameters);
			VisitList(expression.Initializers);
		}

		protected override void Visit(CodeAssignmentStatement statement)
		{
			Visit(statement.LValue);
			Visit(statement.RValue);
		}

		protected override void Visit(CodeAssignmentExpression expression)
		{
			Visit(expression.LValue);
			Visit(expression.RValue);
		}

		protected override void Visit(CodeAwaitStatement statement)
		{
			Visit(statement.Task);
		}

		protected override void Visit(CodeAwaitExpression expression)
		{
			Visit(expression.Task);
		}

		protected override void Visit(CodeUnary expression)
		{
			Visit(expression.Argument);
		}

		protected override void Visit(CodeBinary expression)
		{
			Visit(expression.Left);
			Visit(expression.Right);
		}

		protected override void Visit(CodeTernary expression)
		{
			Visit(expression.Condition);
			Visit(expression.True);
			Visit(expression.False);
		}

		protected override void Visit(CodeLambda method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);

			VisitList(method.CustomAttributes);

			VisitList(method.Parameters);

			if (method.Body != null)
				VisitList(method.Body);
		}

		protected override void Visit(CodeMember expression)
		{
			if (expression.Type != null)
				Visit(expression.Type);

			if (expression.Instance != null)
				Visit(expression.Instance);

			Visit(expression.Member);
		}

		protected override void Visit(CodeNameOf nameOf)
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

		protected override void Visit(CodeMethod method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);

			VisitList(method.CustomAttributes);

			if (method.ReturnType != null)
				Visit(method.ReturnType);

			Visit(method.Name);

			VisitList(method.TypeParameters);
			VisitList(method.Parameters);

			if (method.Body != null)
				VisitList(method.Body);
		}

		protected override void Visit(CodeParameter parameter)
		{
			if (parameter.Type         != null) Visit(parameter.Type);
			if (parameter.DefaultValue != null) Visit(parameter.DefaultValue);

			Visit(parameter.Name);
		}

		protected override void Visit(CodeXmlComment doc)
		{
			foreach (var (param, _) in doc.Parameters)
				Visit(param);
		}

		protected override void Visit(CodeTypeInitializer cctor)
		{
			if (cctor.XmlDoc != null)
				Visit(cctor.XmlDoc);

			VisitList(cctor.CustomAttributes);

			if (cctor.Body != null)
				VisitList(cctor.Body);
		}

		protected override void Visit(CodeConstructor ctor)
		{
			if (ctor.XmlDoc != null)
				Visit(ctor.XmlDoc);

			VisitList(ctor.CustomAttributes);
			VisitList(ctor.Parameters);
			VisitList(ctor.BaseArguments);

			if (ctor.Body != null)
				VisitList(ctor.Body);
		}

		protected override void Visit(CodeCallStatement call)
		{
			Visit(call.Callee);
			Visit(call.MethodName);
			VisitList(call.TypeArguments);
			VisitList(call.Parameters);
		}

		protected override void Visit(CodeCallExpression call)
		{
			Visit(call.Callee);
			Visit(call.MethodName);
			VisitList(call.TypeArguments);
			VisitList(call.Parameters);
		}

		protected override void Visit(CodeReturn statement)
		{
			if (statement.Expression != null)
				Visit(statement.Expression);
		}

		protected override void Visit(CodeProperty property)
		{
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);

			VisitList(property.CustomAttributes);
			Visit(property.Type);
			Visit(property.Name);

			if (property.Getter != null)
				VisitList(property.Getter);

			if (property.Setter != null)
				VisitList(property.Setter);

			if (property.Initializer != null)
				Visit(property.Initializer);

			if (property.TrailingComment != null)
				Visit(property.TrailingComment);
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			VisitList(@namespace.Name);
			VisitList(@namespace.Members);
		}

		protected override void Visit(CodeClass @class)
		{
			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);

			VisitList(@class.CustomAttributes);
			Visit(@class.Name);

			if (@class.Inherits != null)
				Visit(@class.Inherits);

			VisitList(@class.Implements);

			if (@class.TypeInitializer != null)
				Visit(@class.TypeInitializer);

			VisitList(@class.Members);
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

			if (field.Initializer != null)
				Visit(field.Initializer);
		}

		protected override void Visit(CodeDefault expression)
		{
			Visit(expression.Type);
		}

		protected override void Visit(CodeVariable expression)
		{
			Visit(expression.Type);
			Visit(expression.Name);
		}

		protected override void Visit(CodeNewArray expression)
		{
			Visit(expression.Type);
			VisitList(expression.Values);
		}

		protected override void Visit(CodeThrowStatement statement)
		{
			Visit(statement.Exception);
		}

		protected override void Visit(CodeThrowExpression expression)
		{
			Visit(expression.Exception);
		}

		protected override void Visit(CodeIndex expression)
		{
			Visit(expression.Object);
			Visit(expression.Index);
		}

		protected override void Visit(CodeTypeCast expression)
		{
			Visit(expression.Type);
			Visit(expression.Value);
		}

		protected override void Visit(CodeAsOperator expression)
		{
			Visit(expression.Value);
			Visit(expression.Type);
		}

		protected override void Visit(CodeSuppressNull expression)
		{
			Visit(expression.Value);
		}

		protected override void Visit(CodeReference reference)
		{
			Visit(reference.Referenced.Name);
			Visit(reference.Referenced.Type);
		}
	}
}
