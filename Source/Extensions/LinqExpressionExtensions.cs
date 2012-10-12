using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Extensions
{
	using Linq;
	using Linq.Builder;

	public static class LinqExpressionExtensions
	{
		#region IsConstant

		public static bool IsConstantable(this Type type)
		{
			if (type.IsEnum)
				return true;

			switch (Type.GetTypeCode(type))
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
				return type.GetGenericArguments()[0].IsConstantable();

			return false;
		}

		#endregion

		#region EqualsTo

		internal static bool EqualsTo(this Expression expr1, Expression expr2, Dictionary<Expression,QueryableAccessor> queryableAccessorDic)
		{
			return EqualsTo(expr1, expr2, new HashSet<Expression>(), queryableAccessorDic);
		}

#if NEMERLE
		public
#endif
		static bool EqualsTo(
			this Expression     expr1,
			Expression          expr2,
			HashSet<Expression> visited,
			Dictionary<Expression,QueryableAccessor> queryableAccessorDic)
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
						var e1 = (BinaryExpression)expr1;
						var e2 = (BinaryExpression)expr2;
						return
							e1.Method == e2.Method &&
							e1.Conversion.EqualsTo(e2.Conversion, visited, queryableAccessorDic) &&
							e1.Left.      EqualsTo(e2.Left,       visited, queryableAccessorDic) &&
							e1.Right.     EqualsTo(e2.Right,      visited, queryableAccessorDic);
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
						var e1 = (UnaryExpression)expr1;
						var e2 = (UnaryExpression)expr2;
						return e1.Method == e2.Method && e1.Operand.EqualsTo(e2.Operand, visited, queryableAccessorDic);
					}

				case ExpressionType.Call:
					{
						var e1 = (MethodCallExpression)expr1;
						var e2 = (MethodCallExpression)expr2;

						if (e1.Arguments.Count != e2.Arguments.Count || e1.Method != e2.Method)
							return false;

						if (queryableAccessorDic.Count > 0)
						{
							QueryableAccessor qa;

							if (queryableAccessorDic.TryGetValue(expr1, out qa))
								return qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, visited, queryableAccessorDic);
						}

						if (!e1.Object.EqualsTo(e2.Object, visited, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Arguments.Count; i++)
							if (!e1.Arguments[i].EqualsTo(e2.Arguments[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.Conditional:
					{
						var e1 = (ConditionalExpression)expr1;
						var e2 = (ConditionalExpression)expr2;
						return
							e1.Test.   EqualsTo(e2.Test,    visited, queryableAccessorDic) &&
							e1.IfTrue. EqualsTo(e2.IfTrue,  visited, queryableAccessorDic) &&
							e1.IfFalse.EqualsTo(e2.IfFalse, visited, queryableAccessorDic);
					}

				case ExpressionType.Constant:
					{
						var e1 = (ConstantExpression)expr1;
						var e2 = (ConstantExpression)expr2;

						if (e1.Value == null && e2.Value == null)
							return true;

						if (IsConstantable(e1.Type))
							return Equals(e1.Value, e2.Value);

						if (e1.Value == null || e2.Value == null)
							return false;

						if (e1.Value is IQueryable)
						{
							var eq1 = ((IQueryable)e1.Value).Expression;
							var eq2 = ((IQueryable)e2.Value).Expression;

							if (!visited.Contains(eq1))
							{
								visited.Add(eq1);
								return eq1.EqualsTo(eq2, visited, queryableAccessorDic);
							}
						}

						return true;
					}

				case ExpressionType.Invoke:
					{
						var e1 = (InvocationExpression)expr1;
						var e2 = (InvocationExpression)expr2;

						if (e1.Arguments.Count != e2.Arguments.Count || !e1.Expression.EqualsTo(e2.Expression, visited, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Arguments.Count; i++)
							if (!e1.Arguments[i].EqualsTo(e2.Arguments[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.Lambda:
					{
						var e1 = (LambdaExpression)expr1;
						var e2 = (LambdaExpression)expr2;

						if (e1.Parameters.Count != e2.Parameters.Count || !e1.Body.EqualsTo(e2.Body, visited, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Parameters.Count; i++)
							if (!e1.Parameters[i].EqualsTo(e2.Parameters[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.ListInit:
					{
						var e1 = (ListInitExpression)expr1;
						var e2 = (ListInitExpression)expr2;

						if (e1.Initializers.Count != e2.Initializers.Count || !e1.NewExpression.EqualsTo(e2.NewExpression, visited, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Initializers.Count; i++)
						{
							var i1 = e1.Initializers[i];
							var i2 = e2.Initializers[i];

							if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
								return false;

							for (var j = 0; j < i1.Arguments.Count; j++)
								if (!i1.Arguments[j].EqualsTo(i2.Arguments[j], visited, queryableAccessorDic))
									return false;
						}

						return true;
					}

				case ExpressionType.MemberAccess:
					{
						var e1 = (MemberExpression)expr1;
						var e2 = (MemberExpression)expr2;

						if (e1.Member == e2.Member)
						{
							if (e1.Expression == e2.Expression || e1.Expression.Type == e2.Expression.Type)
							{
								if (queryableAccessorDic.Count > 0)
								{
									QueryableAccessor qa;

									if (queryableAccessorDic.TryGetValue(expr1, out qa))
										return
											e1.Expression.EqualsTo(e2.Expression, visited, queryableAccessorDic) &&
											qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, visited, queryableAccessorDic);
								}
							}

							return e1.Expression.EqualsTo(e2.Expression, visited, queryableAccessorDic);
						}

						return false;
					}

				case ExpressionType.MemberInit:
					{
						var e1 = (MemberInitExpression)expr1;
						var e2 = (MemberInitExpression)expr2;

						if (e1.Bindings.Count != e2.Bindings.Count || !e1.NewExpression.EqualsTo(e2.NewExpression, visited, queryableAccessorDic))
							return false;

						Func<MemberBinding,MemberBinding,bool> compareBindings = null; compareBindings = (b1,b2) =>
						{
							if (b1 == b2)
								return true;

							if (b1 == null || b2 == null || b1.BindingType != b2.BindingType || b1.Member != b2.Member)
								return false;

							switch (b1.BindingType)
							{
								case MemberBindingType.Assignment:
									return ((MemberAssignment)b1).Expression.EqualsTo(((MemberAssignment)b2).Expression, visited, queryableAccessorDic);

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
											if (!ei1.Arguments[j].EqualsTo(ei2.Arguments[j], visited, queryableAccessorDic))
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

						for (var i = 0; i < e1.Bindings.Count; i++)
						{
							var b1 = e1.Bindings[i];
							var b2 = e2.Bindings[i];

							if (!compareBindings(b1, b2))
								return false;
						}

						return true;
					}

				case ExpressionType.New:
					{
						var e1 = (NewExpression)expr1;
						var e2 = (NewExpression)expr2;

						if (e1.Arguments.Count != e2.Arguments.Count)
							return false;

						if (e1.Members == null && e2.Members != null)
							return false;

						if (e1.Members != null && e2.Members == null)
							return false;

						if (e1.Constructor != e2.Constructor)
							return false;

						if (e1.Members != null)
						{
							if (e1.Members.Count != e2.Members.Count)
								return false;

							for (var i = 0; i < e1.Members.Count; i++)
								if (e1.Members[i] != e2.Members[i])
									return false;
						}

						for (var i = 0; i < e1.Arguments.Count; i++)
							if (!e1.Arguments[i].EqualsTo(e2.Arguments[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.NewArrayBounds:
				case ExpressionType.NewArrayInit:
					{
						var e1 = (NewArrayExpression)expr1;
						var e2 = (NewArrayExpression)expr2;

						if (e1.Expressions.Count != e2.Expressions.Count)
							return false;

						for (var i = 0; i < e1.Expressions.Count; i++)
							if (!e1.Expressions[i].EqualsTo(e2.Expressions[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.Parameter:
					{
						var e1 = (ParameterExpression)expr1;
						var e2 = (ParameterExpression)expr2;
						return e1.Name == e2.Name;
					}

				case ExpressionType.TypeIs:
					{
						var e1 = (TypeBinaryExpression)expr1;
						var e2 = (TypeBinaryExpression)expr2;
						return e1.TypeOperand == e2.TypeOperand && e1.Expression.EqualsTo(e2.Expression, visited, queryableAccessorDic);
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var e1 = (BlockExpression)expr1;
						var e2 = (BlockExpression)expr2;

						for (var i = 0; i < e1.Expressions.Count; i++)
							if (!e1.Expressions[i].EqualsTo(e2.Expressions[i], visited, queryableAccessorDic))
								return false;

						for (var i = 0; i < e1.Variables.Count; i++)
							if (!e1.Variables[i].EqualsTo(e2.Variables[i], visited, queryableAccessorDic))
								return false;

						return true;
					}

#endif
			}

			throw new InvalidOperationException();
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

#if NEMERLE
		public
#endif
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

		#region Visit

		static void Visit<T>(IEnumerable<T> source, Action<T> func)
		{
			foreach (var item in source)
				func(item);
		}

		static void Visit<T>(IEnumerable<T> source, Action<Expression> func)
			where T : Expression
		{
			foreach (var item in source)
				Visit(item, func);
		}

		public static void Visit(this Expression expr, Action<Expression> func)
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
						var e = (BinaryExpression)expr;

						Visit(e.Conversion, func);
						Visit(e.Left,       func);
						Visit(e.Right,      func);

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
					Visit(((UnaryExpression)expr).Operand, func);
					break;

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						Visit(e.Object,    func);
						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						Visit(e.Test,    func);
						Visit(e.IfTrue,  func);
						Visit(e.IfFalse, func);

						break;
					}

				case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						Visit(e.Expression, func);
						Visit(e.Arguments,  func);

						break;
					}

				case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						Visit(e.Body,       func);
						Visit(e.Parameters, func);

						break;
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Initializers,  ex => Visit(ex.Arguments, func));

						break;
					}

				case ExpressionType.MemberAccess: Visit(((MemberExpression)expr).Expression, func); break;

				case ExpressionType.MemberInit:
					{
						Action<MemberBinding> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : Visit(((MemberAssignment)b). Expression,   func);                          break;
								case MemberBindingType.ListBinding   : Visit(((MemberListBinding)b).Initializers, p => Visit(p.Arguments, func)); break;
								case MemberBindingType.MemberBinding : Visit(((MemberMemberBinding)b).Bindings,   modify);                        break;
							}
						};

						var e = (MemberInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Bindings,      modify);

						break;
					}

				case ExpressionType.New            : Visit(((NewExpression)       expr).Arguments,   func); break;
				case ExpressionType.NewArrayBounds : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.NewArrayInit   : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.TypeIs         : Visit(((TypeBinaryExpression)expr).Expression,  func); break;

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

#endif

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					Visit(((ChangeTypeExpression)expr).Expression,  func); break;
			}

			func(expr);
		}

		static void Visit<T>(IEnumerable<T> source, Func<T,bool> func)
		{
			foreach (var item in source)
				func(item);
		}

		static void Visit<T>(IEnumerable<T> source, Func<Expression,bool> func)
			where T : Expression
		{
			foreach (var item in source)
				Visit(item, func);
		}

		public static void Visit(this Expression expr, Func<Expression,bool> func)
		{
			if (expr == null || !func(expr))
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
						var e = (BinaryExpression)expr;

						Visit(e.Conversion, func);
						Visit(e.Left,       func);
						Visit(e.Right,      func);

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
					Visit(((UnaryExpression)expr).Operand, func);
					break;

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						Visit(e.Object,    func);
						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						Visit(e.Test,    func);
						Visit(e.IfTrue,  func);
						Visit(e.IfFalse, func);

						break;
					}

				case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						Visit(e.Expression, func);
						Visit(e.Arguments,  func);

						break;
					}

				case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						Visit(e.Body,       func);
						Visit(e.Parameters, func);

						break;
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Initializers,  ex => Visit(ex.Arguments, func));

						break;
					}

				case ExpressionType.MemberAccess: Visit(((MemberExpression)expr).Expression, func); break;

				case ExpressionType.MemberInit:
					{
						Func<MemberBinding,bool> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : Visit(((MemberAssignment)b). Expression,   func);                          break;
								case MemberBindingType.ListBinding   : Visit(((MemberListBinding)b).Initializers, p => Visit(p.Arguments, func)); break;
								case MemberBindingType.MemberBinding : Visit(((MemberMemberBinding)b).Bindings,   modify);                        break;
							}

							return true;
						};

						var e = (MemberInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Bindings,      modify);

						break;
					}

				case ExpressionType.New            : Visit(((NewExpression)       expr).Arguments,   func); break;
				case ExpressionType.NewArrayBounds : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.NewArrayInit   : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.TypeIs         : Visit(((TypeBinaryExpression)expr).Expression,  func); break;

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

#endif

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					Visit(((ChangeTypeExpression)expr).Expression,  func);
					break;
			}
		}

		#endregion

		#region Find

		static Expression Find<T>(IEnumerable<T> source, Func<T,Expression> func)
		{
			foreach (var item in source)
			{
				var ex = func(item);
				if (ex != null)
					return ex;
			}

			return null;
		}

		static Expression Find<T>(IEnumerable<T> source, Func<Expression,bool> func)
			where T : Expression
		{
			foreach (var item in source)
			{
				var f = Find(item, func);
				if (f != null)
					return f;
			}

			return null;
		}

		public static Expression Find(this Expression expr, Func<Expression,bool> func)
		{
			if (expr == null || func(expr))
				return expr;

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
						var e = (BinaryExpression)expr;

						return
							Find(e.Conversion, func) ??
							Find(e.Left,       func) ??
							Find(e.Right,      func);
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
					return Find(((UnaryExpression)expr).Operand, func);

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						return
							Find(e.Object,    func) ??
							Find(e.Arguments, func);
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						return
							Find(e.Test,    func) ??
							Find(e.IfTrue,  func) ??
							Find(e.IfFalse, func);
					}

				case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						return
							Find(e.Expression, func) ??
							Find(e.Arguments,  func);
					}

				case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;

						return
							Find(e.Body,       func) ??
							Find(e.Parameters, func);
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						return
							Find(e.NewExpression, func) ??
							Find(e.Initializers,  ex => Find(ex.Arguments, func));
					}

				case ExpressionType.MemberAccess:
					return Find(((MemberExpression)expr).Expression, func);

				case ExpressionType.MemberInit:
					{
						Func<MemberBinding,Expression> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : return Find(((MemberAssignment)b).   Expression,   func);
								case MemberBindingType.ListBinding   : return Find(((MemberListBinding)b).  Initializers, p => Find(p.Arguments, func));
								case MemberBindingType.MemberBinding : return Find(((MemberMemberBinding)b).Bindings,     modify);
							}

							return null;
						};

						var e = (MemberInitExpression)expr;

						return
							Find(e.NewExpression, func) ??
							Find(e.Bindings,      modify);
					}

				case ExpressionType.New            : return Find(((NewExpression)       expr).Arguments,   func);
				case ExpressionType.NewArrayBounds : return Find(((NewArrayExpression)  expr).Expressions, func);
				case ExpressionType.NewArrayInit   : return Find(((NewArrayExpression)  expr).Expressions, func);
				case ExpressionType.TypeIs         : return Find(((TypeBinaryExpression)expr).Expression,  func);

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						return
							Find(e.Expressions, func) ??
							Find(e.Variables,   func);
					}

#endif

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					return Find(((ChangeTypeExpression)expr).Expression, func);
			}

			return null;
		}

		#endregion

		#region Transform

		static IEnumerable<T> Transform<T>(ICollection<T> source, Func<T,T> func)
			where T : class
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = func(item);
				list.Add(e);
				modified = modified || e != item;
			}

			return modified ? list : source;
		}

		static IEnumerable<T> Transform<T>(ICollection<T> source, Func<Expression,Expression> func)
			where T : Expression
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = Transform(item, func);
				list.Add((T)e);
				modified = modified || e != item;
			}

			return modified? list: source;
		}

		public static Expression Transform(this Expression expr, Func<Expression,Expression> func)
		{
			if (expr == null)
				return null;

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
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (BinaryExpression)expr;
						var c = Transform(e.Conversion, func);
						var l = Transform(e.Left,       func);
						var r = Transform(e.Right,      func);

						return c != e.Conversion || l != e.Left || r != e.Right ?
							Expression.MakeBinary(expr.NodeType, l, r, e.IsLiftedToNull, e.Method, (LambdaExpression)c):
							expr;
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
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (UnaryExpression)expr;
						var o = Transform(e.Operand, func);

						return o != e.Operand ?
							Expression.MakeUnary(expr.NodeType, o, e.Type, e.Method) :
							expr;
					}

				case ExpressionType.Call:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (MethodCallExpression)expr;
						var o = Transform(e.Object,    func);
						var a = Transform(e.Arguments, func);

						return o != e.Object || a != e.Arguments ? 
							Expression.Call(o, e.Method, a) : 
							expr;
					}

				case ExpressionType.Conditional:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (ConditionalExpression)expr;
						var s = Transform(e.Test,    func);
						var t = Transform(e.IfTrue,  func);
						var f = Transform(e.IfFalse, func);

						return s != e.Test || t != e.IfTrue || f != e.IfFalse ?
							Expression.Condition(s, t, f) :
							expr;
					}

				case ExpressionType.Invoke:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = (InvocationExpression)expr;
						var ex = Transform(e.Expression, func);
						var a  = Transform(e.Arguments,  func);

						return ex != e.Expression || a != e.Arguments ? Expression.Invoke(ex, a) : expr;
					}

				case ExpressionType.Lambda:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (LambdaExpression)expr;
						var b = Transform(e.Body,       func);
						var p = Transform(e.Parameters, func);

						return b != e.Body || p != e.Parameters ? Expression.Lambda(ex.Type, b, p.ToArray()) : expr;
					}

				case ExpressionType.ListInit:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (ListInitExpression)expr;
						var n = Transform(e.NewExpression, func);
						var i = Transform(e.Initializers,  p =>
						{
							var args = Transform(p.Arguments, func);
							return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
						});

						return n != e.NewExpression || i != e.Initializers ?
							Expression.ListInit((NewExpression)n, i) :
							expr;
					}

				case ExpressionType.MemberAccess:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = (MemberExpression)expr;
						var ex = Transform(e.Expression, func);

						return ex != e.Expression ? Expression.MakeMemberAccess(ex, e.Member) : expr;
					}

				case ExpressionType.MemberInit:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						Func<MemberBinding,MemberBinding> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									{
										var ma = (MemberAssignment)b;
										return ma.Update(Transform(ma.Expression, func));
									}

								case MemberBindingType.ListBinding:
									{
										var ml = (MemberListBinding)b;
										var i  = Transform(ml.Initializers, p =>
										{
											var args = Transform(p.Arguments, func);
											return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
										});

										if (i != ml.Initializers)
											ml = Expression.ListBind(ml.Member, i);

										return ml;
									}

								case MemberBindingType.MemberBinding:
									{
										var mm = (MemberMemberBinding)b;
										var bs = Transform(mm.Bindings, modify);

										if (bs != mm.Bindings)
											mm = Expression.MemberBind(mm.Member);

										return mm;
									}
							}

							return b;
						};

						var e  = (MemberInitExpression)expr;
						return e.Update(
							(NewExpression)Transform(e.NewExpression, func),
							Transform(e.Bindings, modify));
					}

				case ExpressionType.New:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = (NewExpression)expr;
						return e.Update(Transform(e.Arguments, func));
					}

				case ExpressionType.NewArrayBounds:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = (NewArrayExpression)expr;
						var ex = Transform(e.Expressions, func);

						return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : expr;
					}

				case ExpressionType.NewArrayInit:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = (NewArrayExpression)expr;
						var ex = Transform(e.Expressions, func);

						return ex != e.Expressions ?
							Expression.NewArrayInit(e.Type.GetElementType(), ex) :
							expr;
					}

				case ExpressionType.TypeIs:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e = (TypeBinaryExpression)expr;
						return e.Update(Transform(e.Expression, func));
					}

				case ExpressionType.Block:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e = (BlockExpression)expr;
						return e.Update(
							Transform(e.Variables,   func),
							Transform(e.Expressions, func));
					}

				case ExpressionType.Extension:
				case ExpressionType.Constant :
				case ExpressionType.Parameter: return func(expr);

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = (ChangeTypeExpression)expr;
						var ex = Transform(e.Expression, func);

						if (ex == e.Expression)
							return expr;

						if (ex.Type == e.Type)
							return ex;

						return new ChangeTypeExpression(ex, e.Type);
					}

				case ExpressionType.Switch :
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e = (SwitchExpression)expr;
						return e.Update(
							Transform(e.SwitchValue, func),
							Transform(e.Cases,       c => c.Update(Transform(c.TestValues, func), Transform(c.Body, func))),
							Transform(e.DefaultBody, func));
					}
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region Transform2

		static IEnumerable<T> Transform2<T>(ICollection<T> source, Func<T,T> func)
			where T : class
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = func(item);
				list.Add(e);
				modified = modified || e != item;
			}

			return modified ? list : source;
		}

		static IEnumerable<T> Transform2<T>(ICollection<T> source, Func<Expression,TransformInfo> func)
			where T : Expression
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = Transform(item, func);
				list.Add((T)e);
				modified = modified || e != item;
			}

			return modified ? list : source;
		}

		public static Expression Transform(this Expression expr, Func<Expression,TransformInfo> func)
		{
			if (expr == null)
				return null;

			TransformInfo ti;

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
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (BinaryExpression)expr;
						var c = Transform(e.Conversion, func);
						var l = Transform(e.Left,       func);
						var r = Transform(e.Right,      func);

						return c != e.Conversion || l != e.Left || r != e.Right ?
							Expression.MakeBinary(expr.NodeType, l, r, e.IsLiftedToNull, e.Method, (LambdaExpression)c):
							expr;
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
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (UnaryExpression)expr;
						var o = Transform(e.Operand, func);

						return o != e.Operand ?
							Expression.MakeUnary(expr.NodeType, o, e.Type, e.Method) :
							expr;
					}

				case ExpressionType.Call:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (MethodCallExpression)expr;
						var o = Transform(e.Object,    func);
						var a = Transform2(e.Arguments, func);

						return o != e.Object || a != e.Arguments ? 
							Expression.Call(o, e.Method, a) : 
							expr;
					}

				case ExpressionType.Conditional:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (ConditionalExpression)expr;
						var s = Transform(e.Test,    func);
						var t = Transform(e.IfTrue,  func);
						var f = Transform(e.IfFalse, func);

						return s != e.Test || t != e.IfTrue || f != e.IfFalse ?
							Expression.Condition(s, t, f) :
							expr;
					}

				case ExpressionType.Invoke:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (InvocationExpression)expr;
						var ex = Transform(e.Expression, func);
						var a  = Transform2(e.Arguments,  func);

						return ex != e.Expression || a != e.Arguments ? Expression.Invoke(ex, a) : expr;
					}

				case ExpressionType.Lambda:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (LambdaExpression)expr;
						var b = Transform(e.Body,       func);
						var p = Transform2(e.Parameters, func);

						return b != e.Body || p != e.Parameters ? Expression.Lambda(ti.Expression.Type, b, p.ToArray()) : expr;
					}

				case ExpressionType.ListInit:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (ListInitExpression)expr;
						var n = Transform(e.NewExpression, func);
						var i = Transform2(e.Initializers,  p =>
						{
							var args = Transform2(p.Arguments, func);
							return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
						});

						return n != e.NewExpression || i != e.Initializers ?
							Expression.ListInit((NewExpression)n, i) :
							expr;
					}

				case ExpressionType.MemberAccess:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (MemberExpression)expr;
						var ex = Transform(e.Expression, func);

						return ex != e.Expression ? Expression.MakeMemberAccess(ex, e.Member) : expr;
					}

				case ExpressionType.MemberInit:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						Func<MemberBinding,MemberBinding> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									{
										var ma = (MemberAssignment)b;
										var ex = Transform(ma.Expression, func);

										if (ex != ma.Expression)
											ma = Expression.Bind(ma.Member, ex);

										return ma;
									}

								case MemberBindingType.ListBinding:
									{
										var ml = (MemberListBinding)b;
										var i  = Transform(ml.Initializers, p =>
										{
											var args = Transform2(p.Arguments, func);
											return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
										});

										if (i != ml.Initializers)
											ml = Expression.ListBind(ml.Member, i);

										return ml;
									}

								case MemberBindingType.MemberBinding:
									{
										var mm = (MemberMemberBinding)b;
										var bs = Transform(mm.Bindings, modify);

										if (bs != mm.Bindings)
											mm = Expression.MemberBind(mm.Member);

										return mm;
									}
							}

							return b;
						};

						var e  = (MemberInitExpression)expr;
						var ne = Transform(e.NewExpression, func);
						var bb = Transform2(e.Bindings,      modify);

						return ne != e.NewExpression || bb != e.Bindings ?
							Expression.MemberInit((NewExpression)ne, bb) :
							expr;
					}

				case ExpressionType.New:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e = (NewExpression)expr;
						var a = Transform2(e.Arguments, func);

						return a != e.Arguments ?
							e.Members == null ?
								Expression.New(e.Constructor, a) :
								Expression.New(e.Constructor, a, e.Members) :
							expr;
					}

				case ExpressionType.NewArrayBounds:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (NewArrayExpression)expr;
						var ex = Transform2(e.Expressions, func);

						return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : expr;
					}

				case ExpressionType.NewArrayInit:
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (NewArrayExpression)expr;
						var ex = Transform2(e.Expressions, func);

						return ex != e.Expressions ?
							Expression.NewArrayInit(e.Type.GetElementType(), ex) :
							expr;
					}

				case ExpressionType.TypeIs :
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (TypeBinaryExpression)expr;
						var ex = Transform(e.Expression, func);

						return ex != e.Expression ? Expression.TypeIs(ex, e.Type) : expr;
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block :
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (BlockExpression)expr;
						var ex = Transform2(e.Expressions, func);
						var v  = Transform2(e.Variables,   func);

						return ex != e.Expressions || v != e.Variables ? Expression.Block(e.Type, v, ex) : expr;
					}

#endif

				case ExpressionType.Extension:
				case ExpressionType.Constant :
				case ExpressionType.Parameter: return func(expr).Expression;

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					{
						ti = func(expr);
						if (ti.Stop || ti.Expression != expr)
							return ti.Expression;

						var e  = (ChangeTypeExpression)expr;
						var ex = Transform(e.Expression, func);

						if (ex == e.Expression)
							return expr;

						if (ex.Type == e.Type)
							return ex;

						return new ChangeTypeExpression(ex, e.Type);
					}
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region Helpers

		static public Expression Unwrap(this Expression ex)
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

						if (!ue.Operand.Type.IsEnum)
							return ue.Operand.Unwrap();

						break;
					}
			}

			return ex;
		}

		static public Dictionary<Expression,Expression> GetExpressionAccessors(this Expression expression, Expression path)
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

		static public Expression GetRootObject(this Expression expr)
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

		static public List<Expression> GetMembers(this Expression expr)
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

		static public bool IsQueryable(this MethodCallExpression method)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(Queryable) || type == typeof(Enumerable) || type == typeof(LinqExtensions);
		}

		static public bool IsQueryable(this MethodCallExpression method, string name)
		{
			return method.Method.Name == name && method.IsQueryable();
		}

		static public bool IsQueryable(this MethodCallExpression method, params string[] names)
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

		static public Expression GetLevelExpression(this Expression expression, int level)
		{
			var current = 0;
			var expr    = FindLevel(expression, level, ref current);

			if (expr == null || current != level)
				throw new InvalidOperationException();

			return expr;
		}

		#endregion
	}
}
