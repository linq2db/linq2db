using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using LinqToDB;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal readonly struct VisitFuncVisitor<TContext>
	{
		private readonly TContext?                          _context;
		private readonly Func<TContext, Expression, bool>? _func;
		private readonly Func<Expression,bool>?            _staticFunc;

		public VisitFuncVisitor(TContext context, Func<TContext, Expression, bool> func)
		{
			_context    = context;
			_func       = func;
			_staticFunc = null;
		}

		public VisitFuncVisitor(Func<Expression, bool> func)
		{
			_context    = default;
			_func       = null;
			_staticFunc = func;
		}

		/// <summary>
		/// Creates reusable static visitor.
		/// </summary>
		public static VisitFuncVisitor<object?> Create(Func<Expression, bool> func)
		{
			return new VisitFuncVisitor<object?>(func);
		}

		/// <summary>
		/// Creates reusable visitor with static context.
		/// </summary>
		public static VisitFuncVisitor<TContext> Create(TContext context, Func<TContext, Expression, bool> func)
		{
			return new VisitFuncVisitor<TContext>(context, func);
		}

		static void Visit<T>(IEnumerable<T> source, Action<T> func)
		{
			foreach (var item in source)
				func(item);
		}

		void Visit<T>(IEnumerable<T> source)
			where T : Expression
		{
			foreach (var item in source)
				Visit(item);
		}

		public void Visit(Expression? expr)
		{
			if (expr == null || (_staticFunc != null ? !_staticFunc(expr) : !_func!(_context!, expr)))
				return;

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

					Visit(e.Conversion);
					Visit(e.Left);
					Visit(e.Right);

					break;
				}

				case ExpressionType.ArrayLength         :
				case ExpressionType.Convert             :
				case ExpressionType.ConvertChecked      :
				case ExpressionType.Negate              :
				case ExpressionType.NegateChecked       :
				case ExpressionType.Not                 :
				case ExpressionType.Quote               :
				case ExpressionType.TypeAs              :
				case ExpressionType.UnaryPlus           :
				case ExpressionType.Decrement           :
				case ExpressionType.Increment           :
				case ExpressionType.IsFalse             :
				case ExpressionType.IsTrue              :
				case ExpressionType.Throw               :
				case ExpressionType.Unbox               :
				case ExpressionType.PreIncrementAssign  :
				case ExpressionType.PreDecrementAssign  :
				case ExpressionType.PostIncrementAssign :
				case ExpressionType.PostDecrementAssign :
				case ExpressionType.OnesComplement      : Visit(((UnaryExpression           )expr).Operand     ); break;
				case ExpressionType.MemberAccess        : Visit(((MemberExpression          )expr).Expression  ); break;
				case ExpressionType.New                 : Visit(((NewExpression             )expr).Arguments   ); break;
				case ExpressionType.NewArrayBounds      : Visit(((NewArrayExpression        )expr).Expressions ); break;
				case ExpressionType.NewArrayInit        : Visit(((NewArrayExpression        )expr).Expressions ); break;
				case ExpressionType.TypeEqual           :
				case ExpressionType.TypeIs              : Visit(((TypeBinaryExpression      )expr).Expression  ); break;
				case ChangeTypeExpression.ChangeTypeType: Visit(((ChangeTypeExpression      )expr).Expression  ); break;
				case ExpressionType.Dynamic             : Visit(((DynamicExpression         )expr).Arguments   ); break;
				case ExpressionType.Goto                : Visit(((GotoExpression            )expr).Value       ); break;
				case ExpressionType.Label               : Visit(((LabelExpression           )expr).DefaultValue); break;
				case ExpressionType.RuntimeVariables    : Visit(((RuntimeVariablesExpression)expr).Variables   ); break;
				case ExpressionType.Loop                : Visit(((LoopExpression            )expr).Body        ); break;

				case ExpressionType.MemberInit:
				{
					var e = (MemberInitExpression)expr;

					Visit(e.NewExpression);
					Visit(e.Bindings, MemberBindingVisit);

					break;
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expr;

					Visit(e.Object);
					Visit(e.Arguments);

					break;
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expr;

					Visit(e.Test);
					Visit(e.IfTrue);
					Visit(e.IfFalse);

					break;
				}

				case ExpressionType.Invoke:
				{
					var e = (InvocationExpression)expr;

					Visit(e.Expression);
					Visit(e.Arguments);

					break;
				}

				case ExpressionType.Lambda:
				{
					var e = (LambdaExpression)expr;

					Visit(e.Body);
					Visit(e.Parameters);

					break;
				}

				case ExpressionType.ListInit:
				{
					var e = (ListInitExpression)expr;

					Visit(e.NewExpression);
					Visit(e.Initializers, ElementInitVisit);

					break;
				}

				case ExpressionType.Block:
				{
					var e = (BlockExpression)expr;

					Visit(e.Expressions);
					Visit(e.Variables);

					break;
				}

				case ExpressionType.Index:
				{
					var e = (IndexExpression)expr;

					Visit(e.Object);
					Visit(e.Arguments);

					break;
				}

				case ExpressionType.Switch:
				{
					var e = (SwitchExpression)expr;

					Visit(e.SwitchValue);
					Visit(e.Cases, SwitchCaseVisit);
					Visit(e.DefaultBody);

					break;
				}

				case ExpressionType.Try:
				{
					var e = (TryExpression)expr;

					Visit(e.Body);
					Visit(e.Handlers, CatchBlockVisit);
					Visit(e.Finally);
					Visit(e.Fault);

					break;
				}

				case ExpressionType.Extension:
				{
					VisitXE(expr);

					break;
				}

				// final expressions
				case ExpressionType.Default  :
				case ExpressionType.Parameter:
				case ExpressionType.Constant : break;

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr.NodeType}");
			}
		}

		private void SwitchCaseVisit(SwitchCase sc)
		{
			Visit(sc.TestValues);
			Visit(sc.Body);
		}

		private void CatchBlockVisit(CatchBlock cb)
		{
			Visit(cb.Variable);
			Visit(cb.Filter);
			Visit(cb.Body);
		}

		void MemberBindingVisit(MemberBinding b)
		{
			switch (b.BindingType)
			{
				case MemberBindingType.Assignment   : Visit(((MemberAssignment   )b).Expression                      ); break;
				case MemberBindingType.ListBinding  : Visit(((MemberListBinding  )b).Initializers, ElementInitVisit  ); break;
				case MemberBindingType.MemberBinding: Visit(((MemberMemberBinding)b).Bindings    , MemberBindingVisit); break;
			}
		}

		void ElementInitVisit(ElementInit ei)
		{
			Visit(ei.Arguments);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VisitXE(Expression expr)
		{
			if (expr is SqlGenericConstructorExpression generic)
			{
				Visit(generic.Assignments.Select(a => a.Expression));
				Visit(generic.Parameters.Select(p => p.Expression));
			}
			else if (expr is SqlGenericParamAccessExpression paramAccess)
			{
				Visit(paramAccess.Constructor);
			}
			else if (expr is SqlReaderIsNullExpression isNullExpression)
			{
				Visit(isNullExpression.Placeholder);
			}
			else if (expr is SqlAdjustTypeExpression adjustType)
			{
				Visit(adjustType.Expression);
			}
			else if (expr is SqlDefaultIfEmptyExpression defaultIfEmpty)
			{
				Visit(defaultIfEmpty.InnerExpression);
				Visit(defaultIfEmpty.NotNullExpressions);
			}
			else if (expr is SqlPathExpression keyHolder)
			{
			}
			else if (expr is SqlValidateExpression validateExpression)
			{
				Visit(validateExpression.SqlPlaceholder);
			}
			else if (expr.CanReduce)
			{
				Visit(expr.Reduce());
			}
		}

	}
}
