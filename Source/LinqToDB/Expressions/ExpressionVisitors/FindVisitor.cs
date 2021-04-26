using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	internal class FindVisitor<TContext>
	{
		private readonly TContext                         _context;
		private readonly Func<TContext, Expression, bool> _func;

		public FindVisitor(TContext context, Func<TContext, Expression, bool> func)
		{
			_context = context;
			_func    = func;
		}

		private Expression? Find<T>(IEnumerable<T> source, Func<T, Expression?> func)
		{
			foreach (var item in source)
			{
				var ex = func(item);
				if (ex != null)
					return ex;
			}

			return null;
		}

		private Expression? Find<T>(IEnumerable<T> source)
			where T : Expression
		{
			foreach (var item in source)
			{
				var f = Find(item);
				if (f != null)
					return f;
			}

			return null;
		}

		public Expression? Find(Expression? expr)
		{
			if (expr == null || _func(_context, expr))
				return expr;

			switch (expr.NodeType)
			{
				case ExpressionType.Add                  :
				case ExpressionType.AddChecked           :
				case ExpressionType.And                  :
				case ExpressionType.AndAlso              :
				case ExpressionType.ArrayIndex           :
				case ExpressionType.Assign               :
				case ExpressionType.Coalesce             :
				case ExpressionType.Divide               :
				case ExpressionType.Equal                :
				case ExpressionType.ExclusiveOr          :
				case ExpressionType.GreaterThan          :
				case ExpressionType.GreaterThanOrEqual   :
				case ExpressionType.LeftShift            :
				case ExpressionType.LessThan             :
				case ExpressionType.LessThanOrEqual      :
				case ExpressionType.Modulo               :
				case ExpressionType.Multiply             :
				case ExpressionType.MultiplyChecked      :
				case ExpressionType.NotEqual             :
				case ExpressionType.Or                   :
				case ExpressionType.OrElse               :
				case ExpressionType.Power                :
				case ExpressionType.RightShift           :
				case ExpressionType.Subtract             :
				case ExpressionType.SubtractChecked      :
				case ExpressionType.AddAssign            :
				case ExpressionType.AndAssign            :
				case ExpressionType.DivideAssign         :
				case ExpressionType.ExclusiveOrAssign    :
				case ExpressionType.LeftShiftAssign      :
				case ExpressionType.ModuloAssign         :
				case ExpressionType.MultiplyAssign       :
				case ExpressionType.OrAssign             :
				case ExpressionType.PowerAssign          :
				case ExpressionType.RightShiftAssign     :
				case ExpressionType.SubtractAssign       :
				case ExpressionType.AddAssignChecked     :
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
				{
					var e = (BinaryExpression)expr;

					return
						Find(e.Conversion) ??
						Find(e.Left) ??
						Find(e.Right);
				}

				case ExpressionType.ArrayLength        :
				case ExpressionType.Convert            :
				case ExpressionType.ConvertChecked     :
				case ExpressionType.Negate             :
				case ExpressionType.NegateChecked      :
				case ExpressionType.Not                :
				case ExpressionType.Quote              :
				case ExpressionType.TypeAs             :
				case ExpressionType.UnaryPlus          :
				case ExpressionType.Decrement          :
				case ExpressionType.Increment          :
				case ExpressionType.IsFalse            :
				case ExpressionType.IsTrue             :
				case ExpressionType.Throw              :
				case ExpressionType.Unbox              :
				case ExpressionType.PreIncrementAssign :
				case ExpressionType.PreDecrementAssign :
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.OnesComplement     :
					return Find(((UnaryExpression)expr).Operand);

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expr;

					return
						Find(e.Object) ??
						Find(e.Arguments);
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expr;

					return
						Find(e.Test) ??
						Find(e.IfTrue) ??
						Find(e.IfFalse);
				}

				case ExpressionType.Invoke:
				{
					var e = (InvocationExpression)expr;

					return
						Find(e.Expression) ??
						Find(e.Arguments);
				}

				case ExpressionType.Lambda:
				{
					var e = (LambdaExpression)expr;

					return
						Find(e.Body) ??
						Find(e.Parameters);
				}

				case ExpressionType.ListInit:
				{
					var e = (ListInitExpression)expr;

					return
						Find(e.NewExpression) ??
						Find(e.Initializers, ElementInitFind);
				}

				case ExpressionType.MemberAccess:
					return Find(((MemberExpression)expr).Expression);

				case ExpressionType.MemberInit:
				{
					var e = (MemberInitExpression)expr;

					return
						Find(e.NewExpression) ??
						Find(e.Bindings, MemberBindingFind);
				}

				case ExpressionType.New           : return Find(((NewExpression)expr).Arguments);
				case ExpressionType.NewArrayBounds: return Find(((NewArrayExpression)expr).Expressions);
				case ExpressionType.NewArrayInit  : return Find(((NewArrayExpression)expr).Expressions);
				case ExpressionType.TypeEqual     :
				case ExpressionType.TypeIs        : return Find(((TypeBinaryExpression)expr).Expression);

				case ExpressionType.Block:
				{
					var e = (BlockExpression)expr;

					return
						Find(e.Expressions) ??
						Find(e.Variables);
				}

				case ChangeTypeExpression.ChangeTypeType:
					return Find(((ChangeTypeExpression)expr).Expression);

				case ExpressionType.Dynamic:
				{
					var e = (DynamicExpression)expr;

					return
						Find(e.Arguments);
				}

				case ExpressionType.Goto:
				{
					var e = (GotoExpression)expr;

					return
						Find(e.Value);
				}

				case ExpressionType.Index:
				{
					var e = (IndexExpression)expr;

					return
						Find(e.Object) ??
						Find(e.Arguments);
				}

				case ExpressionType.Label:
				{
					var e = (LabelExpression)expr;

					return
						Find(e.DefaultValue);
				}

				case ExpressionType.RuntimeVariables:
				{
					var e = (RuntimeVariablesExpression)expr;

					return
						Find(e.Variables);
				}

				case ExpressionType.Loop:
				{
					var e = (LoopExpression)expr;

					return
						Find(e.Body);
				}


				case ExpressionType.Switch:
				{
					var e = (SwitchExpression)expr;

					return
						Find(e.SwitchValue) ??
						Find(e.Cases, SwitchCaseFind) ??
						Find(e.DefaultBody);
				}

				case ExpressionType.Try:
				{
					var e = (TryExpression)expr;

					return
						Find(e.Body) ??
						Find(e.Handlers, CatchBlockFind) ??
						Find(e.Finally) ??
						Find(e.Fault);
				}

				case ExpressionType.Extension:
					if (expr.CanReduce)
						return Find(expr.Reduce());

					break;
			}

			return null;
		}

		private Expression? SwitchCaseFind(SwitchCase sc)
		{
			return Find(sc.TestValues) ?? Find(sc.Body);
		}

		private Expression? CatchBlockFind(CatchBlock cb)
		{
			return Find(cb.Variable) ?? Find(cb.Filter) ?? Find(cb.Body);
		}

		private Expression? MemberBindingFind(MemberBinding b)
		{
			return b.BindingType switch
			{
				MemberBindingType.Assignment    => Find(((MemberAssignment)b).Expression),
				MemberBindingType.ListBinding   => Find(((MemberListBinding)b).Initializers, ElementInitFind),
				MemberBindingType.MemberBinding => Find(((MemberMemberBinding)b).Bindings, MemberBindingFind),
				_                               => null,
			};
		}

		Expression? ElementInitFind(ElementInit ei)
		{
			return Find(ei.Arguments);
		}
	}
}
