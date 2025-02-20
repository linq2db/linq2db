using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	// PathVisitor cannot be shared/reused due to _visited state field
	internal sealed class PathVisitor<TContext>
	{
		private readonly TContext                                 _context;
		private readonly Action<TContext, Expression, Expression> _func;
		private          Expression                               _path;
		private          HashSet<Expression>?                     _visited;

		public PathVisitor(TContext context, Expression path, Action<TContext, Expression, Expression> func)
		{
			_context = context;
			_path    = path;
			_func    = func;
			_visited = null;
		}

		private void Path<T>(IEnumerable<T> source, PropertyInfo property, Action<T> func)
			where T : class
		{
			var prop = Expression.Property(_path, property);
			var i    = 0;
			foreach (var item in source)
			{
				_path = Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, ExpressionInstances.Int32Array(i++));
				func(item);
			}
		}

		private void Path<T>(IEnumerable<T> source, PropertyInfo property)
			where T : Expression
		{
			var prop = Expression.Property(_path, property);
			var i    = 0;
			foreach (var item in source)
			{
				_path = Expression.Call(prop, ReflectionHelper.IndexExpressor<T>.Item, ExpressionInstances.Int32Array(i++));
				Path(item);
			}
		}

		private void Path(Expression? expr, PropertyInfo property)
		{
			_path = Expression.Property(_path, property);
			Path(expr);
		}

		public void Path(Expression? expr)
		{
			if (expr == null)
				return;

			Expression? path;

			switch (expr.NodeType)
			{
				case ExpressionType.Add               :
				case ExpressionType.AddChecked        :
				case ExpressionType.And               :
				case ExpressionType.AndAlso           :
				case ExpressionType.ArrayIndex        :
				case ExpressionType.Assign            :
				case ExpressionType.Coalesce          :
				case ExpressionType.Divide            :
				case ExpressionType.Equal             :
				case ExpressionType.ExclusiveOr       :
				case ExpressionType.GreaterThan       :
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift         :
				case ExpressionType.LessThan          :
				case ExpressionType.LessThanOrEqual   :
				case ExpressionType.Modulo            :
				case ExpressionType.Multiply          :
				case ExpressionType.MultiplyChecked   :
				case ExpressionType.NotEqual          :
				case ExpressionType.Or                :
				case ExpressionType.OrElse            :
				case ExpressionType.Power             :
				case ExpressionType.RightShift        :
				case ExpressionType.Subtract          :
				case ExpressionType.SubtractChecked   :
					{
						path = ConvertPathTo(typeof(BinaryExpression));

						Path(((BinaryExpression)expr).Conversion, ReflectionHelper.Binary.Conversion);
						_path = path;
						Path(((BinaryExpression)expr).Left, ReflectionHelper.Binary.Left);
						_path = path;
						Path(((BinaryExpression)expr).Right, ReflectionHelper.Binary.Right);

						break;
					}

				case ExpressionType.ArrayLength   :
				case ExpressionType.Convert       :
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate        :
				case ExpressionType.NegateChecked :
				case ExpressionType.Not           :
				case ExpressionType.Quote         :
				case ExpressionType.TypeAs        :
				case ExpressionType.UnaryPlus     :
					path = ConvertPathTo(typeof(UnaryExpression));

					Path(((UnaryExpression)expr).Operand, ReflectionHelper.Unary.Operand);
					break;

				case ExpressionType.Call:
					{
						path = ConvertPathTo(typeof(MethodCallExpression));

						Path(((MethodCallExpression)expr).Object,    ReflectionHelper.MethodCall.Object);
						_path = path;
						Path(((MethodCallExpression)expr).Arguments, ReflectionHelper.MethodCall.Arguments);

						break;
					}

				case ExpressionType.Conditional:
					{
						path = ConvertPathTo(typeof(ConditionalExpression));

						Path(((ConditionalExpression)expr).Test,    ReflectionHelper.Conditional.Test);
						_path = path;
						Path(((ConditionalExpression)expr).IfTrue,  ReflectionHelper.Conditional.IfTrue);
						_path = path;
						Path(((ConditionalExpression)expr).IfFalse, ReflectionHelper.Conditional.IfFalse);

						break;
					}

				case ExpressionType.Invoke:
					{
						path = ConvertPathTo(typeof(InvocationExpression));

						Path(((InvocationExpression)expr).Expression, ReflectionHelper.Invocation.Expression);
						_path = path;
						Path(((InvocationExpression)expr).Arguments, ReflectionHelper.Invocation.Arguments);

						break;
					}

				case ExpressionType.Lambda:
					{
						path = ConvertPathTo(typeof(LambdaExpression));

						Path(((LambdaExpression)expr).Body,       ReflectionHelper.LambdaExpr.Body);
						_path = path;
						Path(((LambdaExpression)expr).Parameters, ReflectionHelper.LambdaExpr.Parameters);

						break;
					}

				case ExpressionType.ListInit:
					{
						path = ConvertPathTo(typeof(ListInitExpression));

						Path(((ListInitExpression)expr).NewExpression, ReflectionHelper.ListInit.NewExpression);
						_path = path;
						Path(((ListInitExpression)expr).Initializers,  ReflectionHelper.ListInit.Initializers, ElementInitPath);

						break;
					}

				case ExpressionType.MemberAccess:
					path = ConvertPathTo(typeof(MemberExpression));
					Path(((MemberExpression)expr).Expression, ReflectionHelper.Member.Expression);
					break;

				case ExpressionType.MemberInit:
					{
						path = ConvertPathTo(typeof(MemberInitExpression));

						Path(((MemberInitExpression)expr).NewExpression, ReflectionHelper.MemberInit.NewExpression);
						_path = path;
						Path(((MemberInitExpression)expr).Bindings,      ReflectionHelper.MemberInit.Bindings,      MemberBindingPath);

						break;
					}

				case ExpressionType.New:
					path = ConvertPathTo(typeof(NewExpression));
					Path(((NewExpression)expr).Arguments, ReflectionHelper.New.Arguments);
					break;

				case ExpressionType.NewArrayBounds:
					path = ConvertPathTo(typeof(NewArrayExpression));
					Path(((NewArrayExpression)expr).Expressions, ReflectionHelper.NewArray.Expressions);
					break;

				case ExpressionType.NewArrayInit:
					path = ConvertPathTo(typeof(NewArrayExpression));
					Path(((NewArrayExpression)expr).Expressions, ReflectionHelper.NewArray.Expressions);
					break;

				case ExpressionType.TypeIs:
					path = ConvertPathTo(typeof(TypeBinaryExpression));
					Path(((TypeBinaryExpression)expr).Expression, ReflectionHelper.TypeBinary.Expression);
					break;

				case ExpressionType.Block:
					{
						path = ConvertPathTo(typeof(BlockExpression));

						Path(((BlockExpression)expr).Expressions, ReflectionHelper.Block.Expressions);
						_path = path;
						Path(((BlockExpression)expr).Variables,   ReflectionHelper.Block.Variables); // ?

						break;
					}

				case ExpressionType.Constant:
					{
						path = ConvertPathTo(typeof(ConstantExpression));

						if (((ConstantExpression)expr).Value is IQueryable iq && _visited?.Contains(iq.Expression) != true)
						{
							(_visited ??= new ()).Add(iq.Expression);

							_path = Expression.Property(_path, ReflectionHelper.Constant.Value);
							ConvertPathTo(typeof(IQueryable));
							Path(iq.Expression, ReflectionHelper.QueryableInt.Expression);
						}

						break;
					}

				case ExpressionType.Parameter: path = ConvertPathTo(typeof(ParameterExpression)); break;
				case ExpressionType.Default  : path = ConvertPathTo(typeof(DefaultExpression  )); break;

				case ExpressionType.Extension:
					{
						path = _path;

						if (expr is SqlGenericConstructorExpression generic)
						{
							path = ConvertPathTo(typeof(SqlGenericConstructorExpression));
							Path(generic.Assignments, ReflectionHelper.SqlGenericConstructor.Assignments, AssignmentsPath);
						}
						else
						{
							if (expr.CanReduce)
								Path(expr.Reduce());
						}

						break;
					}

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr.NodeType}");
			}

			_func(_context, expr, path);
		}

		private void ElementInitPath(ElementInit ei)
		{
			Path(ei.Arguments, ReflectionHelper.ElementInit.Arguments);
		}

		private void MemberBindingPath(MemberBinding b)
		{
			switch (b.BindingType)
			{
				case MemberBindingType.Assignment:
					ConvertPathTo(typeof(MemberAssignment));
					Path(
						((MemberAssignment)b).Expression,
						ReflectionHelper.MemberAssignmentBind.Expression);
					break;

				case MemberBindingType.ListBinding:
					ConvertPathTo(typeof(MemberListBinding));
					Path(
						((MemberListBinding)b).Initializers,
						ReflectionHelper.MemberListBind.Initializers,
						ElementInitPath);
					break;

				case MemberBindingType.MemberBinding:
					ConvertPathTo(typeof(MemberMemberBinding));
					Path(
						((MemberMemberBinding)b).Bindings,
						ReflectionHelper.MemberMemberBind.Bindings,
						MemberBindingPath);
					break;
			}
		}

		private void AssignmentsPath(SqlGenericConstructorExpression.Assignment assignment)
		{
			ConvertPathTo(typeof(SqlGenericConstructorExpression.Assignment));
			Path(assignment.Expression, ReflectionHelper.SqlGenericConstructorAssignment.Expression);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Expression ConvertPathTo(Type type)
		{
			return _path = _path.Type != type ? Expression.Convert(_path, type) : _path;
		}
	}
}
