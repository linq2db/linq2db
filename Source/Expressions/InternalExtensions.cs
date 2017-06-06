using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using Linq;
	using LinqToDB.Extensions;

	static class InternalExtensions
	{
		#region IsConstant

		public static bool IsConstantable(this Type type)
		{
			if (type.IsEnumEx())
				return true;

			switch (type.GetTypeCodeEx())
			{
				case TypeCode.Int16   :
				case TypeCode.Int32   :
				case TypeCode.Int64   :
				case TypeCode.UInt16  :
				case TypeCode.UInt32  :
				case TypeCode.UInt64  :
				case TypeCode.SByte   :
				case TypeCode.Byte    :
				case TypeCode.Decimal :
				case TypeCode.Double  :
				case TypeCode.Single  :
				case TypeCode.Boolean :
				case TypeCode.String  :
				case TypeCode.Char    : return true;
			}

			if (type.IsNullable())
				return type.GetGenericArgumentsEx()[0].IsConstantable();

			return false;
		}

		#endregion

		#region EqualsTo

		internal static bool EqualsToOld(this Expression expr1, Expression expr2, Dictionary<Expression,QueryableAccessor> queryableAccessorDic)
		{
			return EqualsTo(expr1, expr2, new EqualsToInfo { QueryableAccessorDic = queryableAccessorDic });
		}

		internal static bool EqualsTo(this Expression ex1, Expression ex2, Dictionary<Expression,QueryableAccessor> queryableAccessorDic)
		{
			if (ex1 == null || ex2 == null || ex1.NodeType != ex2.NodeType || ex1.Type != ex2.Type)
				return false;

			var info = new EqualsToInfo { QueryableAccessorDic = queryableAccessorDic };

			using (var e1 = ex1.EnumerateParentFirst().GetEnumerator())
			using (var e2 = ex2.EnumerateParentFirst().GetEnumerator())
			{
				while (true)
				{
					var m1 = e1.MoveNext();
					var m2 = e2.MoveNext();
					if (m1 != m2)
						return false;
					if (!m1)
						return true;

					var expr1 = e1.Current;
					var expr2 = e2.Current;

					if (expr1 == null || expr2 == null || expr1.NodeType != expr2.NodeType || expr1.Type != expr2.Type)
						return false;

					switch (expr1.NodeType)
					{
						case ExpressionType.ArrayLength:
						case ExpressionType.Convert:
						case ExpressionType.ConvertChecked:
						case ExpressionType.Negate:
						case ExpressionType.NegateChecked:
						case ExpressionType.Not:
						case ExpressionType.Quote:
						case ExpressionType.TypeAs:
						case ExpressionType.UnaryPlus:
							if (((UnaryExpression)expr1).Method != ((UnaryExpression)expr2).Method)
								return false;
							break;
						case ExpressionType.Constant:
							if (!EqualsToX((ConstantExpression) expr1, (ConstantExpression) expr2, info))
								return false;
							break;
						case ExpressionType.MemberAccess:
							if (!EqualsToX((MemberExpression) expr1, (MemberExpression) expr2, info))
								return false;
							break;
						case ExpressionType.MemberInit:
							if (((MemberInitExpression)expr1).Bindings.Count != ((MemberInitExpression)expr2).Bindings.Count)
								return false;
							break;
						case ExpressionType.Call:
							if (!EqualsToX((MethodCallExpression) expr1, (MethodCallExpression) expr2, info, false))
								return false;
							break;
						case ExpressionType.Parameter: 
							if (((ParameterExpression) expr1).Name != ((ParameterExpression) expr2).Name)
								return false;
							break;
						case ExpressionType.TypeIs:
							if (((TypeBinaryExpression)expr1).TypeOperand != ((TypeBinaryExpression)expr2).TypeOperand)
								return false;
							break;
					}
				}
			}
		}

		class EqualsToInfo
		{
			public HashSet<Expression>                      Visited = new HashSet<Expression>();
			public Dictionary<Expression,QueryableAccessor> QueryableAccessorDic;
		}

		static bool EqualsTo(
			this Expression expr1,
			Expression      expr2,
			EqualsToInfo    info)
		{
			if (expr1 == expr2)
				return true;

			if (expr1 == null || expr2 == null || expr1.NodeType != expr2.NodeType || expr1.Type != expr2.Type)
				return false;

			switch (expr1.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.ArrayIndex:
#if FW4 || SILVERLIGHT
				case ExpressionType.Assign:
#endif
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
					{
//						var e1 = (BinaryExpression)expr1;
//						var e2 = (BinaryExpression)expr2;
						return
							((BinaryExpression)expr1).Method == ((BinaryExpression)expr2).Method &&
							((BinaryExpression)expr1).Conversion.EqualsTo(((BinaryExpression)expr2).Conversion, info) &&
							((BinaryExpression)expr1).Left.      EqualsTo(((BinaryExpression)expr2).Left,       info) &&
							((BinaryExpression)expr1).Right.     EqualsTo(((BinaryExpression)expr2).Right,      info);
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
					{
//						var e1 = (UnaryExpression)expr1;
//						var e2 = (UnaryExpression)expr2;
						return
							((UnaryExpression)expr1).Method == ((UnaryExpression)expr2).Method &&
							((UnaryExpression)expr1).Operand.EqualsTo(((UnaryExpression)expr2).Operand, info);
					}

				case ExpressionType.Conditional:
					{
//						var e1 = (ConditionalExpression)expr1;
//						var e2 = (ConditionalExpression)expr2;
						return
							((ConditionalExpression)expr1).Test.   EqualsTo(((ConditionalExpression)expr2).Test,    info) &&
							((ConditionalExpression)expr1).IfTrue. EqualsTo(((ConditionalExpression)expr2).IfTrue,  info) &&
							((ConditionalExpression)expr1).IfFalse.EqualsTo(((ConditionalExpression)expr2).IfFalse, info);
					}

				case ExpressionType.Call          : return EqualsToX((MethodCallExpression)expr1, (MethodCallExpression)expr2, info);
				case ExpressionType.Constant      : return EqualsToX((ConstantExpression)  expr1, (ConstantExpression)  expr2, info);
				case ExpressionType.Invoke        : return EqualsToX((InvocationExpression)expr1, (InvocationExpression)expr2, info);
				case ExpressionType.Lambda        : return EqualsToX((LambdaExpression)    expr1, (LambdaExpression)    expr2, info);
				case ExpressionType.ListInit      : return EqualsToX((ListInitExpression)  expr1, (ListInitExpression)  expr2, info);
				case ExpressionType.MemberAccess  : return EqualsToX((MemberExpression)    expr1, (MemberExpression)    expr2, info);
				case ExpressionType.MemberInit    : return EqualsToX((MemberInitExpression)expr1, (MemberInitExpression)expr2, info);
				case ExpressionType.New           : return EqualsToX((NewExpression)       expr1, (NewExpression)       expr2, info);
				case ExpressionType.NewArrayBounds:
				case ExpressionType.NewArrayInit  : return EqualsToX((NewArrayExpression)  expr1, (NewArrayExpression)  expr2, info);
				case ExpressionType.Default       : return true;
				case ExpressionType.Parameter     : return ((ParameterExpression) expr1).Name == ((ParameterExpression) expr2).Name;

				case ExpressionType.TypeIs:
					{
//						var e1 = (TypeBinaryExpression)expr1;
//						var e2 = (TypeBinaryExpression)expr2;
						return
							((TypeBinaryExpression)expr1).TypeOperand == ((TypeBinaryExpression)expr2).TypeOperand &&
							((TypeBinaryExpression)expr1).Expression.EqualsTo(((TypeBinaryExpression)expr2).Expression, info);
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					return EqualsToX((BlockExpression)expr1, (BlockExpression)expr2, info);

#endif
			}

			throw new InvalidOperationException();
		}

		static bool EqualsToX(BlockExpression expr1, BlockExpression expr2, EqualsToInfo info)
		{
			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			for (var i = 0; i < expr1.Variables.Count; i++)
				if (!expr1.Variables[i].EqualsTo(expr2.Variables[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewArrayExpression expr1, NewArrayExpression expr2, EqualsToInfo info)
		{
			if (expr1.Expressions.Count != expr2.Expressions.Count)
				return false;

			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewExpression expr1, NewExpression expr2, EqualsToInfo info)
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
				if (expr1.Members.Count != expr2.Members.Count)
					return false;

				for (var i = 0; i < expr1.Members.Count; i++)
					if (expr1.Members[i] != expr2.Members[i])
						return false;
			}

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(MemberInitExpression expr1, MemberInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Bindings.Count != expr2.Bindings.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			Func<MemberBinding,MemberBinding,bool> compareBindings = null;
			compareBindings = (b1, b2) =>
			{
				if (b1 == b2)
					return true;

				if (b1 == null || b2 == null || b1.BindingType != b2.BindingType || b1.Member != b2.Member)
					return false;

				switch (b1.BindingType)
				{
					case MemberBindingType.Assignment:
						return ((MemberAssignment)b1).Expression.EqualsTo(((MemberAssignment)b2).Expression, info);

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
								if (!ei1.Arguments[j].EqualsTo(ei2.Arguments[j], info))
									return false;
						}

						break;

					case MemberBindingType.MemberBinding:
						var mm1 = (MemberMemberBinding)b1;
						var mm2 = (MemberMemberBinding)b2;

						if (mm1.Bindings.Count != mm2.Bindings.Count)
							return false;

						for (var i = 0; i < mm1.Bindings.Count; i++)
							if (!compareBindings(mm1.Bindings[i], mm2.Bindings[i]))
								return false;

						break;
				}

				return true;
			};

			for (var i = 0; i < expr1.Bindings.Count; i++)
			{
				var b1 = expr1.Bindings[i];
				var b2 = expr2.Bindings[i];

				if (!compareBindings(b1, b2))
					return false;
			}

			return true;
		}

		static bool EqualsToX(MemberExpression expr1, MemberExpression expr2, EqualsToInfo info)
		{
			if (expr1.Member == expr2.Member)
			{
				if (expr1.Expression == expr2.Expression || expr1.Expression.Type == expr2.Expression.Type)
				{
					if (info.QueryableAccessorDic.Count > 0)
					{
						QueryableAccessor qa;

						if (info.QueryableAccessorDic.TryGetValue(expr1, out qa))
							return
								expr1.Expression.EqualsTo(expr2.Expression, info) &&
								qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, info);
					}
				}

				return expr1.Expression.EqualsTo(expr2.Expression, info);
			}

			return false;
		}

		static bool EqualsToX(ListInitExpression expr1, ListInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Initializers.Count != expr2.Initializers.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			for (var i = 0; i < expr1.Initializers.Count; i++)
			{
				var i1 = expr1.Initializers[i];
				var i2 = expr2.Initializers[i];

				if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
					return false;

				for (var j = 0; j < i1.Arguments.Count; j++)
					if (!i1.Arguments[j].EqualsTo(i2.Arguments[j], info))
						return false;
			}

			return true;
		}

		static bool EqualsToX(LambdaExpression expr1, LambdaExpression expr2, EqualsToInfo info)
		{
			if (expr1.Parameters.Count != expr2.Parameters.Count || !expr1.Body.EqualsTo(expr2.Body, info))
				return false;

			for (var i = 0; i < expr1.Parameters.Count; i++)
				if (!expr1.Parameters[i].EqualsTo(expr2.Parameters[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(InvocationExpression expr1, InvocationExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || !expr1.Expression.EqualsTo(expr2.Expression, info))
				return false;

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(ConstantExpression expr1, ConstantExpression expr2, EqualsToInfo info)
		{
			if (expr1.Value == null && expr2.Value == null)
				return true;

			if (IsConstantable(expr1.Type))
				return Equals(expr1.Value, expr2.Value);

			if (expr1.Value == null || expr2.Value == null)
				return false;

			if (expr1.Value is IQueryable)
			{
				var eq1 = ((IQueryable)expr1.Value).Expression;
				var eq2 = ((IQueryable)expr2.Value).Expression;

				if (!info.Visited.Contains(eq1))
				{
					info.Visited.Add(eq1);
					return eq1.EqualsTo(eq2, info);
				}
			}

			return true;
		}

		static bool EqualsToX(MethodCallExpression expr1, MethodCallExpression expr2, EqualsToInfo info, bool compareArguments = true)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || expr1.Method != expr2.Method)
				return false;

			if (info.QueryableAccessorDic.Count > 0)
			{
				QueryableAccessor qa;

				if (info.QueryableAccessorDic.TryGetValue(expr1, out qa))
					return qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, info);
			}

			if (!expr1.Object.EqualsTo(expr2.Object, info))
				return false;

			if (compareArguments)
				for (var i = 0; i < expr1.Arguments.Count; i++)
					if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
						return false;

			return true;
		}

		#endregion

		#region Path

		static Expression ConvertTo(Expression expr, Type type)
		{
			return Expression.Convert(expr, type);
		}

		static void Path<T>(IEnumerable<T> source, HashSet<Expression> visited, Expression path, MethodInfo property, Action<T, Expression> func)
			where T : class
		{
			var prop = Expression.Property(path, property);
			var i    = 0;
			foreach (var item in source)
				func(item, Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, new Expression[] { Expression.Constant(i++) }));
		}

		static void Path<T>(IEnumerable<T> source, HashSet<Expression> visited, Expression path, MethodInfo property, Action<Expression, Expression> func)
			where T : Expression
		{
			var prop = Expression.Property(path, property);
			var i    = 0;
			foreach (var item in source)
				Path(item, visited, Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, new Expression[] { Expression.Constant(i++) }), func);
		}

		static void Path(Expression expr, HashSet<Expression> visited, Expression path, MethodInfo property, Action<Expression, Expression> func)
		{
			Path(expr, visited, Expression.Property(path, property), func);
		}

		public static void Path(this Expression expr, Expression path, Action<Expression, Expression> func)
		{
			Path(expr, new HashSet<Expression>(), path, func);
		}

		static void Path(
			this Expression expr,
			HashSet<Expression> visited,
			Expression path,
			Action<Expression, Expression> func)
		{
			if (expr == null)
				return;

			switch (expr.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.ArrayIndex:
#if FW4 || SILVERLIGHT
				case ExpressionType.Assign:
#endif
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
					{
						path = ConvertTo(path, typeof(BinaryExpression));
						var e = (BinaryExpression)expr;

						Path(e.Conversion, visited, path, ReflectionHelper.Binary.Conversion, func);
						Path(e.Left, visited, path, ReflectionHelper.Binary.Left, func);
						Path(e.Right, visited, path, ReflectionHelper.Binary.Right, func);

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
					Path(
						((UnaryExpression)expr).Operand,
						visited,
						path = ConvertTo(path, typeof(UnaryExpression)),
						ReflectionHelper.Unary.Operand,
						func);
					break;

				case ExpressionType.Call:
					{
						path = ConvertTo(path, typeof(MethodCallExpression));
						var e = (MethodCallExpression)expr;

						Path(e.Object, visited, path, ReflectionHelper.MethodCall.Object, func);
						Path(e.Arguments, visited, path, ReflectionHelper.MethodCall.Arguments, func);

						break;
					}

				case ExpressionType.Conditional:
					{
						path = ConvertTo(path, typeof(ConditionalExpression));
						var e = (ConditionalExpression)expr;

						Path(e.Test, visited, path, ReflectionHelper.Conditional.Test, func);
						Path(e.IfTrue, visited, path, ReflectionHelper.Conditional.IfTrue, func);
						Path(e.IfFalse, visited, path, ReflectionHelper.Conditional.IfFalse, func);

						break;
					}

				case ExpressionType.Invoke:
					{
						path = ConvertTo(path, typeof(InvocationExpression));
						var e = (InvocationExpression)expr;

						Path(e.Expression, visited, path, ReflectionHelper.Invocation.Expression, func);
						Path(e.Arguments, visited, path, ReflectionHelper.Invocation.Arguments, func);

						break;
					}

				case ExpressionType.Lambda:
					{
						path = ConvertTo(path, typeof(LambdaExpression));
						var e = (LambdaExpression)expr;

						Path(e.Body, visited, path, ReflectionHelper.LambdaExpr.Body, func);
						Path(e.Parameters, visited, path, ReflectionHelper.LambdaExpr.Parameters, func);

						break;
					}

				case ExpressionType.ListInit:
					{
						path = ConvertTo(path, typeof(ListInitExpression));
						var e = (ListInitExpression)expr;

						Path(e.NewExpression, visited, path, ReflectionHelper.ListInit.NewExpression, func);
						Path(e.Initializers, visited, path, ReflectionHelper.ListInit.Initializers,
							(ex, p) => Path(ex.Arguments, visited, p, ReflectionHelper.ElementInit.Arguments, func));

						break;
					}

				case ExpressionType.MemberAccess:
					Path(
						((MemberExpression)expr).Expression,
						visited,
						path = ConvertTo(path, typeof(MemberExpression)),
						ReflectionHelper.Member.Expression,
						func);
					break;

				case ExpressionType.MemberInit:
					{
						Action<MemberBinding, Expression> modify = null; modify = (b, pinf) =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									Path(
										((MemberAssignment)b).Expression,
										visited,
										ConvertTo(pinf, typeof(MemberAssignment)),
										ReflectionHelper.MemberAssignmentBind.Expression,
										func);
									break;

								case MemberBindingType.ListBinding:
									Path(
										((MemberListBinding)b).Initializers,
										visited,
										ConvertTo(pinf, typeof(MemberListBinding)),
										ReflectionHelper.MemberListBind.Initializers,
										(p, psi) => Path(p.Arguments, visited, psi, ReflectionHelper.ElementInit.Arguments, func));
									break;

								case MemberBindingType.MemberBinding:
									Path(
										((MemberMemberBinding)b).Bindings,
										visited,
										ConvertTo(pinf, typeof(MemberMemberBinding)),
										ReflectionHelper.MemberMemberBind.Bindings,
										modify);
									break;
							}
						};

						path = ConvertTo(path, typeof(MemberInitExpression));
						var e = (MemberInitExpression)expr;

						Path(e.NewExpression, visited, path, ReflectionHelper.MemberInit.NewExpression, func);
						Path(e.Bindings, visited, path, ReflectionHelper.MemberInit.Bindings, modify);

						break;
					}

				case ExpressionType.New:
					Path(
						((NewExpression)expr).Arguments,
						visited,
						path = ConvertTo(path, typeof(NewExpression)),
						ReflectionHelper.New.Arguments,
						func);
					break;

				case ExpressionType.NewArrayBounds:
					Path(
						((NewArrayExpression)expr).Expressions,
						visited,
						path = ConvertTo(path, typeof(NewArrayExpression)),
						ReflectionHelper.NewArray.Expressions,
						func);
					break;

				case ExpressionType.NewArrayInit:
					Path(
						((NewArrayExpression)expr).Expressions,
						visited,
						path = ConvertTo(path, typeof(NewArrayExpression)),
						ReflectionHelper.NewArray.Expressions,
						func);
					break;

				case ExpressionType.TypeIs:
					Path(
						((TypeBinaryExpression)expr).Expression,
						visited,
						path = ConvertTo(path, typeof(TypeBinaryExpression)),
						ReflectionHelper.TypeBinary.Expression,
						func);
					break;

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						path = ConvertTo(path, typeof(BlockExpression));
						var e = (BlockExpression)expr;

						Path(e.Expressions, visited, path, ReflectionHelper.Block.Expressions, func);
						Path(e.Variables, visited, path, ReflectionHelper.Block.Variables, func); // ?

						break;
					}

#endif

				case ExpressionType.Constant:
					{
						path = ConvertTo(path, typeof(ConstantExpression));
						var e = (ConstantExpression)expr;
						var iq = e.Value as IQueryable;

						if (iq != null && !visited.Contains(iq.Expression))
						{
							visited.Add(iq.Expression);

							Expression p = Expression.Property(path, ReflectionHelper.Constant.Value);
							p = ConvertTo(p, typeof(IQueryable));
							Path(iq.Expression, visited, p, ReflectionHelper.QueryableInt.Expression, func);
						}

						break;
					}

				case ExpressionType.Parameter: path = ConvertTo(path, typeof(ParameterExpression)); break;
			}

			func(expr, path);
		}

		#endregion

		#region Helpers

		public static Expression Unwrap(this Expression ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote          : return ((UnaryExpression)ex).Operand.Unwrap();
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
					{
						var ue = (UnaryExpression)ex;

						if (!ue.Operand.Type.IsEnumEx())
							return ue.Operand.Unwrap();

						break;
					}
			}

			return ex;
		}

		public static Dictionary<Expression,Expression> GetExpressionAccessors(this Expression expression, Expression path)
		{
			var accessors = new Dictionary<Expression,Expression>();

			expression.Path(path, (e,p) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Call           :
					case ExpressionType.MemberAccess   :
					case ExpressionType.New            :
						if (!accessors.ContainsKey(e))
							accessors.Add(e, p);
						break;

					case ExpressionType.Constant       :
						if (!accessors.ContainsKey(e))
							accessors.Add(e, Expression.Property(p, ReflectionHelper.Constant.Value));
						break;

					case ExpressionType.ConvertChecked :
					case ExpressionType.Convert        :
						if (!accessors.ContainsKey(e))
						{
							var ue = (UnaryExpression)e;

							switch (ue.Operand.NodeType)
							{
								case ExpressionType.Call           :
								case ExpressionType.MemberAccess   :
								case ExpressionType.New            :
								case ExpressionType.Constant       :

									accessors.Add(e, p);
									break;
							}
						}

						break;
				}
			});

			return accessors;
		}

		public static Expression GetRootObject(this Expression expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Call         :
					{
						var e = (MethodCallExpression)expr;

						if (e.Object != null)
							return GetRootObject(e.Object);

						if (e.Arguments != null && e.Arguments.Count > 0 && e.IsQueryable())
							return GetRootObject(e.Arguments[0]);

						break;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						if (e.Expression != null)
							return GetRootObject(e.Expression.Unwrap());

						break;
					}
			}

			return expr;
		}

		public static List<Expression> GetMembers(this Expression expr)
		{
			if (expr == null)
				return new List<Expression>();

			List<Expression> list;

			switch (expr.NodeType)
			{
				case ExpressionType.Call         :
					{
						var e = (MethodCallExpression)expr;

						if (e.Object != null)
							list = GetMembers(e.Object);
						else if (e.Arguments != null && e.Arguments.Count > 0 && e.IsQueryable())
							list = GetMembers(e.Arguments[0]);
						else
							list = new List<Expression>();

						break;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						list = e.Expression != null ? GetMembers(e.Expression.Unwrap()) : new List<Expression>();

						break;
					}

				default                          :
					list = new List<Expression>();
					break;
			}

			list.Add(expr);

			return list;
		}

		public static bool IsQueryable(this MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(Queryable) || (enumerable && type == typeof(Enumerable)) || type == typeof(LinqExtensions);
		}

		public static bool IsQueryable(this MethodCallExpression method, string name)
		{
			return method.Method.Name == name && method.IsQueryable();
		}

		public static bool IsQueryable(this MethodCallExpression method, params string[] names)
		{
			if (method.IsQueryable())
				foreach (var name in names)
					if (method.Method.Name == name)
						return true;

			return false;
		}

		static Expression FindLevel(Expression expression, int level, ref int current)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;
						var expr = call.Object;

						if (expr == null && call.IsQueryable() && call.Arguments.Count > 0)
							expr = call.Arguments[0];

						if (expr != null)
						{
							var ex = FindLevel(expr, level, ref current);

							if (level == current)
								return ex;

							current++;
						}

						break;
					}

				case ExpressionType.MemberAccess:
					{
						var e = ((MemberExpression)expression);

						if (e.Expression != null)
						{
							var expr = FindLevel(e.Expression.Unwrap(), level, ref current);

							if (level == current)
								return expr;

							current++;
						}

						break;
					}
			}

			return expression;
		}

		public static Expression GetLevelExpression(this Expression expression, int level)
		{
			var current = 0;
			var expr    = FindLevel(expression, level, ref current);

			if (expr == null || current != level)
				throw new InvalidOperationException();

			return expr;
		}

		public static int GetLevel(this Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var e = ((MemberExpression)expression);

						if (e.Expression != null)
							return GetLevel(e.Expression.Unwrap()) + 1;

						break;
					}
			}

			return 0;
		}

		#endregion
	}
}
