using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions
{
	internal readonly struct TransformVisitor<TContext>
	{
		private readonly TContext?                               _context;
		private readonly Func<TContext, Expression, Expression>? _func;
		private readonly Func<Expression, Expression>?           _staticFunc;

		public TransformVisitor(TContext context, Func<TContext, Expression, Expression> func)
		{
			_context    = context;
			_func       = func;
			_staticFunc = null;
		}

		public TransformVisitor(Func<Expression, Expression> func)
		{
			_context    = default;
			_func       = null;
			_staticFunc = func;
		}

		/// <summary>
		/// Creates reusable static visitor.
		/// </summary>
		public static TransformVisitor<object?> Create(Func<Expression, Expression> func)
		{
			return new TransformVisitor<object?>(func);
		}

		/// <summary>
		/// Creates reusable visitor with static context.
		/// </summary>
		public static TransformVisitor<TContext> Create(TContext context, Func<TContext, Expression, Expression> func)
		{
			return new TransformVisitor<TContext>(context, func);
		}

		[return: NotNullIfNotNull("expr")]
		public Expression? Transform(Expression? expr)
		{
			if (expr == null)
				return null;

			var ex = _staticFunc != null ? _staticFunc(expr) : _func!(_context!, expr);
			if (ex != expr)
				return ex;

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
				case ExpressionType.SubtractAssignChecked: return TransformX    ((BinaryExpression          )expr);
				case ExpressionType.ArrayLength          :
				case ExpressionType.Convert              :
				case ExpressionType.ConvertChecked       :
				case ExpressionType.Negate               :
				case ExpressionType.NegateChecked        :
				case ExpressionType.Not                  :
				case ExpressionType.Quote                :
				case ExpressionType.TypeAs               :
				case ExpressionType.UnaryPlus            :
				case ExpressionType.Decrement            :
				case ExpressionType.Increment            :
				case ExpressionType.IsFalse              :
				case ExpressionType.IsTrue               :
				case ExpressionType.Throw                :
				case ExpressionType.Unbox                :
				case ExpressionType.PreIncrementAssign   :
				case ExpressionType.PreDecrementAssign   :
				case ExpressionType.PostIncrementAssign  :
				case ExpressionType.PostDecrementAssign  :
				case ExpressionType.OnesComplement       : return TransformX    ((UnaryExpression           )expr);
				case ExpressionType.Call                 : return TransformX    ((MethodCallExpression      )expr);
				case ExpressionType.Lambda               : return TransformX    ((LambdaExpression          )expr);
				case ExpressionType.ListInit             : return TransformX    ((ListInitExpression        )expr);
				case ExpressionType.MemberAccess         : return TransformX    ((MemberExpression          )expr);
				case ExpressionType.MemberInit           : return TransformX    ((MemberInitExpression      )expr);
				case ExpressionType.NewArrayBounds       : return TransformX    ((NewArrayExpression        )expr);
				case ExpressionType.NewArrayInit         : return TransformXInit((NewArrayExpression        )expr);
				case ChangeTypeExpression.ChangeTypeType : return TransformX    ((ChangeTypeExpression      )expr);
				case ExpressionType.DebugInfo            :
				case ExpressionType.Default              :
				case ExpressionType.Constant             :
				case ExpressionType.Parameter            : return expr;
				case ExpressionType.Switch               : return TransformX    ((SwitchExpression          )expr);
				case ExpressionType.Try                  : return TransformX    ((TryExpression             )expr);
				case ExpressionType.Extension            : return TransformXE   (                            expr);

				case ExpressionType.Dynamic:
					return ((DynamicExpression)expr)
						.Update(Transform(((DynamicExpression)expr).Arguments));

				case ExpressionType.New:
					return ((NewExpression)expr)
						.Update(Transform(((NewExpression)expr).Arguments));

				case ExpressionType.TypeEqual:
				case ExpressionType.TypeIs   :
					return ((TypeBinaryExpression)expr)
						.Update(Transform(((TypeBinaryExpression)expr).Expression));

				case ExpressionType.RuntimeVariables     :
					return ((RuntimeVariablesExpression)expr)
						.Update(Transform(((RuntimeVariablesExpression)expr).Variables));

				case ExpressionType.Conditional:
					return ((ConditionalExpression)expr).Update(
						Transform(((ConditionalExpression)expr).Test),
						Transform(((ConditionalExpression)expr).IfTrue),
						Transform(((ConditionalExpression)expr).IfFalse));

				case ExpressionType.Invoke:
					return ((InvocationExpression)expr).Update(
						Transform(((InvocationExpression)expr).Expression),
						Transform(((InvocationExpression)expr).Arguments));

				case ExpressionType.Block:
					return ((BlockExpression)expr).Update(
						Transform(((BlockExpression)expr).Variables),
						Transform(((BlockExpression)expr).Expressions));

				case ExpressionType.Goto:
					return ((GotoExpression)expr).Update(
						((GotoExpression)expr).Target,
						Transform(((GotoExpression)expr).Value));

				case ExpressionType.Index:
					return ((IndexExpression)expr).Update(
						Transform(((IndexExpression)expr).Object),
						Transform(((IndexExpression)expr).Arguments));

				case ExpressionType.Label:
					return ((LabelExpression)expr).Update(
						((LabelExpression)expr).Target,
						Transform(((LabelExpression)expr).DefaultValue));

				case ExpressionType.Loop:
					return ((LoopExpression)expr).Update(
						((LoopExpression)expr).BreakLabel,
						((LoopExpression)expr).ContinueLabel,
						Transform(((LoopExpression)expr).Body));

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr.NodeType}");
			}
		}

		// ReSharper disable once InconsistentNaming
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformXE(Expression expr) => expr;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(TryExpression e)
		{
			var b = Transform(e.Body);
			var c = Transform(e.Handlers, TransformCatchBlock);
			var f = Transform(e.Finally);
			var t = Transform(e.Fault);

			return e.Update(b, c, f, t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CatchBlock TransformCatchBlock(CatchBlock h)
		{
			return h.Update(
				(ParameterExpression?)Transform(h.Variable),
				Transform(h.Filter),
				Transform(h.Body));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(SwitchExpression e)
		{
			var s = Transform(e.SwitchValue);
			var c = Transform(e.Cases, TransformSwitchCase);
			var d = Transform(e.DefaultBody);

			return e.Update(s, c, d);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SwitchCase TransformSwitchCase(SwitchCase cs)
		{
			return cs.Update(
				Transform(cs.TestValues),
				Transform(cs.Body));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(ChangeTypeExpression e)
		{
			var ex = Transform(e.Expression)!;

			if (ex == e.Expression)
				return e;

			if (ex.Type == e.Type)
				return ex;

			return new ChangeTypeExpression(ex, e.Type);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformXInit(NewArrayExpression e)
		{
			var ex = Transform(e.Expressions);

			return ex != e.Expressions ? Expression.NewArrayInit(e.Type.GetElementType(), ex) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(NewArrayExpression e)
		{
			var ex = Transform(e.Expressions);

			return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(MemberInitExpression e)
		{
			return e.Update(
				(NewExpression)Transform(e.NewExpression)!,
				Transform(e.Bindings, Modify));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(MemberExpression e)
		{
			var ex = Transform(e.Expression);

			return ex != e.Expression ? Expression.MakeMemberAccess(ex, e.Member) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(ListInitExpression e)
		{
			var n = Transform(e.NewExpression)!;
			var i = Transform(e.Initializers, TransformElementInit);

			return n != e.NewExpression || i != e.Initializers ? Expression.ListInit((NewExpression)n, i) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ElementInit TransformElementInit(ElementInit p)
		{
			var args = Transform(p.Arguments);
			return args != p.Arguments ? Expression.ElementInit(p.AddMethod, args) : p;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(LambdaExpression e)
		{
			var b = Transform(e.Body);
			var p = Transform(e.Parameters);

			return b != e.Body || p != e.Parameters ? Expression.Lambda(e.Type, b, p) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(MethodCallExpression e)
		{
			var o = Transform(e.Object);
			var a = Transform(e.Arguments);

			return o != e.Object || a != e.Arguments ? Expression.Call(o, e.Method, a) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(UnaryExpression e)
		{
			var o = Transform(e.Operand);
			return o != e.Operand ? Expression.MakeUnary(e.NodeType, o, e.Type, e.Method) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(BinaryExpression e)
		{
			var c = Transform(e.Conversion);
			var l = Transform(e.Left);
			var r = Transform(e.Right);

			return c != e.Conversion || l != e.Left || r != e.Right
				? Expression.MakeBinary(e.NodeType, l, r, e.IsLiftedToNull, e.Method, (LambdaExpression?)c)
				: e;
		}

		IEnumerable<T> Transform<T>(IList<T> source, Func<T, T> func)
			where T : class
		{
			List<T>? list = null;

			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var e    = func(item);

				if (e != item)
				{
					if (list == null)
						list = new List<T>(source);
					list[i] = e;
				}
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
				{
					if (list == null)
						list    = new List<T>(source);
					list[i] = e;
				}
			}

			return list ?? source;
		}

		MemberBinding Modify(MemberBinding b)
		{
			switch (b.BindingType)
			{
				case MemberBindingType.Assignment:
				{
					var ma = (MemberAssignment) b;
					return ma.Update(Transform(ma.Expression));
				}

				case MemberBindingType.ListBinding:
				{
					var ml = (MemberListBinding) b;
					var i  = Transform(ml.Initializers, TransformElementInit);

					if (i != ml.Initializers)
						ml = Expression.ListBind(ml.Member, i);

					return ml;
				}

				case MemberBindingType.MemberBinding:
				{
					var mm = (MemberMemberBinding) b;
					var bs = Transform<MemberBinding>(mm.Bindings, Modify);

					if (bs != mm.Bindings)
						mm = Expression.MemberBind(mm.Member);

					return mm;
				}
			}

			return b;
		}
	}
}
