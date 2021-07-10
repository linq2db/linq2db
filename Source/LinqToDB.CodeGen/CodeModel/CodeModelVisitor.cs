using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeModelVisitor
	{
		private List<ICodeElement> _stack = new();
		protected ICodeElement? Parent => _stack.Count > 1 ? _stack[_stack.Count - 2] : null;

		public void Visit(ICodeElement node)
		{
			_stack.Add(node);
			switch (node.ElementType)
			{
				case CodeElementType.Namespace:
					Visit((CodeElementNamespace)node);
					break;
				case CodeElementType.Identifier:
					Visit((CodeIdentifier)node);
					break;
				case CodeElementType.Class:
					Visit((CodeClass)node);
					break;
				case CodeElementType.Property:
					Visit((CodeProperty)node);
					break;
				case CodeElementType.ReturnStatement:
					Visit((ReturnStatement)node);
					break;
				case CodeElementType.Call:
					Visit((CodeCallExpression)node);
					break;
				case CodeElementType.This:
					Visit((CodeThisExpression)node);
					break;
				case CodeElementType.Constructor:
					Visit((CodeConstructor)node);
					break;
				case CodeElementType.XmlComment:
					Visit((CodeXmlComment)node);
					break;
				case CodeElementType.TypeReference:
					Visit((TypeReference)node);
					break;
				case CodeElementType.Type:
					Visit((TypeToken)node);
					break;
				case CodeElementType.Parameter:
					Visit((CodeParameter)node);
					break;
				case CodeElementType.Method:
					Visit((CodeMethod)node);
					break;
				case CodeElementType.EmptyLine:
					Visit((CodeElementEmptyLine)node);
					break;
				case CodeElementType.Comment:
					Visit((CodeElementComment)node);
					break;
				case CodeElementType.Attribute:
					Visit((CodeAttribute)node);
					break;
				case CodeElementType.Constant:
					Visit((CodeConstant)node);
					break;
				case CodeElementType.Region:
					Visit((CodeRegion)node);
					break;
				case CodeElementType.NameOf:
					Visit((NameOfExpression)node);
					break;
				case CodeElementType.MemberAccess:
					Visit((CodeMemberExpression)node);
					break;
				case CodeElementType.Lambda:
					Visit((LambdaMethod)node);
					break;
				case CodeElementType.BinaryOperation:
					Visit((CodeBinaryExpression)node);
					break;
				case CodeElementType.File:
					Visit((CodeFile)node);
					break;
				case CodeElementType.Pragma:
					Visit((CodeElementPragma)node);
					break;
				case CodeElementType.Import:
					Visit((CodeElementImport)node);
					break;
				case CodeElementType.PropertyGroup:
					Visit((PropertyGroup)node);
					break;
				case CodeElementType.MethodGroup:
					Visit((MethodGroup)node);
					break;
				case CodeElementType.ConstructorGroup:
					Visit((ConstructorGroup)node);
					break;
				case CodeElementType.RegionGroup:
					Visit((RegionGroup)node);
					break;
				case CodeElementType.Assignment:
					Visit((AssignExpression)node);
					break;
				case CodeElementType.New:
					Visit((NewExpression)node);
					break;
				case CodeElementType.ClassGroup:
					Visit((ClassGroup)node);
					break;
				case CodeElementType.FieldGroup:
					Visit((FieldGroup)node);
					break;
				case CodeElementType.PragmaGroup:
					Visit((PragmaGroup)node);
					break;
				case CodeElementType.Field:
					Visit((CodeField)node);
					break;
				case CodeElementType.Default:
					Visit((CodeDefault)node);
					break;
				case CodeElementType.Variable:
					Visit((VariableExpression)node);
					break;
				case CodeElementType.Array:
					Visit((ArrayExpression)node);
					break;
				case CodeElementType.Index:
					Visit((IndexExpression)node);
					break;
				case CodeElementType.Cast:
					Visit((CastExpression)node);
					break;
				case CodeElementType.Throw:
					Visit((ThrowExpression)node);
					break;
				default: throw new NotImplementedException($"{node.ElementType}");
			}
			_stack.RemoveAt(_stack.Count - 1);
		}

		protected abstract void Visit(PropertyGroup group);
		protected abstract void Visit(MethodGroup group);
		protected abstract void Visit(ConstructorGroup group);
		protected abstract void Visit(RegionGroup group);
		protected abstract void Visit(ClassGroup group);
		protected abstract void Visit(FieldGroup group);
		protected abstract void Visit(PragmaGroup group);

		protected abstract void Visit(CastExpression expression);
		protected abstract void Visit(ThrowExpression expression);
		protected abstract void Visit(VariableExpression expression);
		protected abstract void Visit(ArrayExpression expression);
		protected abstract void Visit(IndexExpression expression);
		protected abstract void Visit(CodeField field);
		protected abstract void Visit(CodeDefault expression);
		protected abstract void Visit(NewExpression expression);
		protected abstract void Visit(AssignExpression expression);
		protected abstract void Visit(CodeElementImport import);
		protected abstract void Visit(CodeElementPragma pragma);
		protected abstract void Visit(CodeFile file);
		protected abstract void Visit(CodeBinaryExpression expression);
		protected abstract void Visit(LambdaMethod method);
		protected abstract void Visit(CodeMemberExpression expression);
		protected abstract void Visit(NameOfExpression nameOf);
		protected abstract void Visit(CodeRegion region);
		protected abstract void Visit(CodeConstant constant);
		protected abstract void Visit(CodeAttribute attribute);
		protected abstract void Visit(CodeElementComment comment);
		protected abstract void Visit(CodeElementEmptyLine line);
		protected abstract void Visit(CodeMethod method);
		protected abstract void Visit(CodeParameter parameter);
		protected abstract void Visit(CodeXmlComment doc);
		protected abstract void Visit(CodeConstructor ctor);
		protected abstract void Visit(CodeThisExpression expression);
		protected abstract void Visit(CodeCallExpression call);
		protected abstract void Visit(ReturnStatement statement);
		protected abstract void Visit(CodeProperty property);
		protected abstract void Visit(CodeElementNamespace @namespace);
		protected abstract void Visit(CodeClass @class);
		protected abstract void Visit(CodeIdentifier identifier);
		protected abstract void Visit(TypeReference type);
		protected abstract void Visit(TypeToken type);

		protected void VisitList<T>(CodeElementList<T> list)
			where T: ICodeElement
		{
			VisitList(list.Items);
		}

		protected void VisitList<T>(IEnumerable<T> list)
			where T : ICodeElement
		{
			foreach (var item in list)
				Visit(item);
		}
	}
}
