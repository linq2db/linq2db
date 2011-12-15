using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Data.Linq.Builder;
using LinqToDB.Extensions;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode

namespace LinqToDB.Linq
{
	using Common;
	using Data.Linq;
	using Reflection;

	public static class ExpressionHelper
	{
		#region IsConstant

		public static bool IsConstant(Type type)
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

			if (ReflectionExtensions.IsNullableType(type))
				return IsConstant(type.GetGenericArguments()[0]);

			return false;
		}

		#endregion

		#region Compare

		public static bool Compare(Expression expr1, Expression expr2, Dictionary<Expression,Func<Expression,IQueryable>> queryableAccessorDic)
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
							Compare(e1.Conversion, e2.Conversion, queryableAccessorDic) &&
							Compare(e1.Left,       e2.Left,       queryableAccessorDic) &&
							Compare(e1.Right,      e2.Right,      queryableAccessorDic);
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
						return e1.Method == e2.Method && Compare(e1.Operand, e2.Operand, queryableAccessorDic);
					}

				case ExpressionType.Call:
					{
						var e1 = (MethodCallExpression)expr1;
						var e2 = (MethodCallExpression)expr2;

						if (e1.Arguments.Count != e2.Arguments.Count || e1.Method != e2.Method)
							return false;

						if (queryableAccessorDic.Count > 0)
						{
							Func<Expression,IQueryable> func;

							if (queryableAccessorDic.TryGetValue(expr1, out func))
								return Compare(func(expr1).Expression, func(expr2).Expression, queryableAccessorDic);
						}

						if (!Compare(e1.Object, e2.Object, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Arguments.Count; i++)
							if (!Compare(e1.Arguments[i], e2.Arguments[i], queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.Conditional:
					{
						var e1 = (ConditionalExpression)expr1;
						var e2 = (ConditionalExpression)expr2;
						return
							Compare(e1.Test,    e2.Test,   queryableAccessorDic) &&
							Compare(e1.IfTrue,  e2.IfTrue, queryableAccessorDic) &&
							Compare(e1.IfFalse, e2.IfFalse, queryableAccessorDic);
					}

				case ExpressionType.Constant:
					{
						var e1 = (ConstantExpression)expr1;
						var e2 = (ConstantExpression)expr2;

						return e1.Value == null && e2.Value == null || IsConstant(e1.Type) ? Equals(e1.Value, e2.Value) : true;
					}

				case ExpressionType.Invoke:
					{
						var e1 = (InvocationExpression)expr1;
						var e2 = (InvocationExpression)expr2;

						if (e1.Arguments.Count != e2.Arguments.Count || !Compare(e1.Expression, e2.Expression, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Arguments.Count; i++)
							if (!Compare(e1.Arguments[i], e2.Arguments[i], queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.Lambda:
					{
						var e1 = (LambdaExpression)expr1;
						var e2 = (LambdaExpression)expr2;

						if (e1.Parameters.Count != e2.Parameters.Count || !Compare(e1.Body, e2.Body, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Parameters.Count; i++)
							if (!Compare(e1.Parameters[i], e2.Parameters[i], queryableAccessorDic))
								return false;

						return true;
					}

				case ExpressionType.ListInit:
					{
						var e1 = (ListInitExpression)expr1;
						var e2 = (ListInitExpression)expr2;

						if (e1.Initializers.Count != e2.Initializers.Count || !Compare(e1.NewExpression, e2.NewExpression, queryableAccessorDic))
							return false;

						for (var i = 0; i < e1.Initializers.Count; i++)
						{
							var i1 = e1.Initializers[i];
							var i2 = e2.Initializers[i];

							if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
								return false;

							for (var j = 0; j < i1.Arguments.Count; j++)
								if (!Compare(i1.Arguments[j], i2.Arguments[j], queryableAccessorDic))
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
							if (e1.Expression == e2.Expression)
							{
								if (queryableAccessorDic.Count > 0)
								{
									Func<Expression,IQueryable> func;

									if (queryableAccessorDic.TryGetValue(expr1, out func))
										return Compare(func(expr1).Expression, func(expr2).Expression, queryableAccessorDic);
								}
							}

							return Compare(e1.Expression, e2.Expression, queryableAccessorDic);
						}

						return false;
					}

				case ExpressionType.MemberInit:
					{
						var e1 = (MemberInitExpression)expr1;
						var e2 = (MemberInitExpression)expr2;

						if (e1.Bindings.Count != e2.Bindings.Count || !Compare(e1.NewExpression, e2.NewExpression, queryableAccessorDic))
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
									return Compare(((MemberAssignment)b1).Expression, ((MemberAssignment)b2).Expression, queryableAccessorDic);

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
											if (!Compare(ei1.Arguments[j], ei2.Arguments[j], queryableAccessorDic))
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
							if (!Compare(e1.Arguments[i], e2.Arguments[i], queryableAccessorDic))
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
							if (!Compare(e1.Expressions[i], e2.Expressions[i], queryableAccessorDic))
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
						return e1.TypeOperand == e2.TypeOperand && Compare(e1.Expression, e2.Expression, queryableAccessorDic);
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var e1 = (BlockExpression)expr1;
						var e2 = (BlockExpression)expr2;

						for (var i = 0; i < e1.Expressions.Count; i++)
							if (!Compare(e1.Expressions[i], e2.Expressions[i], queryableAccessorDic))
								return false;

						for (var i = 0; i < e1.Variables.Count; i++)
							if (!Compare(e1.Variables[i], e2.Variables[i], queryableAccessorDic))
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

		static void Path<T>(IEnumerable<T> source, Expression path, MethodInfo property, Action<T,Expression> func)
			where T : class
		{
			var prop = Expression.Property(path, property);
			var i    = 0;
			foreach (var item in source)
				func(item, Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, new Expression[] { Expression.Constant(i++) }));
		}

		static void Path<T>(IEnumerable<T> source, Expression path, MethodInfo property, Action<Expression,Expression> func)
			where T : Expression
		{
			var prop = Expression.Property(path, property);
			var i    = 0;
			foreach (var item in source)
				Path(item, Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, new Expression[] { Expression.Constant(i++) }), func);
		}

		static void Path(Expression expr, Expression path, MethodInfo property, Action<Expression,Expression> func)
		{
			Path(expr, Expression.Property(path, property), func);
		}

		public static void Path(this Expression expr, Expression path, Action<Expression,Expression> func)
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
						path  = ConvertTo(path, typeof(BinaryExpression));
						var e = (BinaryExpression)expr;

						Path(e.Conversion, path, ReflectionHelper.Binary.Conversion, func);
						Path(e.Left,       path, ReflectionHelper.Binary.Left,       func);
						Path(e.Right,      path, ReflectionHelper.Binary.Right,      func);

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
						path = ConvertTo(path, typeof(UnaryExpression)),
						ReflectionHelper.Unary.Operand,
						func);
					break;

				case ExpressionType.Call:
					{
						path  = ConvertTo(path, typeof(MethodCallExpression));
						var e = (MethodCallExpression)expr;

						Path(e.Object,    path, ReflectionHelper.MethodCall.Object,    func);
						Path(e.Arguments, path, ReflectionHelper.MethodCall.Arguments, func);

						break;
					}

				case ExpressionType.Conditional:
					{
						path  = ConvertTo(path, typeof(ConditionalExpression));
						var e = (ConditionalExpression)expr;

						Path(e.Test,    path, ReflectionHelper.Conditional.Test,    func);
						Path(e.IfTrue,  path, ReflectionHelper.Conditional.IfTrue,  func);
						Path(e.IfFalse, path, ReflectionHelper.Conditional.IfFalse, func);

						break;
					}

				case ExpressionType.Invoke:
					{
						path  = ConvertTo(path, typeof(InvocationExpression));
						var e = (InvocationExpression)expr;

						Path(e.Expression, path, ReflectionHelper.Invocation.Expression, func);
						Path(e.Arguments,  path, ReflectionHelper.Invocation.Arguments,  func);

						break;
					}

				case ExpressionType.Lambda:
					{
						path  = ConvertTo(path, typeof(LambdaExpression));
						var e = (LambdaExpression)expr;

						Path(e.Body,       path, ReflectionHelper.LambdaExpr.Body,       func);
						Path(e.Parameters, path, ReflectionHelper.LambdaExpr.Parameters, func);

						break;
					}

				case ExpressionType.ListInit:
					{
						path  = ConvertTo(path, typeof(ListInitExpression));
						var e = (ListInitExpression)expr;

						Path(e.NewExpression, path, ReflectionHelper.ListInit.NewExpression, func);
						Path(e.Initializers,  path, ReflectionHelper.ListInit.Initializers, (ex,p) => Path(ex.Arguments, p, ReflectionHelper.ElementInit.Arguments, func));

						break;
					}

				case ExpressionType.MemberAccess:
					Path(
						((MemberExpression)expr).Expression,
						path = ConvertTo(path, typeof(MemberExpression)),
						ReflectionHelper.Member.Expression,
						func);
					break;

				case ExpressionType.MemberInit:
					{
						Action<MemberBinding,Expression> modify = null; modify = (b,pinf) =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									Path(
										((MemberAssignment)b).Expression,
										ConvertTo(pinf, typeof(MemberAssignment)),
										ReflectionHelper.MemberAssignmentBind.Expression,
										func);
									break;

								case MemberBindingType.ListBinding:
									Path(
										((MemberListBinding)b).Initializers,
										ConvertTo(pinf, typeof(MemberListBinding)),
										ReflectionHelper.MemberListBind.Initializers,
										(p,psi) => Path(p.Arguments, psi, ReflectionHelper.ElementInit.Arguments, func));
									break;

								case MemberBindingType.MemberBinding:
									Path(
										((MemberMemberBinding)b).Bindings,
										ConvertTo(pinf, typeof(MemberMemberBinding)),
										ReflectionHelper.MemberMemberBind.Bindings,
										modify);
									break;
							}
						};

						path  = ConvertTo(path, typeof(MemberInitExpression));
						var e = (MemberInitExpression)expr;

						Path(e.NewExpression, path, ReflectionHelper.MemberInit.NewExpression, func);
						Path(e.Bindings,      path, ReflectionHelper.MemberInit.Bindings,      modify);

						break;
					}

				case ExpressionType.New:
					Path(
						((NewExpression)expr).Arguments,
						path = ConvertTo(path, typeof(NewExpression)),
						ReflectionHelper.New.Arguments,
						func);
					break;

				case ExpressionType.NewArrayBounds:
					Path(
						((NewArrayExpression)expr).Expressions,
						path = ConvertTo(path, typeof(NewArrayExpression)),
						ReflectionHelper.NewArray.Expressions,
						func);
					break;

				case ExpressionType.NewArrayInit:
					Path(
						((NewArrayExpression)expr).Expressions,
						path = ConvertTo(path, typeof(NewArrayExpression)),
						ReflectionHelper.NewArray.Expressions,
						func);
					break;

				case ExpressionType.TypeIs:
					Path(
						((TypeBinaryExpression)expr).Expression,
						path = ConvertTo(path, typeof(TypeBinaryExpression)),
						ReflectionHelper.TypeBinary.Expression,
						func);
					break;

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						path  = ConvertTo(path, typeof(BlockExpression));
						var e = (BlockExpression)expr;

						Path(e.Expressions, path, ReflectionHelper.Block.Expressions, func);
						Path(e.Variables,   path, ReflectionHelper.Block.Variables,   func); // ?

						break;
					}

#endif

				case ExpressionType.Constant : path = ConvertTo(path, typeof(ConstantExpression));  break;
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

		#region Convert

		static IEnumerable<T> Convert<T>(IEnumerable<T> source, Func<T,T> func)
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

		static IEnumerable<T> Convert<T>(IEnumerable<T> source, Func<Expression,Expression> func)
			where T : Expression
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = Convert(item, func);
				list.Add((T)e);
				modified = modified || e != item;
			}

			return modified? list: source;
		}

		public static Expression Convert(this Expression expr, Func<Expression,Expression> func)
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
						var c = Convert(e.Conversion, func);
						var l = Convert(e.Left,       func);
						var r = Convert(e.Right,      func);

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

						var e = expr as UnaryExpression;
						var o = Convert(e.Operand, func);

						return o != e.Operand ?
							Expression.MakeUnary(expr.NodeType, o, e.Type, e.Method) :
							expr;
					}

				case ExpressionType.Call:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = expr as MethodCallExpression;
						var o = Convert(e.Object,    func);
						var a = Convert(e.Arguments, func);

						return o != e.Object || a != e.Arguments ? 
							Expression.Call(o, e.Method, a) : 
							expr;
					}

				case ExpressionType.Conditional:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = expr as ConditionalExpression;
						var s = Convert(e.Test,    func);
						var t = Convert(e.IfTrue,  func);
						var f = Convert(e.IfFalse, func);

						return s != e.Test || t != e.IfTrue || f != e.IfFalse ?
							Expression.Condition(s, t, f) :
							expr;
					}

				case ExpressionType.Invoke:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as InvocationExpression;
						var ex = Convert(e.Expression, func);
						var a  = Convert(e.Arguments,  func);

						return ex != e.Expression || a != e.Arguments ? Expression.Invoke(ex, a) : expr;
					}

				case ExpressionType.Lambda:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = expr as LambdaExpression;
						var b = Convert(e.Body,       func);
						var p = Convert(e.Parameters, func);

						return b != e.Body || p != e.Parameters ? Expression.Lambda(ex.Type, b, p.ToArray()) : expr;
					}

				case ExpressionType.ListInit:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = expr as ListInitExpression;
						var n = Convert(e.NewExpression, func);
						var i = Convert(e.Initializers,  p =>
						{
							var args = Convert(p.Arguments, func);
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

						var e  = expr as MemberExpression;
						var ex = Convert(e.Expression, func);

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
										var ex = Convert(ma.Expression, func);

										if (ex != ma.Expression)
											ma = Expression.Bind(ma.Member, ex);

										return ma;
									}

								case MemberBindingType.ListBinding:
									{
										var ml = (MemberListBinding)b;
										var i  = Convert(ml.Initializers, p =>
										{
											var args = Convert(p.Arguments, func);
											return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
										});

										if (i != ml.Initializers)
											ml = Expression.ListBind(ml.Member, i);

										return ml;
									}

								case MemberBindingType.MemberBinding:
									{
										var mm = (MemberMemberBinding)b;
										var bs = Convert(mm.Bindings, modify);

										if (bs != mm.Bindings)
											mm = Expression.MemberBind(mm.Member);

										return mm;
									}
							}

							return b;
						};

						var e  = expr as MemberInitExpression;
						var ne = Convert(e.NewExpression, func);
						var bb = Convert(e.Bindings,      modify);

						return ne != e.NewExpression || bb != e.Bindings ?
							Expression.MemberInit((NewExpression)ne, bb) :
							expr;
					}

				case ExpressionType.New:
					{
						var ex = func(expr);
						if (ex != expr)
							return ex;

						var e = expr as NewExpression;
						var a = Convert(e.Arguments, func);

						return a != e.Arguments ?
							e.Members == null ?
								Expression.New(e.Constructor, a) :
								Expression.New(e.Constructor, a, e.Members) :
							expr;
					}

				case ExpressionType.NewArrayBounds:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as NewArrayExpression;
						var ex = Convert(e.Expressions, func);

						return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : expr;
					}

				case ExpressionType.NewArrayInit:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as NewArrayExpression;
						var ex = Convert(e.Expressions, func);

						return ex != e.Expressions ?
							Expression.NewArrayInit(e.Type.GetElementType(), ex) :
							expr;
					}

				case ExpressionType.TypeIs:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as TypeBinaryExpression;
						var ex = Convert(e.Expression, func);

						return ex != e.Expression ? Expression.TypeIs(ex, e.Type) : expr;
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block:
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as BlockExpression;
						var ex = Convert(e.Expressions, func);
						var v  = Convert(e.Variables,   func);

						return ex != e.Expressions || v != e.Variables ? Expression.Block(e.Type, v, ex) : expr;
					}

#endif

				case ExpressionType.Constant : return func(expr);
				case ExpressionType.Parameter: return func(expr);

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					{
						var exp = func(expr);
						if (exp != expr)
							return exp;

						var e  = expr as ChangeTypeExpression;
						var ex = Convert(e.Expression, func);

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

		#region Convert2

		public struct ConvertInfo
		{
			public ConvertInfo(Expression expression, bool stop)
			{
				Expression = expression;
				Stop       = stop;
			}

			public ConvertInfo(Expression expression)
			{
				Expression = expression;
				Stop       = false;
			}

			public Expression Expression;
			public bool       Stop;
		}

		static IEnumerable<T> Convert2<T>(IEnumerable<T> source, Func<T,T> func)
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

		static IEnumerable<T> Convert2<T>(IEnumerable<T> source, Func<Expression,ConvertInfo> func)
			where T : Expression
		{
			var modified = false;
			var list     = new List<T>();

			foreach (var item in source)
			{
				var e = Convert2(item, func);
				list.Add((T)e);
				modified = modified || e != item;
			}

			return modified ? list : source;
		}

		public static Expression Convert2(this Expression expr, Func<Expression,ConvertInfo> func)
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
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as BinaryExpression;
						var c = Convert2(e.Conversion, func);
						var l = Convert2(e.Left,       func);
						var r = Convert2(e.Right,      func);

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
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as UnaryExpression;
						var o = Convert2(e.Operand, func);

						return o != e.Operand ?
							Expression.MakeUnary(expr.NodeType, o, e.Type, e.Method) :
							expr;
					}

				case ExpressionType.Call:
					{
						var ex = func(expr);
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as MethodCallExpression;
						var o = Convert2(e.Object,    func);
						var a = Convert2(e.Arguments, func);

						return o != e.Object || a != e.Arguments ? 
							Expression.Call(o, e.Method, a) : 
							expr;
					}

				case ExpressionType.Conditional:
					{
						var ex = func(expr);
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as ConditionalExpression;
						var s = Convert2(e.Test,    func);
						var t = Convert2(e.IfTrue,  func);
						var f = Convert2(e.IfFalse, func);

						return s != e.Test || t != e.IfTrue || f != e.IfFalse ?
							Expression.Condition(s, t, f) :
							expr;
					}

				case ExpressionType.Invoke:
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as InvocationExpression;
						var ex = Convert2(e.Expression, func);
						var a  = Convert2(e.Arguments,  func);

						return ex != e.Expression || a != e.Arguments ? Expression.Invoke(ex, a) : expr;
					}

				case ExpressionType.Lambda:
					{
						var ex = func(expr);
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as LambdaExpression;
						var b = Convert2(e.Body,       func);
						var p = Convert2(e.Parameters, func);

						return b != e.Body || p != e.Parameters ? Expression.Lambda(ex.Expression.Type, b, p.ToArray()) : expr;
					}

				case ExpressionType.ListInit:
					{
						var ex = func(expr);
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as ListInitExpression;
						var n = Convert2(e.NewExpression, func);
						var i = Convert2(e.Initializers,  p =>
						{
							var args = Convert2(p.Arguments, func);
							return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
						});

						return n != e.NewExpression || i != e.Initializers ?
							Expression.ListInit((NewExpression)n, i) :
							expr;
					}

				case ExpressionType.MemberAccess:
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as MemberExpression;
						var ex = Convert2(e.Expression, func);

						return ex != e.Expression ? Expression.MakeMemberAccess(ex, e.Member) : expr;
					}

				case ExpressionType.MemberInit:
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						Func<MemberBinding,MemberBinding> modify = null; modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									{
										var ma = (MemberAssignment)b;
										var ex = Convert2(ma.Expression, func);

										if (ex != ma.Expression)
											ma = Expression.Bind(ma.Member, ex);

										return ma;
									}

								case MemberBindingType.ListBinding:
									{
										var ml = (MemberListBinding)b;
										var i  = Convert(ml.Initializers, p =>
										{
											var args = Convert2(p.Arguments, func);
											return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
										});

										if (i != ml.Initializers)
											ml = Expression.ListBind(ml.Member, i);

										return ml;
									}

								case MemberBindingType.MemberBinding:
									{
										var mm = (MemberMemberBinding)b;
										var bs = Convert(mm.Bindings, modify);

										if (bs != mm.Bindings)
											mm = Expression.MemberBind(mm.Member);

										return mm;
									}
							}

							return b;
						};

						var e  = expr as MemberInitExpression;
						var ne = Convert2(e.NewExpression, func);
						var bb = Convert2(e.Bindings,      modify);

						return ne != e.NewExpression || bb != e.Bindings ?
							Expression.MemberInit((NewExpression)ne, bb) :
							expr;
					}

				case ExpressionType.New:
					{
						var ex = func(expr);
						if (ex.Stop || ex.Expression != expr)
							return ex.Expression;

						var e = expr as NewExpression;
						var a = Convert2(e.Arguments, func);

						return a != e.Arguments ?
							e.Members == null ?
								Expression.New(e.Constructor, a) :
								Expression.New(e.Constructor, a, e.Members) :
							expr;
					}

				case ExpressionType.NewArrayBounds:
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as NewArrayExpression;
						var ex = Convert2(e.Expressions, func);

						return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : expr;
					}

				case ExpressionType.NewArrayInit:
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as NewArrayExpression;
						var ex = Convert2(e.Expressions, func);

						return ex != e.Expressions ?
							Expression.NewArrayInit(e.Type.GetElementType(), ex) :
							expr;
					}

				case ExpressionType.TypeIs :
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as TypeBinaryExpression;
						var ex = Convert2(e.Expression, func);

						return ex != e.Expression ? Expression.TypeIs(ex, e.Type) : expr;
					}

#if FW4 || SILVERLIGHT

				case ExpressionType.Block :
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as BlockExpression;
						var ex = Convert2(e.Expressions, func);
						var v  = Convert2(e.Variables,   func);

						return ex != e.Expressions || v != e.Variables ? Expression.Block(e.Type, v, ex) : expr;
					}

#endif

				case ExpressionType.Constant : return func(expr).Expression;
				case ExpressionType.Parameter: return func(expr).Expression;

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					{
						var exp = func(expr);
						if (exp.Stop || exp.Expression != expr)
							return exp.Expression;

						var e  = expr as ChangeTypeExpression;
						var ex = Convert2(e.Expression, func);

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
			var accessors = new Dictionary<Expression, Expression>();

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

						if (e.Expression != null)
							list = GetMembers(e.Expression.Unwrap());
						else
							list = new List<Expression>();

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
