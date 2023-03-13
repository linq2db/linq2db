using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base AST rewrite visitor class with noop node visit methods implementation with root-to-leaf visit order.
	/// Each node could be replaced with any other node type and this is visitor implementor's responsibility
	/// to ensure new node type is compatible with node owner.
	/// Otherwise parent node visit method will generate type cast exception trying to consume incompatible child node.
	/// Important note: node visitors should visit only child nodes.
	/// </summary>
	public abstract class ConvertCodeModelVisitor
	{
		private readonly Dictionary<ICodeElement, ICodeElement> _replacements = new ();
		/// <summary>
		/// Main dispatch method.
		/// </summary>
		/// <param name="node">Node to visit.</param>
		/// <returns>Returns new node if node were replaced or same node otherwise.</returns>
		public ICodeElement Visit(ICodeElement node)
		{
			ICodeElement newNode;
			switch (node.ElementType)
			{
				case CodeElementType.Namespace           : newNode = Visit((CodeNamespace           )node); break;
				case CodeElementType.Identifier          : newNode = Visit((CodeIdentifier          )node); break;
				case CodeElementType.Class               : newNode = ConvertClass((CodeClass        )node); break;
				case CodeElementType.Property            : newNode = ConvertProperty((CodeProperty  )node); break;
				case CodeElementType.ReturnStatement     : newNode = Visit((CodeReturn              )node); break;
				case CodeElementType.CallStatement       : newNode = Visit((CodeCallStatement       )node); break;
				case CodeElementType.CallExpression      : newNode = Visit((CodeCallExpression      )node); break;
				case CodeElementType.This                : newNode = Visit((CodeThis                )node); break;
				case CodeElementType.Constructor         : newNode = Visit((CodeConstructor         )node); break;
				case CodeElementType.TypeConstructor     : newNode = Visit((CodeTypeInitializer     )node); break;
				case CodeElementType.XmlComment          : newNode = Visit((CodeXmlComment          )node); break;
				case CodeElementType.TypeReference       : newNode = Visit((CodeTypeReference       )node); break;
				case CodeElementType.TypeToken           : newNode = Visit((CodeTypeToken           )node); break;
				case CodeElementType.Parameter           : newNode = ConvertParameter((CodeParameter)node); break;
				case CodeElementType.Method              : newNode = ConvertMethod((CodeMethod      )node); break;
				case CodeElementType.EmptyLine           : newNode = Visit((CodeEmptyLine           )node); break;
				case CodeElementType.Comment             : newNode = Visit((CodeComment             )node); break;
				case CodeElementType.Attribute           : newNode = Visit((CodeAttribute           )node); break;
				case CodeElementType.Constant            : newNode = Visit((CodeConstant            )node); break;
				case CodeElementType.Region              : newNode = Visit((CodeRegion              )node); break;
				case CodeElementType.NameOf              : newNode = Visit((CodeNameOf              )node); break;
				case CodeElementType.MemberAccess        : newNode = Visit((CodeMember              )node); break;
				case CodeElementType.Lambda              : newNode = Visit((CodeLambda              )node); break;
				case CodeElementType.UnaryOperation      : newNode = Visit((CodeUnary               )node); break;
				case CodeElementType.BinaryOperation     : newNode = Visit((CodeBinary              )node); break;
				case CodeElementType.TernaryOperation    : newNode = Visit((CodeTernary             )node); break;
				case CodeElementType.File                : newNode = Visit((CodeFile                )node); break;
				case CodeElementType.Pragma              : newNode = Visit((CodePragma              )node); break;
				case CodeElementType.Import              : newNode = Visit((CodeImport              )node); break;
				case CodeElementType.PropertyGroup       : newNode = Visit((PropertyGroup           )node); break;
				case CodeElementType.MethodGroup         : newNode = Visit((MethodGroup             )node); break;
				case CodeElementType.ConstructorGroup    : newNode = Visit((ConstructorGroup        )node); break;
				case CodeElementType.RegionGroup         : newNode = Visit((RegionGroup             )node); break;
				case CodeElementType.AssignmentStatement : newNode = Visit((CodeAssignmentStatement )node); break;
				case CodeElementType.AssignmentExpression: newNode = Visit((CodeAssignmentExpression)node); break;
				case CodeElementType.AwaitStatement      : newNode = Visit((CodeAwaitStatement      )node); break;
				case CodeElementType.AwaitExpression     : newNode = Visit((CodeAwaitExpression     )node); break;
				case CodeElementType.New                 : newNode = Visit((CodeNew                 )node); break;
				case CodeElementType.ClassGroup          : newNode = Visit((ClassGroup              )node); break;
				case CodeElementType.FieldGroup          : newNode = Visit((FieldGroup              )node); break;
				case CodeElementType.PragmaGroup         : newNode = Visit((PragmaGroup             )node); break;
				case CodeElementType.Field               : newNode = Visit((CodeField               )node); break;
				case CodeElementType.Default             : newNode = Visit((CodeDefault             )node); break;
				case CodeElementType.Variable            : newNode = Visit((CodeVariable            )node); break;
				case CodeElementType.Array               : newNode = Visit((CodeNewArray            )node); break;
				case CodeElementType.Index               : newNode = Visit((CodeIndex               )node); break;
				case CodeElementType.Cast                : newNode = Visit((CodeTypeCast            )node); break;
				case CodeElementType.AsOperator          : newNode = Visit((CodeAsOperator          )node); break;
				case CodeElementType.SuppressNull        : newNode = Visit((CodeSuppressNull        )node); break;
				case CodeElementType.ThrowStatement      : newNode = Visit((CodeThrowStatement      )node); break;
				case CodeElementType.ThrowExpression     : newNode = Visit((CodeThrowExpression     )node); break;
				case CodeElementType.Reference           : newNode = Visit((CodeReference           )node); break;
				default                                  : throw new NotImplementedException($"{node.ElementType}");
			}

			if (node != newNode)
				_replacements.Add(node, newNode);

			return newNode;
		}

		#region change-tracking clone
		private ICodeElement ConvertClass(CodeClass node)
		{
			var converted = Visit(node);

			if (node.ChangeHandler != null && converted != node && converted is CodeClass newNode)
			{
				newNode.ChangeHandler = node.ChangeHandler;
				newNode.ChangeHandler.Invoke(newNode);
			}

			return converted;
		}

		private ICodeElement ConvertProperty(CodeProperty node)
		{
			var converted = Visit(node);

			if (node.ChangeHandler != null && converted != node && converted is CodeProperty newNode)
			{
				newNode.ChangeHandler = node.ChangeHandler;
				newNode.ChangeHandler.Invoke(newNode);
			}

			return converted;
		}

		private ICodeElement ConvertMethod(CodeMethod node)
		{
			var converted = Visit(node);

			if (node.ChangeHandler != null && converted != node && converted is CodeMethod newNode)
			{
				newNode.ChangeHandler = node.ChangeHandler;
				newNode.ChangeHandler.Invoke(newNode);
			}

			return converted;
		}

		private ICodeElement ConvertParameter(CodeParameter node)
		{
			var converted = Visit(node);

			if (node.ChangeHandler != null && converted != node && converted is CodeParameter newNode)
			{
				newNode.ChangeHandler = node.ChangeHandler;
				newNode.ChangeHandler.Invoke(newNode);
			}

			return converted;
		}
		#endregion

		#region Node Visitors
		protected virtual ICodeElement Visit(PropertyGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new PropertyGroup(members, group.TableLayout);

			return group;
		}

		protected virtual ICodeElement Visit(MethodGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new MethodGroup(members, group.TableLayout);

			return group;
		}

		protected virtual ICodeElement Visit(ConstructorGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new ConstructorGroup(members, group.Class);

			return group;
		}

		protected virtual ICodeElement Visit(RegionGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new RegionGroup(members, group.OwnerType);

			return group;
		}

		protected virtual ICodeElement Visit(ClassGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new ClassGroup(members, group.Owner);

			return group;
		}

		protected virtual ICodeElement Visit(FieldGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new FieldGroup(members, group.TableLayout);

			return group;
		}

		protected virtual ICodeElement Visit(PragmaGroup group)
		{
			var members = VisitList(group.Members);

			if (members != group.Members)
				return new PragmaGroup(members);

			return group;
		}

		protected virtual ICodeElement Visit(CodeTypeCast expression)
		{
			var value = (ICodeExpression)Visit(expression.Value);

			if (value != expression.Value)
				return new CodeTypeCast(expression.Type, value);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeAsOperator expression)
		{
			var value = (ICodeExpression)Visit(expression.Value);

			if (value != expression.Value)
				return new CodeAsOperator(expression.Type, value);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeSuppressNull expression)
		{
			var value = (ICodeExpression)Visit(expression.Value);

			if (value != expression.Value)
				return new CodeSuppressNull(value);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeThrowStatement statement)
		{
			var exception = (ICodeExpression)Visit(statement.Exception);

			if (exception != statement.Exception)
				return new CodeThrowStatement(exception, ((ICodeStatement)statement).Before, ((ICodeStatement)statement).After);

			return statement;
		}

		protected virtual ICodeElement Visit(CodeThrowExpression expression)
		{
			var exception = (ICodeExpression)Visit(expression.Exception);

			if (exception != expression.Exception)
				return new CodeThrowExpression(exception, ((ICodeExpression)expression).Type);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeVariable expression)
		{
			var name = (CodeIdentifier)Visit(expression.Name);

			if (name != expression.Name)
				return new CodeVariable(name, expression.Type, expression.RValueTyped);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeNewArray expression)
		{
			var values = VisitReadOnlyList(expression.Values);

			if (values != expression.Values)
				return new CodeNewArray(expression.Type, expression.ValueTyped, values, expression.Inline);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeIndex expression)
		{
			var idx = (ICodeExpression)Visit(expression.Index);

			if (idx != expression.Index)
				return new CodeIndex(expression.Object, idx, expression.ReturnType);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeField field)
		{
			var name        = (CodeIdentifier)                             Visit(field.Name       );
			var initializer = field.Initializer != null ? (ICodeExpression)Visit(field.Initializer) : null;

			if (name        != field.Name ||
				initializer != field.Initializer)
				return new CodeField(name, field.Type, field.Attributes, initializer);

			return field;
		}

		protected virtual ICodeElement Visit(CodeNew expression)
		{
			var parameters   = VisitReadOnlyList(expression.Parameters  );
			var initializers = VisitReadOnlyList(expression.Initializers);

			if (parameters   != expression.Parameters ||
				initializers != expression.Initializers)
				return new CodeNew(expression.Type, parameters, initializers);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeAssignmentStatement statement)
		{
			var rvalue = (ICodeExpression)Visit(statement.RValue);

			if (rvalue != statement.RValue)
				return new CodeAssignmentStatement(statement.LValue, rvalue, ((ICodeStatement)statement).Before, ((ICodeStatement)statement).After);

			return statement;
		}

		protected virtual ICodeElement Visit(CodeAssignmentExpression expression)
		{
			var rvalue = (ICodeExpression)Visit(expression.RValue);

			if (rvalue != expression.RValue)
				return new CodeAssignmentExpression(expression.LValue, rvalue);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeAwaitStatement statement)
		{
			var task = (ICodeExpression)Visit(statement.Task);

			if (task != statement.Task)
				return new CodeAwaitStatement(task, ((ICodeStatement)statement).Before, ((ICodeStatement)statement).After);

			return statement;
		}

		protected virtual ICodeElement Visit(CodeAwaitExpression expression)
		{
			var task = (ICodeExpression)Visit(expression.Task);

			if (task != expression.Task)
				return new CodeAwaitExpression(task);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeFile file)
		{
			var nameSource = file.NameSource;
			var header     = VisitList(file.Header );
			var imports    = VisitList(file.Imports);
			var items      = VisitList(file.Items  );

			if (header  != file.Header  ||
				imports != file.Imports ||
				items   != file.Items)
				file = new CodeFile(file.FileName, header, imports, items);

			if (nameSource != null && _replacements.TryGetValue(nameSource, out var newNameSource))
				nameSource = (CodeIdentifier)newNameSource;

			file.NameSource = nameSource;

			return file;
		}

		protected virtual ICodeElement Visit(CodeUnary expression)
		{
			var argument = (ICodeExpression)Visit(expression.Argument);

			if (argument != expression.Argument)
				return new CodeUnary(argument, expression.Operation);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeBinary expression)
		{
			var left  = (ICodeExpression)Visit(expression.Left );
			var right = (ICodeExpression)Visit(expression.Right);

			if (left  != expression.Left ||
				right != expression.Right)
				return new CodeBinary(left, expression.Operation, right);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeTernary expression)
		{
			var condition = (ICodeExpression)Visit(expression.Condition);
			var @true     = (ICodeExpression)Visit(expression.True     );
			var @false    = (ICodeExpression)Visit(expression.False    );

			if (condition != expression.Condition ||
				@true     != expression.True      ||
				@false    != expression.False)
				return new CodeTernary(condition, @true, @false);

			return expression;
		}

		protected virtual ICodeElement Visit(CodeLambda method)
		{
			var customAttributes =                                         VisitList     (method.CustomAttributes);
			var body             = method.Body   != null ?                 VisitCodeBlock(method.Body            ) : null;
			var xmlDoc           = method.XmlDoc != null ? (CodeXmlComment)Visit         (method.XmlDoc          ) : null;
			var parameters       =                                         VisitList     (method.Parameters      );

			if (customAttributes != method.CustomAttributes ||
				body             != method.Body             ||
				xmlDoc           != method.XmlDoc           ||
				parameters       != method.Parameters)
				return new CodeLambda(customAttributes, method.Attributes, body, xmlDoc, parameters, method.TargetType, method.CanOmmitTypes);

			return method;
		}

		protected virtual ICodeElement Visit(CodeMethod method)
		{
			var customAttributes =                                             VisitList     (method.CustomAttributes);
			var body             = method.Body   != null     ?                 VisitCodeBlock(method.Body            ) : null;
			var xmlDoc           = method.XmlDoc != null     ? (CodeXmlComment)Visit         (method.XmlDoc          ) : null;
			var parameters       =                                             VisitList     (method.Parameters      );
			var name             =                             (CodeIdentifier)Visit         (method.Name            );
			var typeParameters   =                                             VisitList     (method.TypeParameters  );

			if (customAttributes != method.CustomAttributes ||
				body             != method.Body             ||
				xmlDoc           != method.XmlDoc           ||
				parameters       != method.Parameters       ||
				name             != method.Name             ||
				typeParameters   != method.TypeParameters)
				return new CodeMethod(customAttributes, method.Attributes, body, xmlDoc, parameters, name, method.ReturnType, typeParameters);

			return method;
		}

		protected virtual ICodeElement Visit(CodeConstructor ctor)
		{
			var customAttributes =                                       VisitList     (ctor.CustomAttributes);
			var body             = ctor.Body   != null ?                 VisitCodeBlock(ctor.Body            ) : null;
			var xmlDoc           = ctor.XmlDoc != null ? (CodeXmlComment)Visit         (ctor.XmlDoc          ) : null;
			var parameters       =                                       VisitList     (ctor.Parameters      );
			var baseArguments    =                                       VisitList     (ctor.BaseArguments   );

			if (customAttributes != ctor.CustomAttributes ||
				body             != ctor.Body             ||
				xmlDoc           != ctor.XmlDoc           ||
				parameters       != ctor.Parameters       ||
				baseArguments    != ctor.BaseArguments)
				return new CodeConstructor(customAttributes, ctor.Attributes, body, xmlDoc, parameters, ctor.Class, ctor.ThisCall, baseArguments);

			return ctor;
		}

		protected virtual ICodeElement Visit(CodeProperty property)
		{
			var customAttributes =                                                     VisitList     (property.CustomAttributes);
			var name             =                                    (CodeIdentifier )Visit         (property.Name            );
			var getter           = property.Getter          != null ?                  VisitCodeBlock(property.Getter          ) : null;
			var setter           = property.Setter          != null ?                  VisitCodeBlock(property.Setter          ) : null;
			var trailingComment  = property.TrailingComment != null ? (CodeComment    )Visit         (property.TrailingComment ) : null;
			var xmlDoc           = property.XmlDoc          != null ? (CodeXmlComment )Visit         (property.XmlDoc          ) : null;
			var initializer      = property.Initializer     != null ? (ICodeExpression)Visit         (property.Initializer     ) : null;

			if (customAttributes != property.CustomAttributes ||
				name             != property.Name             ||
				getter           != property.Getter           ||
				setter           != property.Setter           ||
				trailingComment  != property.TrailingComment  ||
				xmlDoc           != property.XmlDoc           ||
				initializer      != property.Initializer)
				return new CodeProperty(customAttributes, name, property.Type, property.Attributes, property.HasGetter, getter, property.HasSetter, setter, trailingComment, xmlDoc, initializer);

			return property;
		}

		protected virtual ICodeElement Visit(CodeClass @class)
		{
			var customAttributes =                                                       VisitList(@class.CustomAttributes);
			var xmlDoc           = @class.XmlDoc          != null ? (CodeXmlComment     )Visit    (@class.XmlDoc          ) : null;
			var name             =                                  (CodeIdentifier     )Visit    (@class.Name            );
			var members          =                                                       VisitList(@class.Members         );
			var typeInitializer  = @class.TypeInitializer != null ? (CodeTypeInitializer)Visit    (@class.TypeInitializer ) : null;

			if (customAttributes != @class.CustomAttributes ||
				xmlDoc           != @class.XmlDoc           ||
				name             != @class.Name             ||
				members          != @class.Members          ||
				typeInitializer  != @class.TypeInitializer)
				return new CodeClass(customAttributes, @class.Attributes, xmlDoc, @class.Type, name, @class.Parent, @class.Inherits, @class.Implements, members, typeInitializer);

			return @class;
		}

		protected virtual ICodeElement Visit(CodeNameOf nameOf)
		{
			var expression = (ICodeExpression)Visit(nameOf.Expression);

			if (expression != nameOf.Expression)
				return new CodeNameOf(expression);

			return nameOf;
		}

		protected virtual ICodeElement Visit(CodeRegion region)
		{
			var members = VisitList(region.Members);

			if (members != region.Members)
				return new CodeRegion(region.Type, region.Name, members);

			return region;
		}

		protected virtual ICodeElement Visit(CodeAttribute attribute)
		{
			var parameters      = VisitList(attribute.Parameters);
			var namedParameters = VisitList(attribute.NamedParameters, np =>
			{
				var value = (ICodeExpression)Visit(np.Value);

				if (value != np.Value)
					return new (np.Property, value);

				return np;
			});

			if (parameters      != attribute.Parameters ||
				namedParameters != attribute.NamedParameters)
				return new CodeAttribute(attribute.Type, parameters, namedParameters);

			return attribute;
		}

		protected virtual ICodeElement Visit(CodeParameter parameter)
		{
			var name         =                                  (CodeIdentifier )Visit(parameter.Name        );
			var defaultValue = parameter.DefaultValue != null ? (ICodeExpression)Visit(parameter.DefaultValue) : null;

			if (name         != parameter.Name ||
				defaultValue != parameter.DefaultValue)
				return new CodeParameter(parameter.Type, name, parameter.Direction, defaultValue);

			return parameter;
		}

		protected virtual ICodeElement Visit(CodeTypeInitializer cctor)
		{
			var customAttributes =                                        VisitList     (cctor.CustomAttributes);
			var body             = cctor.Body   != null ?                 VisitCodeBlock(cctor.Body            ) : null;
			var xmlDoc           = cctor.XmlDoc != null ? (CodeXmlComment)Visit         (cctor.XmlDoc          ) : null;
			var parameters       =                                        VisitList     (cctor.Parameters      );

			if (customAttributes != cctor.CustomAttributes ||
				body             != cctor.Body             ||
				xmlDoc           != cctor.XmlDoc           ||
				parameters       != cctor.Parameters)
				return new CodeTypeInitializer(customAttributes, cctor.Attributes, body, xmlDoc, parameters, cctor.Type);

			return cctor;
		}

		protected virtual ICodeElement Visit(CodeCallStatement call)
		{
			var parameters = VisitReadOnlyList(call.Parameters);

			if (parameters != call.Parameters)
				return new CodeCallStatement(call.Extension, call.Callee, call.MethodName, call.TypeArguments, call.CanSkipTypeArguments, parameters, call.WrapTrivia, ((ICodeStatement)call).Before, ((ICodeStatement)call).After);

			return call;
		}

		protected virtual ICodeElement Visit(CodeCallExpression call)
		{
			var parameters = VisitReadOnlyList(call.Parameters);

			if (parameters != call.Parameters)
				return new CodeCallExpression(call.Extension, call.Callee, call.MethodName, call.TypeArguments, call.CanSkipTypeArguments, parameters, call.WrapTrivia, call.ReturnType);

			return call;
		}

		protected virtual ICodeElement Visit(CodeReturn statement)
		{
			var expression = statement.Expression != null ? (ICodeExpression)Visit(statement.Expression) : null;

			if (expression != statement.Expression)
				return new CodeReturn(expression, ((ICodeStatement)statement).Before, ((ICodeStatement)statement).After);

			return statement;
		}

		protected virtual ICodeElement Visit(CodeNamespace @namespace)
		{
			var name    = VisitReadOnlyList(@namespace.Name   );
			var members = VisitList        (@namespace.Members);

			if (name    != @namespace.Name ||
				members != @namespace.Members)
				return new CodeNamespace(name, members);

			return @namespace;
		}

		protected virtual ICodeElement Visit(CodeComment       comment   ) => comment;
		protected virtual ICodeElement Visit(CodeEmptyLine     line      ) => line;
		protected virtual ICodeElement Visit(CodeTypeReference type      ) => type;
		protected virtual ICodeElement Visit(CodeTypeToken     type      ) => type;
		protected virtual ICodeElement Visit(CodeIdentifier    identifier) => identifier;
		protected virtual ICodeElement Visit(CodeDefault       expression) => expression;
		protected virtual ICodeElement Visit(CodeMember        expression) => expression;
		protected virtual ICodeElement Visit(CodeConstant      constant  ) => constant;
		protected virtual ICodeElement Visit(CodeXmlComment    doc       ) => doc;
		protected virtual ICodeElement Visit(CodeThis          expression) => expression;
		protected virtual ICodeElement Visit(CodeImport        import    ) => import;
		protected virtual ICodeElement Visit(CodePragma        pragma    ) => pragma;
		protected virtual ICodeElement Visit(CodeReference     reference ) => reference;
		#endregion

		/// <summary>
		/// Visit nodes collection.
		/// </summary>
		/// <typeparam name="TElement">Node type.</typeparam>
		/// <param name="members">Nodes collection.</param>
		/// <returns>Original collection of nodes not changed or new collection if any node was replaced with new one.</returns>
		private IReadOnlyList<TElement> VisitList<TElement>(IReadOnlyList<TElement> members)
			where TElement : ICodeElement
		{
			List<TElement>? newMembers = null;

			for (var i = 0; i < members.Count; i++)
			{
				var newMember = (TElement)Visit(members[i]);

				if (newMembers != null || !newMember.Equals(members[i]))
				{
					if (newMembers == null)
					{
						newMembers = new List<TElement>(members.Count);

						for (var j = 0; j < i; j++)
							newMembers.Add(members[j]);
					}

					newMembers.Add(newMember);
				}
			}

			return newMembers ?? members;
		}

		/// <summary>
		/// Visit collection of custom objects.
		/// </summary>
		/// <typeparam name="TElement">Collection element type.</typeparam>
		/// <param name="members">Collection.</param>
		/// <param name="converter">Collection element converter.</param>
		/// <returns>Original collection not changed or new collection if any element was replaced with new one.</returns>
		private IReadOnlyList<TElement> VisitList<TElement>(IReadOnlyList<TElement> members, Func<TElement, TElement> converter)
			where TElement: notnull
		{
			List<TElement>? newMembers = null;

			for (var i = 0; i < members.Count; i++)
			{
				var newMember = converter(members[i]);

				if (newMembers != null || !newMember.Equals(members[i]))
				{
					if (newMembers == null)
					{
						newMembers = new List<TElement>(members.Count);

						for (var j = 0; j < i; j++)
							newMembers.Add(members[j]);
					}

					newMembers.Add(newMember);
				}
			}

			return newMembers ?? members;
		}

		/// <summary>
		/// Visit nodes collection.
		/// </summary>
		/// <typeparam name="TElement">Node type.</typeparam>
		/// <param name="members">Nodes collection.</param>
		/// <returns>Original collection of nodes not changed or new collection if any node was replaced with new one.</returns>
		private IReadOnlyList<TElement> VisitReadOnlyList<TElement>(IReadOnlyList<TElement> members)
			where TElement : ICodeElement
		{
			TElement[]? newMembers = null;

			for (var i = 0; i < members.Count; i++)
			{
				var newMember = (TElement)Visit(members[i]);

				if (newMembers != null || !newMember.Equals(members[i]))
				{
					if (newMembers == null)
					{
						newMembers = new TElement[members.Count];

						for (var j = 0; j < i; j++)
							newMembers[j] = members[j];
					}

					newMembers[i] = newMember;
				}
			}

			return newMembers ?? members;
		}

		/// <summary>
		/// Visit <see cref="CodeBlock"/> pseudo-node.
		/// </summary>
		/// <param name="block"><see cref="CodeBlock"/> instance.</param>
		/// <returns>New <see cref="CodeBlock"/> if it's content moditifer or same block.</returns>
		private CodeBlock VisitCodeBlock(CodeBlock block)
		{
			var items = VisitList(block.Items);

			if (items != block.Items)
				return new CodeBlock(items);

			return block;
		}
	}
}
