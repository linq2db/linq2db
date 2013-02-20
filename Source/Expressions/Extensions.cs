using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public static class Extensions
	{
		#region GetCount

		public static int GetCount(this Expression expr, Func<Expression,bool> func)
		{
			var n = 0;

			expr.Visit(e =>
			{
				if (func(e))
					n++;
			});

			return n;
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
				case ExpressionType.Assign:
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

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

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
				case ExpressionType.Assign:
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
					{
						Visit(((UnaryExpression)expr).Operand, func);
						break;
					}

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

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

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
				case ExpressionType.Assign:
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

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						return
							Find(e.Expressions, func) ??
							Find(e.Variables,   func);
					}

				case (ExpressionType)ChangeTypeExpression.ChangeTypeType :
					return Find(((ChangeTypeExpression)expr).Expression, func);

				case ExpressionType.Extension :
					if (expr.CanReduce)
						return Find(expr.Reduce(), func);
					break;
			}

			return null;
		}

		#endregion

		#region Transform

		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return Transform(lambda.Body, e => e == lambda.Parameters[0] ? exprToReplaceParameter : e);
		}

		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2)
		{
			return Transform(lambda.Body, e =>
				e == lambda.Parameters[0] ? exprToReplaceParameter1 :
				e == lambda.Parameters[1] ? exprToReplaceParameter2 : e);
		}

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
				case ExpressionType.Assign:
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
				case ExpressionType.Assign:
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

						return e.Update(Transform(e.Operand, func));
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
	}
}
