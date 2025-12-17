using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal readonly struct FindVisitor<TContext>
	{
		internal readonly TContext?                         Context;
		private  readonly Func<TContext, Expression, bool>? _func;
		private  readonly Func<Expression, bool>?           _staticFunc;

		/// <summary>
		/// Creates reusable find visitor for calls with same context.
		/// </summary>
		public static FindVisitor<TContext> Create(TContext context, Func<TContext, Expression, bool> func)
		{
			return new FindVisitor<TContext>(context, func);
		}

		/// <summary>
		/// Creates reusable static find visitor.
		/// </summary>
		public static FindVisitor<object?> Create(Func<Expression, bool> func)
		{
			return new FindVisitor<object?>(func);
		}

		/// <summary>
		/// Creates contextful visitor instance. Such instances cannot be cached.
		/// </summary>
		/// <param name="context">Context for current visitor call.</param>
		/// <param name="func">Visit action.</param>
		public FindVisitor(TContext context, Func<TContext, Expression, bool> func)
		{
			Context     = context;
			_func       = func;
			_staticFunc = null;
		}

		/// <summary>
		/// Creates context-less visitor instance. Such instances should be cached and reused by caller as they
		/// don't have per-call context but only if they use static actions.
		/// </summary>
		/// <param name="func">Visit action.</param>
		public FindVisitor(Func<Expression, bool> func)
		{
			Context     = default;
			_func       = null;
			_staticFunc = func;
		}

		private static Expression? Find<T>(IEnumerable<T> source, Func<T, Expression?> func)
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
			if (expr == null || (_staticFunc != null ? _staticFunc(expr) :_func!(Context!, expr)))
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
						Find(e.Left      ) ??
						Find(e.Right     );
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
				case ExpressionType.OnesComplement      : return Find(((UnaryExpression           )expr).Operand     );
				case ExpressionType.MemberAccess        : return Find(((MemberExpression          )expr).Expression  );
				case ExpressionType.New                 : return Find(((NewExpression             )expr).Arguments   );
				case ExpressionType.NewArrayBounds      : return Find(((NewArrayExpression        )expr).Expressions );
				case ExpressionType.NewArrayInit        : return Find(((NewArrayExpression        )expr).Expressions );
				case ExpressionType.TypeEqual           :
				case ExpressionType.TypeIs              : return Find(((TypeBinaryExpression      )expr).Expression  );
				case ChangeTypeExpression.ChangeTypeType: return Find(((ChangeTypeExpression      )expr).Expression  );
				case ExpressionType.Dynamic             : return Find(((DynamicExpression         )expr).Arguments   );
				case ExpressionType.Goto                : return Find(((GotoExpression            )expr).Value       );
				case ExpressionType.Label               : return Find(((LabelExpression           )expr).DefaultValue);
				case ExpressionType.RuntimeVariables    : return Find(((RuntimeVariablesExpression)expr).Variables   );
				case ExpressionType.Loop                : return Find(((LoopExpression            )expr).Body        );

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expr;

					return
						Find(e.Object   ) ??
						Find(e.Arguments);
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expr;

					return
						Find(e.Test   ) ??
						Find(e.IfTrue ) ??
						Find(e.IfFalse);
				}

				case ExpressionType.Invoke:
				{
					var e = (InvocationExpression)expr;

					return
						Find(e.Expression) ??
						Find(e.Arguments );
				}

				case ExpressionType.Lambda:
				{
					var e = (LambdaExpression)expr;

					return
						Find(e.Body      ) ??
						Find(e.Parameters);
				}

				case ExpressionType.ListInit:
				{
					var e = (ListInitExpression)expr;

					return
						Find(e.NewExpression                ) ??
						Find(e.Initializers, ElementInitFind);
				}

				case ExpressionType.MemberInit:
				{
					var e = (MemberInitExpression)expr;

					return
						Find(e.NewExpression              ) ??
						Find(e.Bindings, MemberBindingFind);
				}

				case ExpressionType.Block:
				{
					var e = (BlockExpression)expr;

					return
						Find(e.Expressions) ??
						Find(e.Variables  );
				}

				case ExpressionType.Index:
				{
					var e = (IndexExpression)expr;

					return
						Find(e.Object   ) ??
						Find(e.Arguments);
				}

				case ExpressionType.Switch:
				{
					var e = (SwitchExpression)expr;

					return
						Find(e.SwitchValue          ) ??
						Find(e.Cases, SwitchCaseFind) ??
						Find(e.DefaultBody          );
				}

				case ExpressionType.Try:
				{
					var e = (TryExpression)expr;

					return
						Find(e.Body                    ) ??
						Find(e.Handlers, CatchBlockFind) ??
						Find(e.Finally                 ) ??
						Find(e.Fault                   );
				}

				case ExpressionType.Extension:
				{
					return expr switch
					{
						SqlGenericConstructorExpression generic => 
							Find(generic.Parameters, ParameterFind)
							?? Find(generic.Assignments, AssignmentFind),

						SqlErrorExpression error =>
							Find(error.Expression),

						SqlAdjustTypeExpression adjustType =>
							Find(adjustType.Expression),

						SqlGenericParamAccessExpression paramAccess =>
							Find(paramAccess.Constructor),

						SqlDefaultIfEmptyExpression defaultIfEmptyExpression =>
							Find(defaultIfEmptyExpression.InnerExpression),

						SqlValidateExpression validateExpression =>
							Find(validateExpression.InnerExpression),

						SqlPathExpression pathExpression =>
							Find(pathExpression.Path),

						{ CanReduce: true } =>
							Find(expr.Reduce()),

						_ => null,
					};
				}
					// final expressions
				case ExpressionType.Parameter:
				case ExpressionType.Default  :
				case ExpressionType.Constant : break;

				default:
					throw new NotSupportedException($"Unhandled expression type: {expr.NodeType}");
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
				MemberBindingType.Assignment    => Find(((MemberAssignment   )b).Expression                     ),
				MemberBindingType.ListBinding   => Find(((MemberListBinding  )b).Initializers, ElementInitFind  ),
				MemberBindingType.MemberBinding => Find(((MemberMemberBinding)b).Bindings,     MemberBindingFind),
				_                               => null,
			};
		}

		private Expression? AssignmentFind(SqlGenericConstructorExpression.Assignment assignment)
		{
			return Find(assignment.Expression);
		}

		private Expression? ParameterFind(SqlGenericConstructorExpression.Parameter parameter)
		{
			return Find(parameter.Expression);
		}

		Expression? ElementInitFind(ElementInit ei)
		{
			return Find(ei.Arguments);
		}
	}
}
