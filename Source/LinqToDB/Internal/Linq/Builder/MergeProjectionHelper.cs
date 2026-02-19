using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class MergeProjectionHelper
	{
		public ExpressionBuilder Builder       { get; }
		public MappingSchema     MappingSchema { get; }
		public MergeFallback?    Fallback      { get; }

		public delegate bool MergeFallback(Expression projection1, Expression projection2, out Expression? merged);

		public MergeProjectionHelper(ExpressionBuilder builder, MappingSchema mappingSchema, MergeFallback? fallback = null)
		{
			Builder       = builder;
			MappingSchema = mappingSchema;
			Fallback      = fallback;
		}

		static bool IsNullValueOrSqlNull(Expression expression)
		{
			if (expression.IsNullValue())
				return true;

			if (expression is SqlPlaceholderExpression placeholder)
				return QueryHelper.IsNullValue(placeholder.Sql);

			return false;
		}

		public bool BuildProjectionExpression(
			Expression                                                                                path,
			IBuildContext                                                                             context,
			[NotNullWhen(true)] out  Expression?                                                      projection,
			[NotNullWhen(true)] out  List<(SqlPlaceholderExpression placeholder, Expression[] path)>? foundPlaceholders,
			[NotNullWhen(true)] out  List<SqlEagerLoadExpression>?                                    foundEager,
			[NotNullWhen(false)] out SqlErrorExpression?                                              error)
		{
			var current = path;
			do
			{
				var projected = Builder.BuildSqlExpression(context, current, buildPurpose : BuildPurpose.Expression,
					buildFlags : BuildFlags.ForceDefaultIfEmpty | BuildFlags.ForSetProjection | BuildFlags.ResetPrevious);

				error = SequenceHelper.FindError(projected);

				if (error != null)
				{
					projection        = null;
					foundPlaceholders = null;
					foundEager        = null;
					return false;
				}

				projected = Builder.BuildExtractExpression(context, projected);

				var lambdaResolver = new LambdaResolveVisitor(context, BuildPurpose.Sql, true);
				projected = lambdaResolver.Visit(projected);

				var optimizer = new ExpressionOptimizerVisitor();
				projected = optimizer.Visit(projected);

				projected = SequenceHelper.RemoveMarkers(projected);

				if (ExpressionEqualityComparer.Instance.Equals(projected, current))
					break;

				current = projected;
			} while (true);

			var pathBuilder = new ExpressionPathVisitor();
			var withPath    = pathBuilder.ProcessExpression(current);

			foundPlaceholders = pathBuilder.FoundPlaceholders;
			foundEager        = pathBuilder.FoundEager;
			projection        = withPath;

			return true;
		}

		public bool TryMergeProjections(Expression projection1, Expression projection2, ProjectFlags flags, [NotNullWhen(true)] out Expression? merged)
		{
			merged = null;

			if (projection1.Type != projection2.Type)
			{
				if (projection1.Type.UnwrapNullableType() != projection2.Type.UnwrapNullableType())
					return false;
			}

			if (ExpressionEqualityComparer.Instance.Equals(projection1, projection2))
			{
				merged = projection1;
				return true;
			}

			if (SequenceHelper.UnwrapDefaultIfEmpty(projection1) is SqlGenericConstructorExpression generic1 &&
			    SequenceHelper.UnwrapDefaultIfEmpty(projection2) is SqlGenericConstructorExpression generic2)
			{
				if (generic1.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
				{
					if (generic2.ConstructType != SqlGenericConstructorExpression.CreateType.Full)
					{
						var constructed = Builder.TryConstruct(MappingSchema, generic1, flags);
						if (constructed == null)
							return false;
						if (TryMergeProjections(Builder.ParseGenericConstructor(constructed, flags, null), generic2, flags, out merged))
							return true;
						return false;
					}
				}

				if (generic2.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
				{
					if (generic1.ConstructType != SqlGenericConstructorExpression.CreateType.Full)
					{
						var constructed = Builder.TryConstruct(MappingSchema, generic2, flags);
						if (constructed == null)
							return false;
						if (TryMergeProjections(generic1, Builder.ParseGenericConstructor(constructed, flags, null), flags, out merged))
							return true;
						return false;
					}
				}

				var resultAssignments = new List<SqlGenericConstructorExpression.Assignment>(generic1.Assignments.Count);

				foreach (var a1 in generic1.Assignments)
				{
					var found = generic2.Assignments.FirstOrDefault(a2 =>
						MemberInfoComparer.Instance.Equals(a2.MemberInfo, a1.MemberInfo));

					if (found == null)
					{
						if (a1.Expression is not SqlPathExpression)
							return false;
						resultAssignments.Add(a1);
					}
					else if (!TryMergeProjections(a1.Expression, found.Expression, flags, out var mergedAssignment))
						return false;
					else
						resultAssignments.Add(a1.WithExpression(mergedAssignment));
				}

				foreach (var a2 in generic2.Assignments)
				{
					var found = generic1.Assignments.FirstOrDefault(a1 =>
						MemberInfoComparer.Instance.Equals(a2.MemberInfo, a1.MemberInfo));

					if (found == null)
					{
						if (a2.Expression is not SqlPathExpression)
							return false;
						resultAssignments.Add(a2);
					}
				}

				if (generic1.Parameters.Count != generic2.Parameters.Count)
				{
					return false;
				}

				var resultGeneric = generic1.ReplaceAssignments(resultAssignments.AsReadOnly());

				if (generic1.Parameters.Count > 0)
				{
					var resultParameters = new List<SqlGenericConstructorExpression.Parameter>(generic1.Parameters.Count);

					for (int i = 0; i < generic1.Parameters.Count; i++)
					{
						if (!TryMergeProjections(generic1.Parameters[i].Expression,
							    generic2.Parameters[i].Expression, flags, out var mergedAssignment))
							return false;

						resultParameters.Add(generic1.Parameters[i].WithExpression(mergedAssignment));
					}

					resultGeneric = resultGeneric.ReplaceParameters(resultParameters.AsReadOnly());
				}

				if (Builder.TryConstruct(MappingSchema, resultGeneric, flags) == null)
					return false;

				merged = resultGeneric;
				return true;
			}

			if (projection1 is ConditionalExpression cond1 && projection2 is ConditionalExpression cond2)
			{
				if (!ExpressionEqualityComparer.Instance.Equals(cond1.Test, cond2.Test))
					return false;

				if (!TryMergeProjections(cond1.IfTrue, cond2.IfTrue, flags, out var ifTrueMerged) ||
				    !TryMergeProjections(cond1.IfFalse, cond2.IfFalse, flags, out var ifFalseMerged))
				{
					return false;
				}

				merged = cond1.Update(cond1.Test, ifTrueMerged, ifFalseMerged);
				return true;
			}

			if (projection1 is SqlPathExpression && IsNullValueOrSqlNull(projection2))
			{
				merged = projection1;
				return true;
			}

			if (projection2 is SqlPathExpression && IsNullValueOrSqlNull(projection1))
			{
				merged = projection2;
				return true;
			}

			if (projection1.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				if (TryMergeProjections(((UnaryExpression)projection1).Operand, projection2, flags, out merged))
				{
					if (merged.Type != projection1.Type)
					{
						merged = Expression.Convert(merged, projection1.Type);
					}

					return true;
				}
			}

			if (projection2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				if (TryMergeProjections(projection1, ((UnaryExpression)projection2).Operand, flags, out merged))
				{
					if (merged.Type != projection2.Type)
					{
						merged = Expression.Convert(merged, projection2.Type);
					}

					return true;
				}
			}

			if (Fallback != null && Fallback(projection1, projection2, out merged) && merged != null)
				return true;

			return false;
		}

		#region Nested class

		sealed class ExpressionOptimizerVisitor : ExpressionVisitorBase
		{
			protected override Expression VisitConditional(ConditionalExpression node)
			{
				var newNode = base.VisitConditional(node);
				if (!ReferenceEquals(newNode, node))
					return Visit(newNode);

				if (node.IfTrue is ConditionalExpression condTrue                        &&
				    ExpressionEqualityComparer.Instance.Equals(node.Test, condTrue.Test) &&
				    ExpressionEqualityComparer.Instance.Equals(node.IfFalse, condTrue.IfFalse))
				{
					return condTrue;
				}

				return node;
			}
		}

		sealed class ExpressionPathVisitor : ExpressionVisitorBase
		{
			Stack<Expression> _stack = new();

			bool _isDictionary;
			bool _insideLambda;
			bool _isStep2;

			public List<(SqlPlaceholderExpression placeholder, Expression[] path)> FoundPlaceholders { get; } = new();

			public List<SqlEagerLoadExpression> FoundEager { get; } = new();

			public Expression ProcessExpression(Expression expression)
			{
				_isStep2 = false;

				var result = Visit(expression);

				_isStep2 = true;

				result = Visit(result);

				return result;
			}

			protected override Expression VisitConditional(ConditionalExpression node)
			{
				_stack.Push(Expression.Constant("?"));
				_stack.Push(Visit(node.Test));

				_stack.Push(Expression.Constant(true));
				var ifTrue = Visit(node.IfTrue);
				_stack.Pop();

				_stack.Push(Expression.Constant(false));
				var ifFalse = Visit(node.IfFalse);
				_stack.Pop();

				var test = _stack.Pop();
				_stack.Pop();

				return node.Update(test, ifTrue, ifFalse);
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				_stack.Push(Expression.Constant("binary"));
				_stack.Push(Expression.Constant(node.NodeType));
				_stack.Push(Visit(node.Left));
				_stack.Push(Visit(node.Right));

				var right = _stack.Pop();
				var left  = _stack.Pop();

				_stack.Pop();
				_stack.Pop();

				return node.Update(left, node.Conversion, right);
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				if (_insideLambda)
				{
					var (placeholder, path) = FoundPlaceholders.FirstOrDefault(p => ExpressionEqualityComparer.Instance.Equals(p.placeholder.Path, node.Path));
					if (placeholder != null)
						return new SqlPathExpression(path, node.Type);
				}

				var stack = _stack.ToArray();
				Array.Reverse(stack);

				FoundPlaceholders.Add((node, stack));

				return new SqlPathExpression(stack, node.Type);
			}

			protected override Expression VisitLambda<T>(Expression<T> node)
			{
				if (!_isStep2)
				{
					return node;
				}

				var saveInsideLambda = _insideLambda;
				_insideLambda = true;

				var result = base.VisitLambda(node);

				_insideLambda = saveInsideLambda;

				return result;
			}

			public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				_stack.Push(Expression.Constant("construct"));
				_stack.Push(Expression.Constant(node.Type));

				if (node.Assignments.Count > 0)
				{
					var newAssignments = new List<SqlGenericConstructorExpression.Assignment>(node.Assignments.Count);

					foreach (var a in node.Assignments)
					{
						var memberInfo = a.MemberInfo.DeclaringType?.GetMemberEx(a.MemberInfo) ?? a.MemberInfo;

						var assignmentExpression = a.Expression;

						_stack.Push(Expression.Constant(memberInfo));
						newAssignments.Add(a.WithExpression(Visit(assignmentExpression)));
						_stack.Pop();
					}

					node = node.ReplaceAssignments(newAssignments.AsReadOnly());
				}

				if (node.Parameters.Count > 0)
				{
					var newParameters = new List<SqlGenericConstructorExpression.Parameter>(node.Parameters.Count);
					for (var index = 0; index < node.Parameters.Count; index++)
					{
						var param = node.Parameters[index];

						var paramExpression = param.Expression;

						if (param.MemberInfo != null)
						{
							// mimic assignment
							var memberInfo = param.MemberInfo.DeclaringType?.GetMemberEx(param.MemberInfo) ?? param.MemberInfo;

							_stack.Push(Expression.Constant(memberInfo));
							newParameters.Add(param.WithExpression(Visit(paramExpression)));
							_stack.Pop();
						}
						else
						{
							_stack.Push(Expression.Constant(index));
							newParameters.Add(param.WithExpression(Visit(paramExpression)));
							_stack.Pop();
						}
					}

					node = node.ReplaceParameters(newParameters.AsReadOnly());
				}

				_stack.Pop();
				_stack.Pop();

				return node;
			}

			public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
			{
				_stack.Push(Expression.Constant("default_if_empty"));

				var newNode = base.VisitSqlDefaultIfEmptyExpression(node);

				_stack.Pop();

				return newNode;
			}

			protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
			{
				_stack.Push(Expression.Constant(node.BindingType));
				_stack.Push(Expression.Constant(node.Member));

				var newNode = base.VisitMemberAssignment(node);

				_stack.Pop();
				_stack.Pop();

				return newNode;
			}

			protected override MemberBinding VisitMemberBinding(MemberBinding node)
			{
				_stack.Push(Expression.Constant(node.BindingType));
				_stack.Push(Expression.Constant(node.Member));

				var newNode = base.VisitMemberBinding(node);

				_stack.Pop();
				_stack.Pop();

				return newNode;
			}

			protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
			{
				_stack.Push(Expression.Constant(node.BindingType));
				_stack.Push(Expression.Constant(node.Member));

				var newNode = base.VisitMemberListBinding(node);

				_stack.Pop();
				_stack.Pop();

				return newNode;
			}

			protected override Expression VisitMemberInit(MemberInitExpression node)
			{
				_stack.Push(Expression.Constant("init"));
				_stack.Push(Expression.Constant(node.NewExpression.Constructor));

				var newExpr = base.VisitMemberInit(node);

				_stack.Pop();
				_stack.Pop();

				return newExpr;
			}

			protected override ElementInit VisitElementInit(ElementInit node)
			{
				_stack.Push(Expression.Constant(node.AddMethod));

				var arguments = new List<Expression>(node.Arguments.Count);

				for (int i = 0; i < node.Arguments.Count; i++)
				{
					_stack.Push(Expression.Constant(i));

					var nodeArgument = node.Arguments[i];

					var arg = Visit(nodeArgument);

					_stack.Pop();

					arguments.Add(arg);
				}

				var newNode = node.Update(arguments);

				_stack.Pop();

				return newNode;
			}

			protected override Expression VisitListInit(ListInitExpression node)
			{
				_stack.Push(Expression.Constant("list init"));

				var initializers = new List<ElementInit>(node.Initializers.Count);

				var saveIDictionary = _isDictionary;
				_isDictionary = typeof(IDictionary<,>).IsSameOrParentOf(node.Type);

				for (int i = 0; i < node.Initializers.Count; i++)
				{
					_stack.Push(Expression.Constant(i));
					initializers.Add(VisitElementInit(node.Initializers[i]));
					_stack.Pop();
				}

				_isDictionary = saveIDictionary;

				var newExpr = node.Update((NewExpression)Visit(node.NewExpression), initializers);

				_stack.Pop();

				return newExpr;
			}

			protected override Expression VisitNew(NewExpression node)
			{
				_stack.Push(Expression.Constant("new"));
				_stack.Push(Expression.Constant(node.Constructor));

				var args = new List<Expression>(node.Arguments.Count);

				for (int i = 0; i < node.Arguments.Count; i++)
				{
					_stack.Push(Expression.Constant(i));
					args.Add(Visit(node.Arguments[i]));
					_stack.Pop();
				}

				var newNode = node.Update(args);

				_stack.Pop();
				_stack.Pop();

				return newNode;
			}

			protected override Expression VisitNewArray(NewArrayExpression node)
			{
				_stack.Push(Expression.Constant("new array"));
				_stack.Push(Expression.Constant(node.Type));

				var args = new List<Expression>(node.Expressions.Count);

				for (int i = 0; i < node.Expressions.Count; i++)
				{
					_stack.Push(Expression.Constant(i));
					args.Add(Visit(node.Expressions[i]));
					_stack.Pop();
				}

				var newNode = node.Update(args);

				_stack.Pop();
				_stack.Pop();

				return newNode;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				_stack.Push(Expression.Constant("call"));

				var obj = Visit(node.Object);
				if (obj != null)
					_stack.Push(Expression.Constant(obj));

				_stack.Push(Expression.Constant(node.Method));

				var args = new List<Expression>(node.Arguments.Count);

				for (var index = 0; index < node.Arguments.Count; index++)
				{
					var arg = node.Arguments[index];
					_stack.Push(Expression.Constant(index));
					args.Add(Visit(arg));
					_stack.Pop();
				}

				_stack.Pop();

				if (obj != null)
					_stack.Pop();

				_stack.Pop();

				return node.Update(obj, args);
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				if (!_isStep2)
				{
					return node;
				}

				var saveStack = _stack;
				_stack = new();

				var newEager = node.Update(Visit(node.SequenceExpression), Visit(node.Predicate));

				_stack = saveStack;

				FoundEager.Add(newEager);

				return newEager;
			}
		}

		#endregion
	}
}
