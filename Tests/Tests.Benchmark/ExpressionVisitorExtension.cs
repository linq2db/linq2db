using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LinqToDB.Expressions;

namespace Tests.Benchmark
{
	public static class ExpressionVisitorExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void PushItemsReverse(IList<Expression> exprs, Stack<Expression> stack)
		{
			for (var i = exprs.Count - 1; i >= 0; i--)
			{
				var e = exprs[i];
				if (e != null)
					stack.Push(e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void PushItemsReverse(IEnumerable<Expression> exprs, Stack<Expression> stack)
		{
			foreach (var e in exprs.Reverse())
			{
				if (e != null)
					stack.Push(e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void PushItems(IList<Expression> exprs, Stack<Expression> stack)
		{
			for (var i = 0; i < exprs.Count; i++)
			{
				var e = exprs[i];
				if (e != null)
					stack.Push(e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void PushItems(IEnumerable<Expression> exprs, Stack<Expression> stack)
		{
			foreach (var e in exprs)
			{
				if (e != null)
					stack.Push(e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void EnqueueItem(Expression expr, Queue<Expression> queue)
		{
			if (expr != null)
				queue.Enqueue(expr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void EnqueueItems(IList<Expression> exprs, Queue<Expression> queue)
		{
			for (var i = 0; i < exprs.Count; i++)
			{
				var e = exprs[i];
				if (e != null)
					queue.Enqueue(e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		static void EnqueueItems(IEnumerable<Expression> exprs, Queue<Expression> queue)
		{
			foreach (var e in exprs)
			{
				if (e != null)
					queue.Enqueue(e);
			}
		}

		public static IEnumerable<Expression> EnumerateParentFirst1(this Expression expression)
		{
			if (expression == null)
				yield break;

			var queue = new Queue<Expression>();
			queue.Enqueue(expression);

//			var maxCount = 0;

			while (queue.Count > 0)
			{
//				maxCount = Math.Max(maxCount, queue.Count);
				var expr = queue.Dequeue();

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

						queue.Enqueue(e.Left);
						queue.Enqueue(e.Conversion);
						queue.Enqueue(e.Right);

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
						queue.Enqueue(((UnaryExpression)expr).Operand);
						break;

					case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						EnqueueItems(e.Arguments, queue);
						queue.Enqueue(e.Object);

						break;
					}

					case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						queue.Enqueue(e.Test);
						queue.Enqueue(e.IfTrue);
						queue.Enqueue(e.IfFalse);

						break;
					}

					case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						queue.Enqueue(e.Expression);
						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						queue.Enqueue(e.Body);
						EnqueueItems(e.Parameters, queue);

						break;
					}

					case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						queue.Enqueue(e.NewExpression);
						EnqueueItems(e.Initializers.SelectMany(i => i.Arguments), queue);

						break;
					}

					case ExpressionType.MemberAccess: 
						queue.Enqueue(((MemberExpression)expr).Expression); 
						break;

					case ExpressionType.MemberInit:
					{
						Action<IEnumerable<MemberBinding>> enqueueLocal = null; enqueueLocal = bindings =>
						{
							foreach (var b in bindings)
							{
								switch (b.BindingType)
								{
									case MemberBindingType.Assignment    : queue.Enqueue(((MemberAssignment)b).Expression);                                       break;
									case MemberBindingType.ListBinding   : EnqueueItems(((MemberListBinding)b).Initializers.SelectMany(i => i.Arguments), queue); break;
									case MemberBindingType.MemberBinding : enqueueLocal(((MemberMemberBinding)b).Bindings);                                       break;
								}
							}
						};

						var e = (MemberInitExpression)expr;

						queue.Enqueue(e.NewExpression);
						enqueueLocal(e.Bindings);

						break;
					}

					case ExpressionType.New            : EnqueueItems(((NewExpression)       expr).Arguments,   queue); break;
					case ExpressionType.NewArrayBounds : EnqueueItems(((NewArrayExpression)  expr).Expressions, queue); break;
					case ExpressionType.NewArrayInit   : EnqueueItems(((NewArrayExpression)  expr).Expressions, queue); break;
					case ExpressionType.TypeEqual      :
					case ExpressionType.TypeIs         : queue.Enqueue(((TypeBinaryExpression)expr).Expression);        break;

					case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						EnqueueItems(e.Expressions, queue);
						EnqueueItems(e.Variables,   queue);

						break;
					}

//					case ChangeTypeExpression.ChangeTypeType :
//						queue.Enqueue(((ChangeTypeExpression)expr).Expression); break;

					case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						queue.Enqueue(e.Value);

						break;
					}

					case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						queue.Enqueue(e.Object);
						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						queue.Enqueue(e.DefaultValue);

						break;
					}

					case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						EnqueueItems(e.Variables, queue);

						break;
					}

					case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						queue.Enqueue(e.Body);

						break;
					}

					case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						queue.Enqueue(e.SwitchValue);
						EnqueueItems(e.Cases.SelectMany(cs => cs.TestValues.Concat(new[] {cs.Body})), queue);
						queue.Enqueue(e.DefaultBody);

						break;
					}

					case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						EnqueueItems(e.Handlers.SelectMany(h => new [] {h.Body, h.Filter, h.Variable}), queue);
						queue.Enqueue(e.Body);
						queue.Enqueue(e.Fault);
						queue.Enqueue(e.Finally);

						break;
					}

					case ExpressionType.Extension:
					{
						var aggregate = expr as BinaryAggregateExpression;
						if (aggregate != null)
							EnqueueItems(aggregate.Expressions, queue);
						else
						{
							if (expr.CanReduce)
								queue.Enqueue(expr.Reduce());
						}

						break;
					}

				}
			}
		}

		public static IEnumerable<Expression> EnumerateParentFirst2(this Expression expression)
		{
			if (expression == null)
				yield break;

			var queue = new Queue<Expression>();
			queue.Enqueue(expression);

//			var maxCount = 0;

			while (queue.Count > 0)
			{
//				maxCount = Math.Max(maxCount, queue.Count);
				var expr = queue.Dequeue();

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

						EnqueueItem(e.Left, queue);
						EnqueueItem(e.Conversion, queue);
						EnqueueItem(e.Right, queue);

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
						EnqueueItem(((UnaryExpression)expr).Operand, queue);
						break;

					case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						EnqueueItems(e.Arguments, queue);
						EnqueueItem(e.Object, queue);

						break;
					}

					case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						EnqueueItem(e.Test, queue);
						EnqueueItem(e.IfTrue, queue);
						EnqueueItem(e.IfFalse, queue);

						break;
					}

					case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						EnqueueItem(e.Expression, queue);
						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						EnqueueItem(e.Body, queue);
						EnqueueItems(e.Parameters, queue);

						break;
					}

					case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						EnqueueItem(e.NewExpression, queue);
						EnqueueItems(e.Initializers.SelectMany(i => i.Arguments), queue);

						break;
					}

					case ExpressionType.MemberAccess: 
						EnqueueItem(((MemberExpression)expr).Expression, queue); 
						break;

					case ExpressionType.MemberInit:
					{
						Action<IEnumerable<MemberBinding>> enqueueLocal = null; enqueueLocal = bindings =>
						{
							foreach (var b in bindings)
							{
								switch (b.BindingType)
								{
									case MemberBindingType.Assignment    : EnqueueItem(((MemberAssignment)b).Expression, queue);                                       break;
									case MemberBindingType.ListBinding   : EnqueueItems(((MemberListBinding)b).Initializers.SelectMany(i => i.Arguments), queue); break;
									case MemberBindingType.MemberBinding : enqueueLocal(((MemberMemberBinding)b).Bindings);                                       break;
								}
							}
						};

						var e = (MemberInitExpression)expr;

						EnqueueItem(e.NewExpression, queue);
						enqueueLocal(e.Bindings);

						break;
					}

					case ExpressionType.New            : EnqueueItems(((NewExpression)       expr).Arguments,   queue); break;
					case ExpressionType.NewArrayBounds : EnqueueItems(((NewArrayExpression)  expr).Expressions, queue); break;
					case ExpressionType.NewArrayInit   : EnqueueItems(((NewArrayExpression)  expr).Expressions, queue); break;
					case ExpressionType.TypeEqual      :
					case ExpressionType.TypeIs         : EnqueueItem(((TypeBinaryExpression)expr).Expression, queue);        break;

					case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						EnqueueItems(e.Expressions, queue);
						EnqueueItems(e.Variables,   queue);

						break;
					}

//					case ChangeTypeExpression.ChangeTypeType :
//						EnqueueItem(((ChangeTypeExpression)expr).Expression); break;

					case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						EnqueueItem(e.Value, queue);

						break;
					}

					case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						EnqueueItem(e.Object, queue);
						EnqueueItems(e.Arguments, queue);

						break;
					}

					case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						EnqueueItem(e.DefaultValue, queue);

						break;
					}

					case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						EnqueueItems(e.Variables, queue);

						break;
					}

					case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						EnqueueItem(e.Body, queue);

						break;
					}

					case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						EnqueueItem(e.SwitchValue, queue);
						EnqueueItems(e.Cases.SelectMany(cs => cs.TestValues.Concat(new[] {cs.Body})), queue);
						EnqueueItem(e.DefaultBody, queue);

						break;
					}

					case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						EnqueueItems(e.Handlers.SelectMany(h => new [] {h.Body, h.Filter, h.Variable}), queue);
						EnqueueItem(e.Body, queue);
						EnqueueItem(e.Fault, queue);
						EnqueueItem(e.Finally, queue);

						break;
					}

					case ExpressionType.Extension:
					{
						var aggregate = expr as BinaryAggregateExpression;
						if (aggregate != null)
							EnqueueItems(aggregate.Expressions, queue);
						else
						{
							if (expr.CanReduce)
								EnqueueItem(expr.Reduce(), queue);
						}

						break;
					}

				}
			}
		}

	}
}