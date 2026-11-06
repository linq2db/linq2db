using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	static class EqualsToVisitor
	{
		static readonly ObjectPool<ParameterBindings> BindingsPool = new(() => new ParameterBindings(), b => b.Clear(), 100);

		internal static bool EqualsTo(this Expression? expr1, Expression? expr2)
		{
			if (expr1 == expr2)
				return true;

			if (expr1 == null || expr2 == null || expr1.Type != expr2.Type)
				return false;

			using var bindings = BindingsPool.Allocate();
			return expr1.EqualsTo(expr2, bindings.Value);
		}

		static bool EqualsTo(this Expression? expr1, Expression? expr2, ParameterBindings bindings)
		{
			if (expr1 == expr2 && (expr1 == null || bindings.AreIdentical))
				return true;

			if (expr1 == null || expr2 == null || expr1.Type != expr2.Type)
				return false;

			if (expr1.NodeType != expr2.NodeType)
			{
				// special cache case
				return expr1.NodeType == ExpressionType.Extension && expr2.NodeType == ExpressionType.Constant && expr1 is ConstantPlaceholderExpression;
			}

			return expr1.NodeType switch
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
				ExpressionType.SubtractChecked =>
					((BinaryExpression)expr1).Method == ((BinaryExpression)expr2).Method &&
					((BinaryExpression)expr1).Conversion.EqualsTo(((BinaryExpression)expr2).Conversion, bindings) &&
					((BinaryExpression)expr1).Left.EqualsTo(((BinaryExpression)expr2).Left, bindings) &&
					((BinaryExpression)expr1).Right.EqualsTo(((BinaryExpression)expr2).Right, bindings),

				ExpressionType.ArrayLength or
				ExpressionType.Convert or
				ExpressionType.ConvertChecked or
				ExpressionType.Negate or
				ExpressionType.NegateChecked or
				ExpressionType.Not or
				ExpressionType.Quote or
				ExpressionType.TypeAs or
				ExpressionType.Throw or
				ExpressionType.UnaryPlus =>
					((UnaryExpression)expr1).Method == ((UnaryExpression)expr2).Method &&
					((UnaryExpression)expr1).Operand.EqualsTo(((UnaryExpression)expr2).Operand, bindings),

				ExpressionType.Conditional =>
					((ConditionalExpression)expr1).Test.EqualsTo(((ConditionalExpression)expr2).Test, bindings) &&
					((ConditionalExpression)expr1).IfTrue.EqualsTo(((ConditionalExpression)expr2).IfTrue, bindings) &&
					((ConditionalExpression)expr1).IfFalse.EqualsTo(((ConditionalExpression)expr2).IfFalse, bindings),

				ExpressionType.Call         => EqualsToX((MethodCallExpression)expr1, (MethodCallExpression)expr2, bindings),
				ExpressionType.Constant     => EqualsToX((ConstantExpression)expr1, (ConstantExpression)expr2),
				ExpressionType.Invoke       => EqualsToX((InvocationExpression)expr1, (InvocationExpression)expr2, bindings),
				ExpressionType.Index        => EqualsToX((IndexExpression)expr1, (IndexExpression)expr2, bindings),
				ExpressionType.Lambda       => EqualsToX((LambdaExpression)expr1, (LambdaExpression)expr2, bindings),
				ExpressionType.ListInit     => EqualsToX((ListInitExpression)expr1, (ListInitExpression)expr2, bindings),
				ExpressionType.MemberAccess => EqualsToX((MemberExpression)expr1, (MemberExpression)expr2, bindings),
				ExpressionType.MemberInit   => EqualsToX((MemberInitExpression)expr1, (MemberInitExpression)expr2, bindings),
				ExpressionType.New          => EqualsToX((NewExpression)expr1, (NewExpression)expr2, bindings),

				ExpressionType.NewArrayBounds or
				ExpressionType.NewArrayInit =>
					EqualsToX((NewArrayExpression)expr1, (NewArrayExpression)expr2, bindings),

				ExpressionType.Default   => true,
				ExpressionType.Parameter => bindings.Compare((ParameterExpression)expr1, (ParameterExpression)expr2),

				ExpressionType.TypeIs =>
					((TypeBinaryExpression)expr1).TypeOperand == ((TypeBinaryExpression)expr2).TypeOperand &&
					((TypeBinaryExpression)expr1).Expression.EqualsTo(((TypeBinaryExpression)expr2).Expression, bindings),

				ExpressionType.Block => EqualsToX((BlockExpression)expr1, (BlockExpression)expr2, bindings),
				ExpressionType.Try   => EqualsToX((TryExpression)expr1, (TryExpression)expr2, bindings),

				ChangeTypeExpression.ChangeTypeType =>
					((ChangeTypeExpression)expr1).Type == ((ChangeTypeExpression)expr2).Type &&
					((ChangeTypeExpression)expr1).Expression.EqualsTo(((ChangeTypeExpression)expr2).Expression, bindings),

				ExpressionType.Extension => EqualsExtensions(expr1, expr2, bindings),

				// A shared unsupported node cannot be accepted under different bindings.
				_ when expr1 == expr2 => false,
				_ => throw new InvalidOperationException($"Unhandled expression type: {expr1.NodeType}"),
			};
		}

		static bool EqualsExtensions(Expression expr1, Expression expr2, ParameterBindings bindings)
		{
			// A shared node cannot be accepted under different bindings.
			if (ReferenceEquals(expr1, expr2))
				return bindings.AreIdentical;

			return expr1.Equals(expr2);
		}

		static bool EqualsToX(BlockExpression expr1, BlockExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Expressions.Count != expr2.Expressions.Count)
				return false;

			var count = bindings.Count;
			try
			{
				if (!bindings.Push(expr1.Variables, expr2.Variables))
					return false;

				for (var i = 0; i < expr1.Expressions.Count; i++)
					if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], bindings))
						return false;

				return true;
			}
			finally
			{
				bindings.Pop(count);
			}
		}

		static bool EqualsToX(TryExpression expr1, TryExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Handlers.Count != expr2.Handlers.Count ||
				!expr1.Body.EqualsTo(expr2.Body, bindings) ||
				!expr1.Finally.EqualsTo(expr2.Finally, bindings) ||
				!expr1.Fault.EqualsTo(expr2.Fault, bindings))
				return false;

			for (var i = 0; i < expr1.Handlers.Count; i++)
			{
				var handler1 = expr1.Handlers[i];
				var handler2 = expr2.Handlers[i];
				if (handler1.Test != handler2.Test)
					return false;

				var count = bindings.Count;
				try
				{
					if (!bindings.Push(handler1.Variable, handler2.Variable) ||
						!handler1.Filter.EqualsTo(handler2.Filter, bindings) ||
						!handler1.Body.EqualsTo(handler2.Body, bindings))
						return false;
				}
				finally
				{
					bindings.Pop(count);
				}
			}

			return true;
		}

		static bool EqualsToX(NewArrayExpression expr1, NewArrayExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Expressions.Count != expr2.Expressions.Count)
				return false;

			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], bindings))
					return false;

			return true;
		}

		static bool EqualsToX(NewExpression expr1, NewExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count)
				return false;

			if (expr1.Members == null && expr2.Members != null)
				return false;

			if (expr1.Members != null && expr2.Members == null)
				return false;

			if (expr1.Constructor != expr2.Constructor)
				return false;

			if (expr1.Members != null)
			{
				if (expr1.Members.Count != expr2.Members!.Count)
					return false;

				for (var i = 0; i < expr1.Members.Count; i++)
					if (expr1.Members[i] != expr2.Members[i])
						return false;
			}

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], bindings))
					return false;

			return true;
		}

		static bool EqualsToX(MemberInitExpression expr1, MemberInitExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Bindings.Count != expr2.Bindings.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, bindings))
				return false;

			for (var i = 0; i < expr1.Bindings.Count; i++)
			{
				var b1 = expr1.Bindings[i];
				var b2 = expr2.Bindings[i];

				if (!CompareBindings(b1, b2, bindings))
					return false;
			}

			return true;
		}

		static bool CompareBindings(MemberBinding? b1, MemberBinding? b2, ParameterBindings bindings)
		{
			if (b1 == b2 && (b1 == null || bindings.AreIdentical))
				return true;

			if (b1 == null || b2 == null || b1.BindingType != b2.BindingType || b1.Member != b2.Member)
				return false;

			switch (b1.BindingType)
			{
				case MemberBindingType.Assignment:
					return ((MemberAssignment)b1).Expression.EqualsTo(((MemberAssignment)b2).Expression, bindings);

				case MemberBindingType.ListBinding:
					var ml1 = (MemberListBinding)b1;
					var ml2 = (MemberListBinding)b2;

					if (ml1.Initializers.Count != ml2.Initializers.Count)
						return false;

					for (var i = 0; i < ml1.Initializers.Count; i++)
					{
						var ei1 = ml1.Initializers[i];
						var ei2 = ml2.Initializers[i];

						if (ei1.AddMethod != ei2.AddMethod || ei1.Arguments.Count != ei2.Arguments.Count)
							return false;

						for (var j = 0; j < ei1.Arguments.Count; j++)
							if (!ei1.Arguments[j].EqualsTo(ei2.Arguments[j], bindings))
								return false;
					}

					break;

				case MemberBindingType.MemberBinding:
					var mm1 = (MemberMemberBinding)b1;
					var mm2 = (MemberMemberBinding)b2;

					if (mm1.Bindings.Count != mm2.Bindings.Count)
						return false;

					for (var i = 0; i < mm1.Bindings.Count; i++)
						if (!CompareBindings(mm1.Bindings[i], mm2.Bindings[i], bindings))
							return false;

					break;
			}

			return true;
		}

		static bool EqualsToX(MemberExpression expr1, MemberExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Member == expr2.Member)
			{
				return expr1.Expression.EqualsTo(expr2.Expression, bindings);
			}

			return false;
		}

		static bool EqualsToX(ListInitExpression expr1, ListInitExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Initializers.Count != expr2.Initializers.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, bindings))
				return false;

			for (var i = 0; i < expr1.Initializers.Count; i++)
			{
				var i1 = expr1.Initializers[i];
				var i2 = expr2.Initializers[i];

				if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
					return false;

				for (var j = 0; j < i1.Arguments.Count; j++)
					if (!i1.Arguments[j].EqualsTo(i2.Arguments[j], bindings))
						return false;
			}

			return true;
		}

		static bool EqualsToX(LambdaExpression expr1, LambdaExpression expr2, ParameterBindings bindings)
		{
			var count = bindings.Count;
			try
			{
				return bindings.Push(expr1.Parameters, expr2.Parameters) && expr1.Body.EqualsTo(expr2.Body, bindings);
			}
			finally
			{
				bindings.Pop(count);
			}
		}

		static bool EqualsToX(InvocationExpression expr1, InvocationExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || !expr1.Expression.EqualsTo(expr2.Expression, bindings))
				return false;

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], bindings))
					return false;

			return true;
		}

		static bool EqualsToX(IndexExpression expr1, IndexExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Indexer != expr2.Indexer || expr1.Arguments.Count != expr2.Arguments.Count || !expr1.Object.EqualsTo(expr2.Object, bindings))
				return false;

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], bindings))
					return false;

			return true;
		}

		static bool EqualsToX(ConstantExpression expr1, ConstantExpression expr2)
		{
			if (expr1.Value == null && expr2.Value == null)
				return true;

			var result = Equals(expr1.Value, expr2.Value);

			return result;
		}

		static bool EqualsToX(MethodCallExpression expr1, MethodCallExpression expr2, ParameterBindings bindings)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || expr1.Method != expr2.Method)
				return false;

			if (!expr1.Object.EqualsTo(expr2.Object, bindings))
				return false;

			var dependedParameters = SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(expr1.Method);

			if (dependedParameters == null)
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					if (!DefaultCompareArguments(expr1.Arguments[i], expr2.Arguments[i], bindings))
						return false;
				}
			}
			else
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					var dependentAttribute = dependedParameters[i];
					var arg1 = expr1.Arguments[i];
					var arg2 = expr2.Arguments[i];

					if (dependentAttribute != null)
					{
						if (arg1 is not ConstantPlaceholderExpression)
						{
							if (!dependentAttribute.ExpressionsEqual(bindings, arg1, arg2, static (scope, e1, e2) => e1.EqualsTo(e2, scope)))
								return false;
						}
					}
					else
					{
						if (!DefaultCompareArguments(arg1, arg2, bindings))
							return false;
					}
				}

			}

			return true;
		}

		static bool DefaultCompareArguments(Expression arg1, Expression arg2, ParameterBindings bindings)
		{
			return arg1.EqualsTo(arg2, bindings);
		}

		sealed class ParameterBindings
		{
			readonly List<(ParameterExpression First, ParameterExpression Second)> _pairs = new(8);

			public int Count => _pairs.Count;

			public bool AreIdentical
			{
				get
				{
					for (var i = _pairs.Count - 1; i >= 0; i--)
						if (!ReferenceEquals(_pairs[i].First, _pairs[i].Second))
							return false;

					return true;
				}
			}

			public void Clear() => _pairs.Clear();

			public void Pop(int count) => _pairs.RemoveRange(count, _pairs.Count - count);

			public bool Push(IReadOnlyList<ParameterExpression> first, IReadOnlyList<ParameterExpression> second)
			{
				if (first.Count != second.Count)
					return false;

				for (var i = 0; i < first.Count; i++)
					if (!AreCompatible(first[i], second[i]))
						return false;

				for (var i = 0; i < first.Count; i++)
					_pairs.Add((first[i], second[i]));

				return true;
			}

			public bool Push(ParameterExpression? first, ParameterExpression? second)
			{
				if (first == null || second == null)
					return first == second;

				if (!AreCompatible(first, second))
					return false;

				_pairs.Add((first, second));
				return true;
			}

			// A declared parameter's name is part of the key as well: the builder turns it into the table alias, so
			// two lambdas that differ only in the parameter name must not share one cached SQL text.
			static bool AreCompatible(ParameterExpression first, ParameterExpression second)
			{
				return first.Type == second.Type
					&& first.IsByRef == second.IsByRef
					&& string.Equals(first.Name, second.Name, StringComparison.Ordinal);
			}

			public bool Compare(ParameterExpression first, ParameterExpression second)
			{
				// Search both trees from the innermost declaration: a free parameter must
				// never match a bound one, even when their names or instances coincide.
				for (var i = _pairs.Count - 1; i >= 0; i--)
				{
					var (declaredFirst, declaredSecond) = _pairs[i];

					var firstBound  = ReferenceEquals(declaredFirst, first);
					var secondBound = ReferenceEquals(declaredSecond, second);
					if (firstBound || secondBound)
						return firstBound && secondBound;
				}

				// Accessor expressions also contain free runtime/context parameters.
				return string.Equals(first.Name, second.Name, StringComparison.Ordinal);
			}
		}
	}
}
