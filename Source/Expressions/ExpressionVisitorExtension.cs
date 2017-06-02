using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public static class ExpressionVisitorExtension
	{
		static void PushItems(IList<Expression> exprs, Stack<Expression> stack)
		{
			for (var i = exprs.Count - 1; i >= 0; i--)
			{
				var e = exprs[i];
				if (e != null)
					stack.Push(e);
			}
		}

		static void PushItems(IEnumerable<Expression> exprs, Stack<Expression> stack)
		{
			foreach (var e in exprs.Reverse())
			{
				if (e != null)
					stack.Push(e);
			}
		}

		public static IEnumerable<Expression> EnumerateParentFirst(this Expression expression)
		{
			if (expression == null)
				yield break;

			var stack = new Stack<Expression>();
			stack.Push(expression);

			while (stack.Count > 0)
			{
				var expr = stack.Pop();

				if (expr == null)
					continue;

				yield return expr;

				switch (expr.NodeType)
				{
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.And:
					case ExpressionType.AndAlso:
					case ExpressionType.ArrayIndex:
					case ExpressionType.Assign:
					case ExpressionType.Coalesce:
					case ExpressionType.Divide:
					case ExpressionType.Equal:
					case ExpressionType.ExclusiveOr:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.LeftShift:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.Modulo:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.NotEqual:
					case ExpressionType.Or:
					case ExpressionType.OrElse:
					case ExpressionType.Power:
					case ExpressionType.RightShift:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					case ExpressionType.AddAssign:
					case ExpressionType.AndAssign:
					case ExpressionType.DivideAssign:
					case ExpressionType.ExclusiveOrAssign:
					case ExpressionType.LeftShiftAssign:
					case ExpressionType.ModuloAssign:
					case ExpressionType.MultiplyAssign:
					case ExpressionType.OrAssign:
					case ExpressionType.PowerAssign:
					case ExpressionType.RightShiftAssign:
					case ExpressionType.SubtractAssign:
					case ExpressionType.AddAssignChecked:
					case ExpressionType.MultiplyAssignChecked:
					case ExpressionType.SubtractAssignChecked:
					{
						var e = (BinaryExpression)expr;

						stack.Push(e.Right);
						stack.Push(e.Conversion);
						stack.Push(e.Left);

						break;
					}

					case ExpressionType.ArrayLength:
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
					case ExpressionType.Not:
					case ExpressionType.Quote:
					case ExpressionType.TypeAs:
					case ExpressionType.UnaryPlus:
					case ExpressionType.Decrement:
					case ExpressionType.Increment:
					case ExpressionType.IsFalse:
					case ExpressionType.IsTrue:
					case ExpressionType.Throw:
					case ExpressionType.Unbox:
					case ExpressionType.PreIncrementAssign:
					case ExpressionType.PreDecrementAssign:
					case ExpressionType.PostIncrementAssign:
					case ExpressionType.PostDecrementAssign:
					case ExpressionType.OnesComplement:
						stack.Push(((UnaryExpression)expr).Operand);
						break;

					case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						PushItems(e.Arguments, stack);
						stack.Push(e.Object);

						break;
					}

					case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						stack.Push(e.IfFalse);
						stack.Push(e.IfTrue);
						stack.Push(e.Test);

						break;
					}

					case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						PushItems(e.Arguments, stack);
						stack.Push(expression);

						break;
					}

					case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						PushItems(e.Parameters, stack);
						stack.Push(e.Body);

						break;
					}

					case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						PushItems(e.Initializers.SelectMany(i => i.Arguments), stack);
						stack.Push(e.NewExpression);

						break;
					}

					case ExpressionType.MemberAccess: 
						stack.Push(((MemberExpression)expr).Expression); 
						break;

					case ExpressionType.MemberInit:
					{
						Action<IEnumerable<MemberBinding>> pushLocal = null; pushLocal = bindings =>
						{
							foreach (var b in bindings.Reverse())
							{
								switch (b.BindingType)
								{
									case MemberBindingType.Assignment    : stack.Push(((MemberAssignment)b).Expression);                                       break;
									case MemberBindingType.ListBinding   : PushItems(((MemberListBinding)b).Initializers.SelectMany(i => i.Arguments), stack); break;
									case MemberBindingType.MemberBinding : pushLocal(((MemberMemberBinding)b).Bindings);                                       break;
								}
							}
						};

						var e = (MemberInitExpression)expr;

						pushLocal(e.Bindings);
						stack.Push(e.NewExpression);

						break;
					}

					case ExpressionType.New            : PushItems(((NewExpression)       expr).Arguments,   stack); break;
					case ExpressionType.NewArrayBounds : PushItems(((NewArrayExpression)  expr).Expressions, stack); break;
					case ExpressionType.NewArrayInit   : PushItems(((NewArrayExpression)  expr).Expressions, stack); break;
					case ExpressionType.TypeEqual      :
					case ExpressionType.TypeIs         : stack.Push(((TypeBinaryExpression)expr).Expression);        break;

					case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						PushItems(e.Variables,   stack);
						PushItems(e.Expressions, stack);

						break;
					}

					case ChangeTypeExpression.ChangeTypeType :
						stack.Push(((ChangeTypeExpression)expr).Expression); break;

					case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						PushItems(e.Arguments, stack);

						break;
					}

					case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						stack.Push(e.Value);

						break;
					}

					case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						PushItems(e.Arguments, stack);
						stack.Push(e.Object);

						break;
					}

					case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						stack.Push(e.DefaultValue);

						break;
					}

					case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						PushItems(e.Variables, stack);

						break;
					}

					case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						stack.Push(e.Body);

						break;
					}

					case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						stack.Push(e.DefaultBody);
						PushItems(e.Cases.SelectMany(cs => new[]{cs.Body}.Concat(cs.TestValues)), stack);
						stack.Push(e.SwitchValue);

						break;
					}

					case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						PushItems(e.Handlers.SelectMany(h => new [] {h.Variable, h.Filter, h.Body}), stack);
						stack.Push(e.Finally);
						stack.Push(e.Fault);
						stack.Push(e.Body);

						break;
					}

					case ExpressionType.Extension:
					{
						var aggregate = expr as BinaryAggregateExpression;
						if (aggregate != null)
							PushItems(aggregate.Expressions, stack);
						else
						{
							if (expr.CanReduce)
								stack.Push(expr.Reduce());
						}

						break;
					}
				}
			}
		}
	}
}