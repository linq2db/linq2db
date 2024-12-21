using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions.ExpressionVisitors
{
	using Extensions;

	readonly struct TransformInfoVisitor<TContext>
	{
		private readonly TContext?                                  _context;
		private readonly Func<TContext, Expression, TransformInfo>? _func;
		private readonly Func<Expression, TransformInfo>?           _staticFunc;

		public TransformInfoVisitor(TContext context, Func<TContext, Expression, TransformInfo> func)
		{
			_context    = context;
			_func       = func;
			_staticFunc = null;
		}

		public TransformInfoVisitor(Func<Expression, TransformInfo> func)
		{
			_context    = default;
			_func       = null;
			_staticFunc = func;
		}

		/// <summary>
		/// Creates reusable static visitor.
		/// </summary>
		public static TransformInfoVisitor<object?> Create(Func<Expression, TransformInfo> func)
		{
			return new TransformInfoVisitor<object?>(func);
		}

		/// <summary>
		/// Creates reusable visitor with static context.
		/// </summary>
		public static TransformInfoVisitor<TContext> Create(TContext context, Func<TContext, Expression, TransformInfo> func)
		{
			return new TransformInfoVisitor<TContext>(context, func);
		}

		[return: NotNullIfNotNull(nameof(expr))]
		public Expression? Transform(Expression? expr)
		{
			if (expr == null)
				return null;

			do
			{
				var ti = _staticFunc != null ? _staticFunc(expr) : _func!(_context!, expr);
				if (ti.Stop || !ti.Continue && ti.Expression != expr)
					return ti.Expression;
				if (expr == ti.Expression)
					break;
				expr = ti.Expression;
			} while (true);

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

					return e.Update(
						Transform(e.Left),
						(LambdaExpression?)Transform(e.Conversion),
						Transform(e.Right));
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
				{
					var e = (UnaryExpression)expr;

					return e.Update(Transform(e.Operand));
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expr;

					return e.Update(
						Transform(e.Object),
						Transform(e.Arguments));
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expr;

					return e.Update(
						Transform(e.Test),
						Transform(e.IfTrue),
						Transform(e.IfFalse));
				}

				case ExpressionType.Invoke:
				{
					var e  = (InvocationExpression)expr;

					return e.Update(
						Transform(e.Expression),
						Transform(e.Arguments));
				}

				case ExpressionType.Lambda:
				{
					var e = (LambdaExpression)expr;
					var b = Transform(e.Body);
					var p = Transform(e.Parameters);

					return b != e.Body || p != e.Parameters ? Expression.Lambda(expr.Type, b, p) : expr;
				}

				case ExpressionType.ListInit:
				{
					var e = (ListInitExpression)expr;

					return e.Update(
						(NewExpression)Transform(e.NewExpression),
						Transform(e.Initializers, TransformElementInit));
				}

				case ExpressionType.MemberAccess:
				{
					var e  = (MemberExpression)expr;

					return e.Update(Transform(e.Expression));
				}

				case ExpressionType.MemberInit:
				{
					var e  = (MemberInitExpression)expr;

					return e.Update(
						(NewExpression)Transform(e.NewExpression),
						Transform(e.Bindings, TransformMemberBinding));
				}

				case ExpressionType.New:
				{
					var e = (NewExpression)expr;

					return e.Update(Transform(e.Arguments));
				}

				case ExpressionType.NewArrayBounds:
				{
					var e  = (NewArrayExpression)expr;

					return e.Update(Transform(e.Expressions));
				}

				case ExpressionType.NewArrayInit:
				{
					var e  = (NewArrayExpression)expr;

					return e.Update(Transform(e.Expressions));
				}

				case ExpressionType.TypeEqual:
				case ExpressionType.TypeIs:
				{
					var e  = (TypeBinaryExpression)expr;

					return e.Update(Transform(e.Expression));
				}

				case ExpressionType.Block:
				{
					var e  = (BlockExpression)expr;

					return e.Update(
						Transform(e.Variables),
						Transform(e.Expressions));
				}

				case ExpressionType.DebugInfo:
				case ExpressionType.Default  :
				case ExpressionType.Constant :
				case ExpressionType.Parameter: return expr;

				case ChangeTypeExpression.ChangeTypeType:
				{
					var e  = (ChangeTypeExpression)expr;
					var ex = Transform(e.Expression)!;

					if (ex == e.Expression)
						return expr;

					if (ex.Type == e.Type)
						return ex;

					return new ChangeTypeExpression(ex, e.Type);
				}

				case ExpressionType.Dynamic:
				{
					var e = (DynamicExpression)expr;

					return e.Update(Transform(e.Arguments));
				}

				case ExpressionType.Goto:
				{
					var e = (GotoExpression)expr;

					return e.Update(
						e.Target,
						Transform(e.Value));
				}

				case ExpressionType.Index:
				{
					var e = (IndexExpression)expr;

					return e.Update(
						Transform(e.Object!),
						Transform(e.Arguments));
				}

				case ExpressionType.Label:
				{
					var e = (LabelExpression)expr;

					return e.Update(
						e.Target,
						Transform(e.DefaultValue));
				}

				case ExpressionType.RuntimeVariables:
				{
					var e = (RuntimeVariablesExpression)expr;

					return e.Update(Transform(e.Variables));
				}

				case ExpressionType.Loop:
				{
					var e = (LoopExpression)expr;

					return e.Update(
						e.BreakLabel,
						e.ContinueLabel,
						Transform(e.Body));
				}

				case ExpressionType.Switch:
				{
					var e = (SwitchExpression)expr;

					return e.Update(
						Transform(e.SwitchValue),
						Transform(e.Cases, TransformSwitchCase),
						Transform(e.DefaultBody));
				}

				case ExpressionType.Try:
				{
					var e = (TryExpression)expr;

					return e.Update(
						Transform(e.Body),
						Transform(e.Handlers, TransformCatchBlock),
						Transform(e.Finally),
						Transform(e.Fault));
				}

				case ExpressionType.Extension:
					return TransformXE(expr);

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr.NodeType}");
			}
		}

		private CatchBlock TransformCatchBlock(CatchBlock h)
		{
			return h.Update(
				(ParameterExpression?)Transform(h.Variable),
				Transform(h.Filter),
				Transform(h.Body));
		}

		private SwitchCase TransformSwitchCase(SwitchCase cs)
		{
			return cs.Update(
				Transform(cs.TestValues),
				Transform(cs.Body));
		}

		static ReadOnlyCollection<T> Transform<T>(ReadOnlyCollection<T> source, Func<T, T> func)
			where T : class
		{
			List<T>? list = null;

			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var e    = func(item);

				if (e != item)
					(list ??= new(source))[i] = e;
			}

			return list?.AsReadOnly() ?? source;
		}

		ReadOnlyCollection<T> Transform<T>(ReadOnlyCollection<T> source)
			where T : Expression
		{
			List<T>? list = null;

			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var e    = (T)Transform(item)!;

				if (e != item)
					(list ??= new(source))[i] = e;
			}

			return list?.AsReadOnly() ?? source;
		}

		ElementInit TransformElementInit(ElementInit p)
		{
			return p.Update(Transform(p.Arguments));
		}

		MemberBinding TransformMemberBinding(MemberBinding b)
		{
			switch (b.BindingType)
			{
				case MemberBindingType.Assignment:
				{
					var ma = (MemberAssignment)b;
					var ex = Transform(ma.Expression)!;

					if (ex != ma.Expression)
					{
						var memberType = ma.Member.GetMemberType();
						if (ex.Type != memberType)
							ex = Expression.Convert(ex, memberType);
						ma = ma.Update(ex);
					}

					return ma;
				}

				case MemberBindingType.ListBinding:
				{
					var ml = (MemberListBinding)b;

					return ml.Update(Transform(ml.Initializers, TransformElementInit));
				}

				case MemberBindingType.MemberBinding:
				{
					var mm = (MemberMemberBinding)b;

					return mm.Update(Transform(mm.Bindings, TransformMemberBinding));
				}
			}

			return b;
		}

		// ReSharper disable once InconsistentNaming
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformXE(Expression expr)
		{
			if (expr is SqlGenericConstructorExpression generic)
			{
				generic = generic.ReplaceAssignments(Transform(generic.Assignments, TransformAssignments));

				generic = generic.ReplaceParameters(Transform(generic.Parameters, TransformParameters));

				return generic;
			}

			if (expr is SqlGenericParamAccessExpression paramAccess)
			{
				return paramAccess.Update(Transform(paramAccess.Constructor));
			}

			if (expr is SqlReaderIsNullExpression isNullExpression)
			{
				return isNullExpression.Update((SqlPlaceholderExpression)Transform(isNullExpression.Placeholder));
			}

			if (expr is SqlAdjustTypeExpression adjustType)
			{
				return adjustType.Update(Transform(adjustType.Expression));
			}

			if (expr is MarkerExpression placeholder)
			{
				return placeholder.Update(Transform(placeholder.InnerExpression));
			}

			if (expr is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				return defaultIfEmptyExpression.Update(
					Transform(defaultIfEmptyExpression.InnerExpression),
					Transform(defaultIfEmptyExpression.NotNullExpressions));
			}

			return expr;
		}

		private SqlGenericConstructorExpression.Assignment TransformAssignments(SqlGenericConstructorExpression.Assignment a)
		{
			return a.WithExpression(Transform(a.Expression));
		}

		private SqlGenericConstructorExpression.Parameter TransformParameters(SqlGenericConstructorExpression.Parameter p)
		{
			return p.WithExpression(Transform(p.Expression));
		}
	}
}
