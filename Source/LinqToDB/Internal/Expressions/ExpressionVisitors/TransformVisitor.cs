using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal readonly struct TransformVisitor<TContext>
	{
		private readonly TContext?                             _context;
		private readonly Func<TContext,Expression,Expression>? _func;
		private readonly Func<Expression,Expression>?          _staticFunc;

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

		[return: NotNullIfNotNull(nameof(expr))]
		public Expression? Transform(Expression? expr)
		{
			if (expr == null)
				return null;

			var ex = _staticFunc != null ? _staticFunc(expr) : _func!(_context!, expr);
			if (ex != expr)
				return ex;

			return expr.NodeType switch
			{
				ExpressionType.Add or
				ExpressionType.AddChecked or
				ExpressionType.And or
				ExpressionType.AndAlso or
				ExpressionType.ArrayIndex or
				ExpressionType.Assign or
				ExpressionType.Coalesce or
				ExpressionType.Divide or
				ExpressionType.Equal or
				ExpressionType.ExclusiveOr or
				ExpressionType.GreaterThan or
				ExpressionType.GreaterThanOrEqual or
				ExpressionType.LeftShift or
				ExpressionType.LessThan or
				ExpressionType.LessThanOrEqual or
				ExpressionType.Modulo or
				ExpressionType.Multiply or
				ExpressionType.MultiplyChecked or
				ExpressionType.NotEqual or
				ExpressionType.Or or
				ExpressionType.OrElse or
				ExpressionType.Power or
				ExpressionType.RightShift or
				ExpressionType.Subtract or
				ExpressionType.SubtractChecked or
				ExpressionType.AddAssign or
				ExpressionType.AndAssign or
				ExpressionType.DivideAssign or
				ExpressionType.ExclusiveOrAssign or
				ExpressionType.LeftShiftAssign or
				ExpressionType.ModuloAssign or
				ExpressionType.MultiplyAssign or
				ExpressionType.OrAssign or
				ExpressionType.PowerAssign or
				ExpressionType.RightShiftAssign or
				ExpressionType.SubtractAssign or
				ExpressionType.AddAssignChecked or
				ExpressionType.MultiplyAssignChecked or
				ExpressionType.SubtractAssignChecked =>
					TransformX((BinaryExpression)expr),

				ExpressionType.ArrayLength or
				ExpressionType.Convert or
				ExpressionType.ConvertChecked or
				ExpressionType.Negate or
				ExpressionType.NegateChecked or
				ExpressionType.Not or
				ExpressionType.Quote or
				ExpressionType.TypeAs or
				ExpressionType.UnaryPlus or
				ExpressionType.Decrement or
				ExpressionType.Increment or
				ExpressionType.IsFalse or
				ExpressionType.IsTrue or
				ExpressionType.Throw or
				ExpressionType.Unbox or
				ExpressionType.PreIncrementAssign or
				ExpressionType.PreDecrementAssign or
				ExpressionType.PostIncrementAssign or
				ExpressionType.PostDecrementAssign or
				ExpressionType.OnesComplement =>
					TransformX((UnaryExpression)expr),

				ExpressionType.DebugInfo or
				ExpressionType.Default or
				ExpressionType.Constant or
				ExpressionType.Parameter =>
					expr,

				ExpressionType.Call                 => TransformX((MethodCallExpression)expr),
				ExpressionType.Lambda               => TransformX((LambdaExpression)expr),
				ExpressionType.ListInit             => TransformX((ListInitExpression)expr),
				ExpressionType.MemberAccess         => TransformX((MemberExpression)expr),
				ExpressionType.MemberInit           => TransformX((MemberInitExpression)expr),
				ExpressionType.NewArrayBounds       => TransformX((NewArrayExpression)expr),
				ExpressionType.NewArrayInit         => TransformXInit((NewArrayExpression)expr),
				ChangeTypeExpression.ChangeTypeType => TransformX((ChangeTypeExpression)expr),
				ExpressionType.Switch               => TransformX((SwitchExpression)expr),
				ExpressionType.Try                  => TransformX((TryExpression)expr),
				ExpressionType.Extension            => TransformXE(expr),

				ExpressionType.Dynamic =>
					((DynamicExpression)expr)
						.Update(Transform(((DynamicExpression)expr).Arguments)
					),

				ExpressionType.New =>
					((NewExpression)expr)
						.Update(Transform(((NewExpression)expr).Arguments)
					),

				ExpressionType.TypeEqual or
				ExpressionType.TypeIs =>
					((TypeBinaryExpression)expr)
						.Update(Transform(((TypeBinaryExpression)expr).Expression)
					),

				ExpressionType.RuntimeVariables =>
					((RuntimeVariablesExpression)expr)
						.Update(Transform(((RuntimeVariablesExpression)expr).Variables)
					),

				ExpressionType.Conditional =>
					((ConditionalExpression)expr).Update(
						Transform(((ConditionalExpression)expr).Test),
						Transform(((ConditionalExpression)expr).IfTrue),
						Transform(((ConditionalExpression)expr).IfFalse)
					),

				ExpressionType.Invoke =>
					((InvocationExpression)expr).Update(
						Transform(((InvocationExpression)expr).Expression),
						Transform(((InvocationExpression)expr).Arguments)
					),

				ExpressionType.Block =>
					((BlockExpression)expr).Update(
						Transform(((BlockExpression)expr).Variables),
						Transform(((BlockExpression)expr).Expressions)
					),

				ExpressionType.Goto =>
					((GotoExpression)expr).Update(
						((GotoExpression)expr).Target,
						Transform(((GotoExpression)expr).Value)
					),

				ExpressionType.Index =>
					((IndexExpression)expr).Update(
						Transform(((IndexExpression)expr).Object!),
						Transform(((IndexExpression)expr).Arguments)
					),

				ExpressionType.Label =>
					((LabelExpression)expr).Update(
						((LabelExpression)expr).Target,
						Transform(((LabelExpression)expr).DefaultValue)
					),

				ExpressionType.Loop =>
					((LoopExpression)expr).Update(
						((LoopExpression)expr).BreakLabel,
						((LoopExpression)expr).ContinueLabel,
						Transform(((LoopExpression)expr).Body)
					),

				_ => throw new NotImplementedException($"Unhandled expression type: {expr.NodeType}"),
			};
		}

		// ReSharper disable once InconsistentNaming
		Expression TransformXE(Expression expr)
		{
			if (expr is SqlGenericConstructorExpression generic)
			{
				var assignments = Transform(this, generic.Assignments, TransformAssignments);

				generic = generic.ReplaceAssignments(assignments);

				var parameters = Transform(this, generic.Parameters, TransformParameters);

				generic = generic.ReplaceParameters(parameters);

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

			if (expr is SqlPathExpression)
			{
				return expr;
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

			if (expr is SqlValidateExpression validateExpression)
			{
				return validateExpression.Update(Transform(validateExpression.InnerExpression));
			}

			return expr;
		}

		private SqlGenericConstructorExpression.Assignment TransformAssignments(TransformVisitor<TContext> visitor, SqlGenericConstructorExpression.Assignment a)
		{
			return a.WithExpression(Transform(a.Expression));
		}

		private SqlGenericConstructorExpression.Parameter TransformParameters(TransformVisitor<TContext> visitor, SqlGenericConstructorExpression.Parameter p)
		{
			return p.WithExpression(Transform(p.Expression));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(TryExpression e)
		{
			return e.Update(
				Transform(e.Body),
				Transform(this, e.Handlers, TransformCatchBlock),
				Transform(e.Finally),
				Transform(e.Fault));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CatchBlock TransformCatchBlock(TransformVisitor<TContext> visitor, CatchBlock h)
		{
			return h.Update(
				(ParameterExpression?)visitor.Transform(h.Variable),
				visitor.Transform(h.Filter),
				visitor.Transform(h.Body));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(SwitchExpression e)
		{
			return e.Update(
				Transform(e.SwitchValue),
				Transform(this, e.Cases, TransformSwitchCase),
				Transform(e.DefaultBody));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SwitchCase TransformSwitchCase(TransformVisitor<TContext> visitor, SwitchCase cs)
		{
			return cs.Update(
				visitor.Transform(cs.TestValues),
				visitor.Transform(cs.Body));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression TransformX(ChangeTypeExpression e)
		{
			var ex = Transform(e.Expression);

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

			return ex != e.Expressions ? Expression.NewArrayInit(e.Type.GetElementType()!, ex) : e;
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
				(NewExpression)Transform(e.NewExpression),
				Transform(this, e.Bindings, Modify));
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
			var n = Transform(e.NewExpression);
			var i = Transform(this, e.Initializers, TransformElementInit);

			return n != e.NewExpression || i != e.Initializers ? Expression.ListInit((NewExpression)n, i) : e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ElementInit TransformElementInit(TransformVisitor<TContext> visitor, ElementInit p)
		{
			return p.Update(visitor.Transform(p.Arguments));
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

		static ReadOnlyCollection<T> Transform<T>(TransformVisitor<TContext> visitor, ReadOnlyCollection<T> source, Func<TransformVisitor<TContext>, T, T> func)
			where T : class
		{
			List<T>? list = null;

			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var e    = func(visitor, item);

				if (e != item)
				{
					list ??= [.. source];
					list[i] = e;
				}
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
				var e    = (T)Transform(item);

				if (e != item)
				{
					list    ??= [.. source];
					list[i] = e;
				}
			}

			return list?.AsReadOnly() ?? source;
		}

		static MemberBinding Modify(TransformVisitor<TContext> visitor, MemberBinding b)
		{
			switch (b.BindingType)
			{
				case MemberBindingType.Assignment:
				{
					var ma = (MemberAssignment) b;
					return ma.Update(visitor.Transform(ma.Expression));
				}

				case MemberBindingType.ListBinding:
				{
					var ml = (MemberListBinding) b;

					return ml.Update(Transform(visitor, ml.Initializers, TransformElementInit));
				}

				case MemberBindingType.MemberBinding:
				{
					var mm = (MemberMemberBinding) b;

					return mm.Update(Transform(visitor, mm.Bindings, Modify));
				}
			}

			return b;
		}
	}
}
