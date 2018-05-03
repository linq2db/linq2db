﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;

	public static class Extensions
	{
		#region GetDebugView

		private static Func<Expression,string> _getDebugView;

		/// <summary>
		/// Gets the DebugView internal property value of provided expression.
		/// </summary>
		/// <param name="expression">Expression to get DebugView.</param>
		/// <returns>DebugView value.</returns>
		public static string GetDebugView(this Expression expression)
		{
			if (_getDebugView == null)
			{
				var p = Expression.Parameter(typeof(Expression));

				try
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						Expression.PropertyOrField(p, "DebugView"),
						p);

					_getDebugView = l.Compile();
				}
				catch (ArgumentException)
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						Expression.Call(p, MemberHelper.MethodOf<Expression>(e => e.ToString())),
						p);

					_getDebugView = l.Compile();
				}
			}

			return _getDebugView(expression);
		}

		#endregion

		#region GetCount

		/// <summary>
		/// Returns the total number of expression items which are matching the given.
		/// <paramref name="func"/>.
		/// </summary>
		/// <param name="expr">Expression-Tree which gets counted.</param>
		/// <param name="func">Predicate which is used to test if the given expression should be counted.</param>
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

		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
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
				case ExpressionType.AddAssign:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
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
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.IsFalse:
				case ExpressionType.IsTrue:
				case ExpressionType.Throw:
				case ExpressionType.Unbox:
				case ExpressionType.PreIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.OnesComplement:
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
						void MemberVisit(MemberBinding b)
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : Visit(((MemberAssignment)b). Expression,   func);                             break;
								case MemberBindingType.ListBinding   : Visit(((MemberListBinding)b).Initializers, p => Visit(p.Arguments, func));    break;
								case MemberBindingType.MemberBinding : Visit(((MemberMemberBinding)b).Bindings, (Action<MemberBinding>)MemberVisit); break;
							}
						}

						var e = (MemberInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Bindings,      (Action<MemberBinding>)MemberVisit);

						break;
					}

				case ExpressionType.New            : Visit(((NewExpression)       expr).Arguments,   func); break;
				case ExpressionType.NewArrayBounds : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.NewArrayInit   : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.TypeEqual      :
				case ExpressionType.TypeIs         : Visit(((TypeBinaryExpression)expr).Expression,  func); break;

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

				case ChangeTypeExpression.ChangeTypeType :
					Visit(((ChangeTypeExpression)expr).Expression,  func); break;

				case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						Visit(e.Value, func);

						break;
					}

				case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						Visit(e.Object,    func);
						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						Visit(e.DefaultValue, func);

						break;
					}

				case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						Visit(e.Variables, func);

						break;
					}

				case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						Visit(e.Body, func);

						break;
					}

				case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						Visit(e.SwitchValue, func);
						Visit(e.Cases, cs => { Visit(cs.TestValues, func); Visit(cs.Body, func); });
						Visit(e.DefaultBody, func);

						break;
					}

				case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						Visit(e.Body, func);
						Visit(e.Handlers, h => { Visit(h.Variable, func); Visit(h.Filter, func); Visit(h.Body, func); });
						Visit(e.Finally, func);
						Visit(e.Fault, func);

						break;
					}

				case ExpressionType.Extension:
					{
						if (expr.CanReduce)
							Visit(expr.Reduce(), func);

						break;
					}
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

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
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
				case ExpressionType.AddAssign:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
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
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.IsFalse:
				case ExpressionType.IsTrue:
				case ExpressionType.Throw:
				case ExpressionType.Unbox:
				case ExpressionType.PreIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.OnesComplement:
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
						bool Modify(MemberBinding b)
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : Visit(((MemberAssignment)b). Expression,   func);                          break;
								case MemberBindingType.ListBinding   : Visit(((MemberListBinding)b).Initializers, p => Visit(p.Arguments, func)); break;
								case MemberBindingType.MemberBinding : Visit(((MemberMemberBinding)b).Bindings,   Modify);                        break;
							}

							return true;
						}

						var e = (MemberInitExpression)expr;

						Visit(e.NewExpression, func);
						Visit(e.Bindings,      Modify);

						break;
					}

				case ExpressionType.New            : Visit(((NewExpression)       expr).Arguments,   func); break;
				case ExpressionType.NewArrayBounds : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.NewArrayInit   : Visit(((NewArrayExpression)  expr).Expressions, func); break;
				case ExpressionType.TypeEqual      :
				case ExpressionType.TypeIs         : Visit(((TypeBinaryExpression)expr).Expression,  func); break;

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						Visit(e.Expressions, func);
						Visit(e.Variables,   func);

						break;
					}

				case ChangeTypeExpression.ChangeTypeType :
					Visit(((ChangeTypeExpression)expr).Expression,  func);
					break;

				case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						Visit(e.Value, func);

						break;
					}

				case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						Visit(e.Object,    func);
						Visit(e.Arguments, func);

						break;
					}

				case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						Visit(e.DefaultValue, func);

						break;
					}

				case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						Visit(e.Variables, func);

						break;
					}

				case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						Visit(e.Body, func);

						break;
					}

				case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						Visit(e.SwitchValue, func);
						Visit(e.Cases, cs => { Visit(cs.TestValues, func); Visit(cs.Body, func); });
						Visit(e.DefaultBody, func);

						break;
					}

				case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						Visit(e.Body, func);
						Visit(e.Handlers, h => { Visit(h.Variable, func); Visit(h.Filter, func); Visit(h.Body, func); });
						Visit(e.Finally, func);
						Visit(e.Fault, func);

						break;
					}

				case ExpressionType.Extension:
					{
						if (expr.CanReduce)
							Visit(expr.Reduce(), func);

						break;
					}
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

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression Find(this Expression expr, Expression exprToFind)
		{
			return expr.Find(e => e == exprToFind);
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
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
				case ExpressionType.AddAssign:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
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
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.IsFalse:
				case ExpressionType.IsTrue:
				case ExpressionType.Throw:
				case ExpressionType.Unbox:
				case ExpressionType.PreIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.OnesComplement:
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
						Expression MemberFind(MemberBinding b)
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    : return Find(((MemberAssignment)b).   Expression,   func);
								case MemberBindingType.ListBinding   : return Find(((MemberListBinding)b).  Initializers, p => Find(p.Arguments, func));
								case MemberBindingType.MemberBinding : return Find(((MemberMemberBinding)b).Bindings,     MemberFind);
							}

							return null;
						}

						var e = (MemberInitExpression)expr;

						return
							Find(e.NewExpression, func) ??
							Find(e.Bindings,      MemberFind);
					}

				case ExpressionType.New            : return Find(((NewExpression)       expr).Arguments,   func);
				case ExpressionType.NewArrayBounds : return Find(((NewArrayExpression)  expr).Expressions, func);
				case ExpressionType.NewArrayInit   : return Find(((NewArrayExpression)  expr).Expressions, func);
				case ExpressionType.TypeEqual      :
				case ExpressionType.TypeIs         : return Find(((TypeBinaryExpression)expr).Expression,  func);

				case ExpressionType.Block:
					{
						var e = (BlockExpression)expr;

						return
							Find(e.Expressions, func) ??
							Find(e.Variables,   func);
					}

				case ChangeTypeExpression.ChangeTypeType :
					return Find(((ChangeTypeExpression)expr).Expression, func);

				case ExpressionType.Dynamic:
					{
						var e = (DynamicExpression)expr;

						return
							Find(e.Arguments, func);
					}

				case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;

						return
							Find(e.Value, func);
					}

				case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;

						return
							Find(e.Object,    func) ??
							Find(e.Arguments, func);
					}

				case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;

						return
							Find(e.DefaultValue, func);
					}

				case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;

						return
							Find(e.Variables, func);
					}

				case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;

						return
							Find(e.Body, func);
					}


				case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;

						return
							Find(e.SwitchValue, func) ??
							Find(e.Cases, cs => Find(cs.TestValues, func) ?? Find(cs.Body, func)) ??
							Find(e.DefaultBody, func);
					}

				case ExpressionType.Try:
					{
						var e = (TryExpression)expr;

						return
							Find(e.Body,     func) ??
							Find(e.Handlers, h => Find(h.Variable, func) ?? Find(h.Filter, func) ?? Find(h.Body, func)) ??
							Find(e.Finally,  func) ??
							Find(e.Fault,    func);
					}

				case ExpressionType.Extension:
					if (expr.CanReduce)
						return Find(expr.Reduce(), func);

					break;
			}

			return null;
		}

		#endregion

		#region Transform

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first parameter of that
		/// lambda expression with the <paramref name="exprToReplaceParameter"/> expression.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return Transform(lambda.Body, e => e == lambda.Parameters[0] ? exprToReplaceParameter : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first two parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
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

			return modified ? list : source;
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		public static Expression Transform(this Expression expr, [InstantHandle] Func<Expression,Expression> func)
		{
			if (expr == null)
				return null;

			{
				var ex = func(expr);
				if (ex != expr)
					return ex;
			}

			switch (expr.NodeType)
			{
				case ExpressionType.Add                   :
				case ExpressionType.AddChecked            :
				case ExpressionType.And                   :
				case ExpressionType.AndAlso               :
				case ExpressionType.ArrayIndex            :
				case ExpressionType.Assign                :
				case ExpressionType.Coalesce              :
				case ExpressionType.Divide                :
				case ExpressionType.Equal                 :
				case ExpressionType.ExclusiveOr           :
				case ExpressionType.GreaterThan           :
				case ExpressionType.GreaterThanOrEqual    :
				case ExpressionType.LeftShift             :
				case ExpressionType.LessThan              :
				case ExpressionType.LessThanOrEqual       :
				case ExpressionType.Modulo                :
				case ExpressionType.Multiply              :
				case ExpressionType.MultiplyChecked       :
				case ExpressionType.NotEqual              :
				case ExpressionType.Or                    :
				case ExpressionType.OrElse                :
				case ExpressionType.Power                 :
				case ExpressionType.RightShift            :
				case ExpressionType.Subtract              :
				case ExpressionType.SubtractChecked       :
				case ExpressionType.AddAssign             :
				case ExpressionType.AndAssign             :
				case ExpressionType.DivideAssign          :
				case ExpressionType.ExclusiveOrAssign     :
				case ExpressionType.LeftShiftAssign       :
				case ExpressionType.ModuloAssign          :
				case ExpressionType.MultiplyAssign        :
				case ExpressionType.OrAssign              :
				case ExpressionType.PowerAssign           :
				case ExpressionType.RightShiftAssign      :
				case ExpressionType.SubtractAssign        :
				case ExpressionType.AddAssignChecked      :
				case ExpressionType.MultiplyAssignChecked :
				case ExpressionType.SubtractAssignChecked : return TransformX((BinaryExpression)expr, func);

				case ExpressionType.ArrayLength           :
				case ExpressionType.Convert               :
				case ExpressionType.ConvertChecked        :
				case ExpressionType.Negate                :
				case ExpressionType.NegateChecked         :
				case ExpressionType.Not                   :
				case ExpressionType.Quote                 :
				case ExpressionType.TypeAs                :
				case ExpressionType.UnaryPlus             :
				case ExpressionType.Decrement             :
				case ExpressionType.Increment             :
				case ExpressionType.IsFalse               :
				case ExpressionType.IsTrue                :
				case ExpressionType.Throw                 :
				case ExpressionType.Unbox                 :
				case ExpressionType.PreIncrementAssign    :
				case ExpressionType.PreDecrementAssign    :
				case ExpressionType.PostIncrementAssign   :
				case ExpressionType.PostDecrementAssign   :
				case ExpressionType.OnesComplement        : return TransformX((UnaryExpression)expr, func);

				case ExpressionType.Call                  : return TransformX((MethodCallExpression)expr, func);
				case ExpressionType.Lambda                : return TransformX((LambdaExpression)expr,     func);
				case ExpressionType.ListInit              : return TransformX((ListInitExpression)expr,   func);
				case ExpressionType.MemberAccess          : return TransformX((MemberExpression)expr,     func);
				case ExpressionType.MemberInit            : return TransformX((MemberInitExpression)expr, func);

				case ExpressionType.Conditional           :
					{
						return ((ConditionalExpression)expr).Update(
							Transform(((ConditionalExpression)expr).Test,    func),
							Transform(((ConditionalExpression)expr).IfTrue,  func),
							Transform(((ConditionalExpression)expr).IfFalse, func));
					}

				case ExpressionType.Invoke                :
					{
						return ((InvocationExpression)expr).Update(
							Transform(((InvocationExpression)expr).Expression, func),
							Transform(((InvocationExpression)expr).Arguments,  func));
					}

				case ExpressionType.New                   :
					{
						return ((NewExpression)expr).Update(Transform(((NewExpression)expr).Arguments, func));
					}

				case ExpressionType.NewArrayBounds        : return TransformX((NewArrayExpression)expr,     func);
				case ExpressionType.NewArrayInit          : return TransformXInit((NewArrayExpression)expr, func);

				case ExpressionType.TypeEqual             :
				case ExpressionType.TypeIs                :
					{
						return ((TypeBinaryExpression) expr).Update(Transform(((TypeBinaryExpression) expr).Expression, func));
					}

				case ExpressionType.Block                 :
					{
						return ((BlockExpression)expr).Update(
							Transform(((BlockExpression)expr).Variables,   func),
							Transform(((BlockExpression)expr).Expressions, func));
					}

				case ExpressionType.DebugInfo             :
				case ExpressionType.Default               :
				case ExpressionType.Constant              :
				case ExpressionType.Parameter             : return expr;

				case ChangeTypeExpression.ChangeTypeType  : return TransformX((ChangeTypeExpression) expr, func);
				case ExpressionType.Dynamic               : return TransformX((DynamicExpression)expr,     func);

				case ExpressionType.Goto                  :
					{
						return ((GotoExpression)expr).Update(
							((GotoExpression)expr).Target,
							Transform(((GotoExpression)expr).Value, func));
					}

				case ExpressionType.Index                 :
					{
						return ((IndexExpression)expr).Update(
							Transform(((IndexExpression)expr).Object,    func),
							Transform(((IndexExpression)expr).Arguments, func));
					}

				case ExpressionType.Label                 :
					{
						return ((LabelExpression)expr).Update(
							((LabelExpression)expr).Target,
							Transform(((LabelExpression)expr).DefaultValue, func));
					}

				case ExpressionType.RuntimeVariables      :
					{
						return ((RuntimeVariablesExpression)expr).Update(Transform(((RuntimeVariablesExpression)expr).Variables, func));
					}

				case ExpressionType.Loop                  :
					{
						return ((LoopExpression)expr).Update(
							((LoopExpression)expr).BreakLabel,
							((LoopExpression)expr).ContinueLabel,
							Transform(((LoopExpression)expr).Body, func));
					}

				case ExpressionType.Switch                : return TransformX((SwitchExpression)expr, func);
				case ExpressionType.Try                   : return TransformX((TryExpression)expr,    func);
				case ExpressionType.Extension             : return TransformXE(expr,                  func);
			}

			throw new InvalidOperationException();
		}

		// ReSharper disable once InconsistentNaming
		static Expression TransformXE(Expression expr, Func<Expression,Expression> func)
		{
			return expr;
		}

		static Expression TransformX(TryExpression e, Func<Expression,Expression> func)
		{
			var b = Transform(e.Body, func);
			var c = Transform(e.Handlers,
				h => h.Update((ParameterExpression) Transform(h.Variable, func), Transform(h.Filter, func), Transform(h.Body, func)));
			var f = Transform(e.Finally, func);
			var t = Transform(e.Fault, func);

			return e.Update(b, c, f, t);
		}

		static Expression TransformX(SwitchExpression e, Func<Expression,Expression> func)
		{
			var s = Transform(e.SwitchValue, func);
			var c = Transform(e.Cases, cs => cs.Update(Transform(cs.TestValues, func), Transform(cs.Body, func)));
			var d = Transform(e.DefaultBody, func);

			return e.Update(s, c, d);
		}

		static Expression TransformX(DynamicExpression e, Func<Expression,Expression> func)
		{
			var args = Transform(e.Arguments, func);

			return e.Update(args);
		}

		static Expression TransformX(ChangeTypeExpression e, Func<Expression,Expression> func)
		{
			var ex = Transform(e.Expression, func);

			if (ex == e.Expression)
				return e;

			if (ex.Type == e.Type)
				return ex;

			return new ChangeTypeExpression(ex, e.Type);
		}

		static Expression TransformXInit(NewArrayExpression e, Func<Expression,Expression> func)
		{
			var ex = Transform(e.Expressions, func);

			return ex != e.Expressions ? Expression.NewArrayInit(e.Type.GetElementType(), ex) : e;
		}

		static Expression TransformX(NewArrayExpression e, Func<Expression,Expression> func)
		{
			var ex = Transform(e.Expressions, func);

			return ex != e.Expressions ? Expression.NewArrayBounds(e.Type, ex) : e;
		}

		static Expression TransformX(MemberInitExpression e, Func<Expression,Expression> func)
		{
			MemberBinding Modify(MemberBinding b)
			{
				switch (b.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var ma = (MemberAssignment) b;
						return ma.Update(Transform(ma.Expression, func));
					}

					case MemberBindingType.ListBinding:
					{
						var ml = (MemberListBinding) b;
						var i  = Transform(ml.Initializers, p =>
						{
							var args = Transform(p.Arguments, func);
							return args != p.Arguments ? Expression.ElementInit(p.AddMethod, args) : p;
						});

						if (i != ml.Initializers)
							ml = Expression.ListBind(ml.Member, i);

						return ml;
					}

					case MemberBindingType.MemberBinding:
					{
						var mm = (MemberMemberBinding) b;
						var bs = Transform(mm.Bindings, Modify);

						if (bs != mm.Bindings)
							mm = Expression.MemberBind(mm.Member);

						return mm;
					}
				}

				return b;
			}

			return e.Update(
				(NewExpression) Transform(e.NewExpression, func),
				Transform(e.Bindings, Modify));
		}

		static Expression TransformX(MemberExpression e, Func<Expression,Expression> func)
		{
			var ex = Transform(e.Expression, func);

			return ex != e.Expression ? Expression.MakeMemberAccess(ex, e.Member) : e;
		}

		static Expression TransformX(ListInitExpression e, Func<Expression,Expression> func)
		{
			var n = Transform(e.NewExpression, func);
			var i = Transform(e.Initializers,  p =>
			{
				var args = Transform(p.Arguments, func);
				return args != p.Arguments ? Expression.ElementInit(p.AddMethod, args) : p;
			});

			return n != e.NewExpression || i != e.Initializers ? Expression.ListInit((NewExpression) n, i) : e;
		}

		static Expression TransformX(LambdaExpression e, Func<Expression,Expression> func)
		{
			var b = Transform(e.Body,       func);
			var p = Transform(e.Parameters, func);

			return b != e.Body || p != e.Parameters ? Expression.Lambda(e.Type, b, p.ToArray()) : e;
		}

		static Expression TransformX(MethodCallExpression e, Func<Expression,Expression> func)
		{
			var o = Transform(e.Object,    func);
			var a = Transform(e.Arguments, func);

			return o != e.Object || a != e.Arguments ? Expression.Call(o, e.Method, a) : e;
		}

		static Expression TransformX(UnaryExpression e, Func<Expression,Expression> func)
		{
			var o = Transform(e.Operand, func);
			return o != e.Operand ? Expression.MakeUnary(e.NodeType, o, e.Type, e.Method) : e;
		}

		static Expression TransformX(BinaryExpression e, Func<Expression,Expression> func)
		{
			var c = Transform(e.Conversion, func);
			var l = Transform(e.Left,       func);
			var r = Transform(e.Right,      func);

			return c != e.Conversion || l != e.Left || r != e.Right
				? Expression.MakeBinary(e.NodeType, l, r, e.IsLiftedToNull, e.Method, (LambdaExpression)c)
				: e;
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

		static IEnumerable<T> Transform2<T>(IEnumerable<T> source, Func<Expression,TransformInfo> func)
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

			{
				ti = func(expr);
				if (ti.Stop || ti.Expression != expr)
					return ti.Expression;
			}

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
				case ExpressionType.AddAssign:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
					{
						var e = (BinaryExpression)expr;
						var c = Transform(e.Conversion, func);
						var l = Transform(e.Left,       func);
						var r = Transform(e.Right,      func);

						return e.Update(l, (LambdaExpression)c, r);
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
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.IsFalse:
				case ExpressionType.IsTrue:
				case ExpressionType.Throw:
				case ExpressionType.Unbox:
				case ExpressionType.PreIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.OnesComplement:
					{
						var e = (UnaryExpression)expr;

						return e.Update(Transform(e.Operand, func));
					}

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;
						var o = Transform (e.Object,    func);
						var a = Transform2(e.Arguments, func);

						return e.Update(o, a);
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;
						var s = Transform(e.Test,    func);
						var t = Transform(e.IfTrue,  func);
						var f = Transform(e.IfFalse, func);

						return e.Update(s, t, f);
					}

				case ExpressionType.Invoke:
					{
						var e  = (InvocationExpression)expr;
						var ex = Transform(e.Expression, func);
						var a  = Transform2(e.Arguments,  func);

						return e.Update(ex, a);
					}

				case ExpressionType.Lambda:
					{
						var e = (LambdaExpression)expr;
						var b = Transform(e.Body,       func);
						var p = Transform2(e.Parameters, func);

						return b != e.Body || p != e.Parameters ? Expression.Lambda(ti.Expression.Type, b, p.ToArray()) : expr;
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;
						var n = Transform(e.NewExpression, func);
						var i = Transform2(e.Initializers,  p =>
						{
							var args = Transform2(p.Arguments, func);
							return args != p.Arguments? Expression.ElementInit(p.AddMethod, args): p;
						});

						return e.Update((NewExpression)n, i);
					}

				case ExpressionType.MemberAccess:
					{
						var e  = (MemberExpression)expr;
						var ex = Transform(e.Expression, func);

						return e.Update(ex);
					}

				case ExpressionType.MemberInit:
					{
						MemberBinding Modify(MemberBinding b)
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									{
										var ma = (MemberAssignment)b;
										var ex = Transform(ma.Expression, func);

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
										var bs = Transform(mm.Bindings, Modify);

										if (bs != mm.Bindings)
											mm = Expression.MemberBind(mm.Member);

										return mm;
									}
							}

							return b;
						}

						var e  = (MemberInitExpression)expr;
						var ne = Transform (e.NewExpression, func);
						var bb = Transform2(e.Bindings,      Modify);

						return e.Update((NewExpression)ne, bb);
					}

				case ExpressionType.New:
					{
						var e = (NewExpression)expr;
						var a = Transform2(e.Arguments, func);

						return e.Update(a);
					}

				case ExpressionType.NewArrayBounds:
					{
						var e  = (NewArrayExpression)expr;
						var ex = Transform2(e.Expressions, func);

						return e.Update(ex);
					}

				case ExpressionType.NewArrayInit:
					{
						var e  = (NewArrayExpression)expr;
						var ex = Transform2(e.Expressions, func);

						return e.Update(ex);
					}

				case ExpressionType.TypeEqual:
				case ExpressionType.TypeIs :
					{
						var e  = (TypeBinaryExpression)expr;
						var ex = Transform(e.Expression, func);

						return e.Update(ex);
					}

				case ExpressionType.Block :
					{
						var e  = (BlockExpression)expr;
						var ex = Transform2(e.Expressions, func);
						var v  = Transform2(e.Variables,   func);

						return e.Update(v, ex);
					}

				case ExpressionType.DebugInfo:
				case ExpressionType.Default  :
				case ExpressionType.Constant :
				case ExpressionType.Parameter: return ti.Expression;

				case ChangeTypeExpression.ChangeTypeType :
					{
						var e  = (ChangeTypeExpression)expr;
						var ex = Transform(e.Expression, func);

						if (ex == e.Expression)
							return expr;

						if (ex.Type == e.Type)
							return ex;

						return new ChangeTypeExpression(ex, e.Type);
					}

				case ExpressionType.Dynamic:
					{
						var e    = (DynamicExpression)expr;
						var args = Transform2(e.Arguments, func);

						return e.Update(args);
					}

				case ExpressionType.Goto:
					{
						var e = (GotoExpression)expr;
						var v = Transform(e.Value, func);

						return e.Update(e.Target, v);
					}

				case ExpressionType.Index:
					{
						var e = (IndexExpression)expr;
						var o = Transform (e.Object,    func);
						var a = Transform2(e.Arguments, func);

						return e.Update(o, a);
					}

				case ExpressionType.Label:
					{
						var e = (LabelExpression)expr;
						var v = Transform(e.DefaultValue, func);

						return e.Update(e.Target, v);
					}

				case ExpressionType.RuntimeVariables:
					{
						var e = (RuntimeVariablesExpression)expr;
						var v = Transform2(e.Variables, func);

						return e.Update(v);
					}

				case ExpressionType.Loop:
					{
						var e = (LoopExpression)expr;
						var b = Transform(e.Body, func);

						return e.Update(e.BreakLabel, e.ContinueLabel, b);
					}

				case ExpressionType.Switch:
					{
						var e = (SwitchExpression)expr;
						var s = Transform (e.SwitchValue, func);
						var c = Transform2(e.Cases,       cs => cs.Update(Transform2(cs.TestValues, func), Transform(cs.Body, func)));
						var d = Transform (e.DefaultBody, func);

						return e.Update(s, c, d);
					}

				case ExpressionType.Try:
					{
						var e = (TryExpression)expr;
						var b = Transform (e.Body,     func);
						var c = Transform2(e.Handlers, h => h.Update((ParameterExpression)Transform(h.Variable, func), Transform(h.Filter, func), Transform(h.Body, func)));
						var f = Transform (e.Finally,  func);
						var t = Transform (e.Fault,    func);

						return e.Update(b, c, f, t);
					}

				case ExpressionType.Extension:
				{
					return expr;
				}
			}

			throw new InvalidOperationException();
		}

		#endregion
	}
}
