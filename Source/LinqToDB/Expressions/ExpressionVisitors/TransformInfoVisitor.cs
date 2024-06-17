using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions
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

			TransformInfo ti;

			do
			{
				ti = _staticFunc != null ? _staticFunc(expr) : _func!(_context!, expr);
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
					var c = Transform(e.Conversion);
					var l = Transform(e.Left);
					var r = Transform(e.Right);

					return e.Update(l, (LambdaExpression?)c, r);
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
					var o = Transform(e.Object);
					var a = Transform(e.Arguments);

					return e.Update(o, a);
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expr;
					var s = Transform(e.Test);
					var t = Transform(e.IfTrue);
					var f = Transform(e.IfFalse);

					return e.Update(s, t, f);
				}

				case ExpressionType.Invoke:
				{
					var e  = (InvocationExpression)expr;
					var ex = Transform(e.Expression);
					var a  = Transform(e.Arguments);

					return e.Update(ex, a);
				}

				case ExpressionType.Lambda:
				{
					var e = (LambdaExpression)expr;
					var b = Transform(e.Body);
					var p = Transform(e.Parameters);

					return b != e.Body || p != e.Parameters ? Expression.Lambda(ti.Expression.Type, b, p) : expr;
				}

				case ExpressionType.ListInit:
				{
					var e = (ListInitExpression)expr;
					var n = Transform(e.NewExpression)!;
					var i = Transform(e.Initializers,  TransformElementInit);

					return e.Update((NewExpression)n, i);
				}

				case ExpressionType.MemberAccess:
				{
					var e  = (MemberExpression)expr;
					var ex = Transform(e.Expression);

					return e.Update(ex);
				}

				case ExpressionType.MemberInit:
				{
					var e  = (MemberInitExpression)expr;
					var ne = Transform(e.NewExpression)!;
					var bb = Transform(e.Bindings, TransformMemberBinding);

					return e.Update((NewExpression)ne, bb);
				}

				case ExpressionType.New:
				{
					var e = (NewExpression)expr;
					var a = Transform(e.Arguments);

					return e.Update(a);
				}

				case ExpressionType.NewArrayBounds:
				{
					var e  = (NewArrayExpression)expr;
					var ex = Transform(e.Expressions);

					return e.Update(ex);
				}

				case ExpressionType.NewArrayInit:
				{
					var e  = (NewArrayExpression)expr;
					var ex = Transform(e.Expressions);

					return e.Update(ex);
				}

				case ExpressionType.TypeEqual:
				case ExpressionType.TypeIs:
				{
					var e  = (TypeBinaryExpression)expr;
					var ex = Transform(e.Expression);

					return e.Update(ex);
				}

				case ExpressionType.Block:
				{
					var e  = (BlockExpression)expr;
					var ex = Transform(e.Expressions);
					var v  = Transform(e.Variables);

					return e.Update(v, ex);
				}

				case ExpressionType.DebugInfo:
				case ExpressionType.Default  :
				case ExpressionType.Constant :
				case ExpressionType.Parameter: return ti.Expression;

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
					var e    = (DynamicExpression)expr;
					var args = Transform(e.Arguments);

					return e.Update(args);
				}

				case ExpressionType.Goto:
				{
					var e = (GotoExpression)expr;
					var v = Transform(e.Value);

					return e.Update(e.Target, v);
				}

				case ExpressionType.Index:
				{
					var e = (IndexExpression)expr;
					var o = Transform(e.Object!);
					var a = Transform(e.Arguments);

					return e.Update(o, a);
				}

				case ExpressionType.Label:
				{
					var e = (LabelExpression)expr;
					var v = Transform(e.DefaultValue);

					return e.Update(e.Target, v);
				}

				case ExpressionType.RuntimeVariables:
				{
					var e = (RuntimeVariablesExpression)expr;
					var v = Transform(e.Variables);

					return e.Update(v);
				}

				case ExpressionType.Loop:
				{
					var e = (LoopExpression)expr;
					var b = Transform(e.Body);

					return e.Update(e.BreakLabel, e.ContinueLabel, b);
				}

				case ExpressionType.Switch:
				{
					var e = (SwitchExpression)expr;
					var s = Transform(e.SwitchValue);
					var c = Transform(e.Cases, TransformSwitchCase);
					var d = Transform(e.DefaultBody);

					return e.Update(s, c, d);
				}

				case ExpressionType.Try:
				{
					var e = (TryExpression)expr;
					var b = Transform(e.Body);
					var c = Transform(e.Handlers, TransformCatchBlock);
					var f = Transform(e.Finally);
					var t = Transform(e.Fault);

					return e.Update(b, c, f, t);
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

		static IEnumerable<T> Transform<T>(IList<T> source, Func<T, T> func)
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

			return list ?? source;
		}

		IEnumerable<T> Transform<T>(IList<T> source)
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

			return list ?? source;
		}

		ElementInit TransformElementInit(ElementInit p)
		{
			var args = Transform(p.Arguments);
			return args != p.Arguments ? Expression.ElementInit(p.AddMethod, args) : p;
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
						ma = Expression.Bind(ma.Member, ex);
					}

					return ma;
				}

				case MemberBindingType.ListBinding:
				{
					var ml = (MemberListBinding)b;
					var i  = Transform(ml.Initializers, TransformElementInit);

					if (!ReferenceEquals(i, ml.Initializers))
						ml = Expression.ListBind(ml.Member, i);

					return ml;
				}

				case MemberBindingType.MemberBinding:
				{
					var mm = (MemberMemberBinding)b;
					var bs = Transform(mm.Bindings, TransformMemberBinding);

					if (!ReferenceEquals(bs, mm.Bindings))
						mm = Expression.MemberBind(mm.Member, bs);

					return mm;
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
				var assignments = Transform(generic.Assignments, TransformAssignments);

				if (!ReferenceEquals(assignments, generic.Assignments))
				{
					generic = generic.ReplaceAssignments(assignments.ToList());
				}

				var parameters = Transform(generic.Parameters, TransformParameters);

				if (!ReferenceEquals(parameters, generic.Parameters))
				{
					generic = generic.ReplaceParameters(parameters.ToList());
				}

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

			if (expr is PlaceholderExpression { PlaceholderType: PlaceholderType.Closure })
			{
				return expr;
			}

			if (expr is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				var inner = Transform(defaultIfEmptyExpression.InnerExpression);
				var items = Transform(defaultIfEmptyExpression.NotNullExpressions);

				return defaultIfEmptyExpression.Update(inner,
					ReferenceEquals(items, defaultIfEmptyExpression.NotNullExpressions)
						? defaultIfEmptyExpression.NotNullExpressions
						: items.ToList().AsReadOnly());
			}

			return expr;
		}

		private SqlGenericConstructorExpression.Assignment TransformAssignments(SqlGenericConstructorExpression.Assignment a)
		{
			var aExpr = Transform(a.Expression);
			return a.WithExpression(aExpr);
		}

		private SqlGenericConstructorExpression.Parameter TransformParameters(SqlGenericConstructorExpression.Parameter p)
		{
			var aExpr = Transform(p.Expression);
			return p.WithExpression(aExpr);
		}
	}
}
