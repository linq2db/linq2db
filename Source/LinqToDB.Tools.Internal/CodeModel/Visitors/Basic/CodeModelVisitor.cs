using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base AST visitor class without node visit methods implementation.
	/// </summary>
	public abstract class CodeModelVisitor
	{
		/// <summary>
		/// Visited nodes in-depth stack with current node on top.
		/// </summary>
		private List<ICodeElement> _stack = new();

		/// <summary>
		/// Parent node or <c>null</c> if visitor at top level node position.
		/// </summary>
		protected ICodeElement? Parent => _stack.Count > 1 ? _stack[_stack.Count - 2] : null;

		/// <summary>
		/// Main dispatch method.
		/// </summary>
		/// <param name="node">Node to visit.</param>
		public void Visit(ICodeElement node)
		{
			_stack.Add(node);

			switch (node.ElementType)
			{
				case CodeElementType.Namespace           : Visit((CodeNamespace           )node); break;
				case CodeElementType.Identifier          : Visit((CodeIdentifier          )node); break;
				case CodeElementType.Class               : Visit((CodeClass               )node); break;
				case CodeElementType.Property            : Visit((CodeProperty            )node); break;
				case CodeElementType.ReturnStatement     : Visit((CodeReturn              )node); break;
				case CodeElementType.CallStatement       : Visit((CodeCallStatement       )node); break;
				case CodeElementType.CallExpression      : Visit((CodeCallExpression      )node); break;
				case CodeElementType.This                : Visit((CodeThis                )node); break;
				case CodeElementType.Constructor         : Visit((CodeConstructor         )node); break;
				case CodeElementType.TypeConstructor     : Visit((CodeTypeInitializer     )node); break;
				case CodeElementType.XmlComment          : Visit((CodeXmlComment          )node); break;
				case CodeElementType.TypeReference       : Visit((CodeTypeReference       )node); break;
				case CodeElementType.TypeToken           : Visit((CodeTypeToken           )node); break;
				case CodeElementType.Parameter           : Visit((CodeParameter           )node); break;
				case CodeElementType.Method              : Visit((CodeMethod              )node); break;
				case CodeElementType.EmptyLine           : Visit((CodeEmptyLine           )node); break;
				case CodeElementType.Comment             : Visit((CodeComment             )node); break;
				case CodeElementType.Attribute           : Visit((CodeAttribute           )node); break;
				case CodeElementType.Constant            : Visit((CodeConstant            )node); break;
				case CodeElementType.Region              : Visit((CodeRegion              )node); break;
				case CodeElementType.NameOf              : Visit((CodeNameOf              )node); break;
				case CodeElementType.MemberAccess        : Visit((CodeMember              )node); break;
				case CodeElementType.Lambda              : Visit((CodeLambda              )node); break;
				case CodeElementType.UnaryOperation      : Visit((CodeUnary               )node); break;
				case CodeElementType.BinaryOperation     : Visit((CodeBinary              )node); break;
				case CodeElementType.TernaryOperation    : Visit((CodeTernary             )node); break;
				case CodeElementType.File                : Visit((CodeFile                )node); break;
				case CodeElementType.Pragma              : Visit((CodePragma              )node); break;
				case CodeElementType.Import              : Visit((CodeImport              )node); break;
				case CodeElementType.PropertyGroup       : Visit((PropertyGroup           )node); break;
				case CodeElementType.MethodGroup         : Visit((MethodGroup             )node); break;
				case CodeElementType.ConstructorGroup    : Visit((ConstructorGroup        )node); break;
				case CodeElementType.RegionGroup         : Visit((RegionGroup             )node); break;
				case CodeElementType.AssignmentStatement : Visit((CodeAssignmentStatement )node); break;
				case CodeElementType.AssignmentExpression: Visit((CodeAssignmentExpression)node); break;
				case CodeElementType.AwaitStatement      : Visit((CodeAwaitStatement      )node); break;
				case CodeElementType.AwaitExpression     : Visit((CodeAwaitExpression     )node); break;
				case CodeElementType.New                 : Visit((CodeNew                 )node); break;
				case CodeElementType.ClassGroup          : Visit((ClassGroup              )node); break;
				case CodeElementType.FieldGroup          : Visit((FieldGroup              )node); break;
				case CodeElementType.PragmaGroup         : Visit((PragmaGroup             )node); break;
				case CodeElementType.Field               : Visit((CodeField               )node); break;
				case CodeElementType.Default             : Visit((CodeDefault             )node); break;
				case CodeElementType.Variable            : Visit((CodeVariable            )node); break;
				case CodeElementType.Array               : Visit((CodeNewArray            )node); break;
				case CodeElementType.Index               : Visit((CodeIndex               )node); break;
				case CodeElementType.Cast                : Visit((CodeTypeCast            )node); break;
				case CodeElementType.AsOperator          : Visit((CodeAsOperator          )node); break;
				case CodeElementType.SuppressNull        : Visit((CodeSuppressNull        )node); break;
				case CodeElementType.ThrowStatement      : Visit((CodeThrowStatement      )node); break;
				case CodeElementType.ThrowExpression     : Visit((CodeThrowExpression     )node); break;
				case CodeElementType.Reference           : Visit((CodeReference           )node); break;

				default                                  : throw new NotImplementedException($"{node.ElementType}");
			}

			_stack.RemoveAt(_stack.Count - 1);
		}

		protected abstract void Visit(PropertyGroup            group     );
		protected abstract void Visit(MethodGroup              group     );
		protected abstract void Visit(ConstructorGroup         group     );
		protected abstract void Visit(RegionGroup              group     );
		protected abstract void Visit(ClassGroup               group     );
		protected abstract void Visit(FieldGroup               group     );
		protected abstract void Visit(PragmaGroup              group     );
		protected abstract void Visit(CodeTypeCast             expression);
		protected abstract void Visit(CodeAsOperator           expression);
		protected abstract void Visit(CodeSuppressNull         expression);
		protected abstract void Visit(CodeThrowStatement       statement );
		protected abstract void Visit(CodeThrowExpression      expression);
		protected abstract void Visit(CodeVariable             expression);
		protected abstract void Visit(CodeNewArray             expression);
		protected abstract void Visit(CodeIndex                expression);
		protected abstract void Visit(CodeField                field     );
		protected abstract void Visit(CodeDefault              expression);
		protected abstract void Visit(CodeNew                  expression);
		protected abstract void Visit(CodeAssignmentStatement  statement );
		protected abstract void Visit(CodeAssignmentExpression expression);
		protected abstract void Visit(CodeAwaitStatement       statement );
		protected abstract void Visit(CodeAwaitExpression      expression);
		protected abstract void Visit(CodeImport               import    );
		protected abstract void Visit(CodePragma               pragma    );
		protected abstract void Visit(CodeFile                 file      );
		protected abstract void Visit(CodeUnary                expression);
		protected abstract void Visit(CodeBinary               expression);
		protected abstract void Visit(CodeTernary              expression);
		protected abstract void Visit(CodeLambda               method    );
		protected abstract void Visit(CodeMember               expression);
		protected abstract void Visit(CodeNameOf               nameOf    );
		protected abstract void Visit(CodeRegion               region    );
		protected abstract void Visit(CodeConstant             constant  );
		protected abstract void Visit(CodeAttribute            attribute );
		protected abstract void Visit(CodeComment              comment   );
		protected abstract void Visit(CodeEmptyLine            line      );
		protected abstract void Visit(CodeMethod               method    );
		protected abstract void Visit(CodeParameter            parameter );
		protected abstract void Visit(CodeXmlComment           doc       );
		protected abstract void Visit(CodeConstructor          ctor      );
		protected abstract void Visit(CodeTypeInitializer      cctor     );
		protected abstract void Visit(CodeThis                 expression);
		protected abstract void Visit(CodeCallStatement        call      );
		protected abstract void Visit(CodeCallExpression       call      );
		protected abstract void Visit(CodeReturn               statement );
		protected abstract void Visit(CodeProperty             property  );
		protected abstract void Visit(CodeNamespace            @namespace);
		protected abstract void Visit(CodeClass                @class    );
		protected abstract void Visit(CodeIdentifier           identifier);
		protected abstract void Visit(CodeTypeReference        type      );
		protected abstract void Visit(CodeTypeToken            type      );
		protected abstract void Visit(CodeReference            reference );

		/// <summary>
		/// Helper method to visit list of nodes.
		/// </summary>
		/// <typeparam name="T">Node type in list.</typeparam>
		/// <param name="list">List of nodes.</param>
		protected void VisitList<T>(CodeElementList<T> list)
			where T: ICodeElement
		{
			VisitList(list.Items);
		}

		/// <summary>
		/// Helper method to visit collection of nodes.
		/// </summary>
		/// <typeparam name="T">Node type in list.</typeparam>
		/// <param name="list">Collection of nodes.</param>
		protected void VisitList<T>(IEnumerable<T> list)
			where T : ICodeElement
		{
			foreach (var item in list)
				Visit(item);
		}
	}
}
