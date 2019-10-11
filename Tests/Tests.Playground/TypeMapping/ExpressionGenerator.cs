using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Expressions;

namespace Tests.Playground.TypeMapping
{
	public class ExpressionGenerator
	{
		private readonly List<ParameterExpression> _variables = new List<ParameterExpression>();
		private readonly List<Expression> _expressions = new List<Expression>();
		private readonly TypeMapper _mapper;

		public ExpressionGenerator([NotNull] TypeMapper mapper)
		{
			_mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}

		public ExpressionGenerator() : this(new TypeMapper())
		{
		}

		public Expression ResultExpression => Build();

		public ParameterExpression DeclareVariable(Type type, string name = default)
		{
			var variable = Expression.Variable(type, name);
			_variables.Add(variable);
			return variable;
		}

		public Expression AddExpression(Expression expression)
		{
			_expressions.Add(expression);
			return expression;
		}

		public Expression Build()
		{
			if (_variables.Count == 0 && _expressions.Count == 1)
				return _expressions[0];

			var block = Expression.Block(_variables, _expressions);
			return block;
		}

		public Expression Assign(Expression left, Expression right)
		{
			if (left.Type != right.Type)
				right = Expression.Convert(right, left.Type);
			return AddExpression(Expression.Assign(left, right));
		}

		public ParameterExpression AssignToVariable(Expression expression, string name = default)
		{
			var variable = DeclareVariable(expression.Type, name);
			Assign(variable, expression);
			return variable;
		}

		public Expression Throw(Expression expression)
		{
			return AddExpression(Expression.Throw(expression));
		}

		public Expression IfThen(Expression test, Expression ifTrue)
		{
			return AddExpression(Expression.IfThen(test, ifTrue));
		}

		public Expression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse)
		{
			return AddExpression(Expression.IfThenElse(test, ifTrue, ifFalse));
		}

		public Expression TryCatch(Expression body, CatchBlock[] catchBlocks)
		{
			return AddExpression(Expression.TryCatch(body, catchBlocks));
		}

		public MemberExpression MemberAccess<T>(Expression<Func<T, object>> memberExpression, Expression obj)
		{
			var expr = _mapper.MapExpression(memberExpression, obj).Unwrap();
			return (MemberExpression)expr;
		}

		#region MapExpression

		public Expression MapExpression<TR>(Expression<Func<TR>> func)
			=> _mapper.MapExpression(func);

		public Expression MapExpression<T, TR>(Expression<Func<T, TR>> func, Expression p)
			=> _mapper.MapExpression(func, p);

		public Expression MapExpression<T1, T2, TR>(Expression<Func<T1, T2, TR>> func, Expression p1, Expression p2) 
			=> _mapper.MapExpression(func, p1, p2);

		public Expression MapExpression<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func, Expression p1, Expression p2, Expression p3) 
			=> _mapper.MapExpression(func, p1, p2, p3);

		public Expression MapExpression<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func, Expression p1, Expression p2, Expression p3, Expression p4) 
			=> _mapper.MapExpression(func, p1, p2, p3, p4);

		public Expression MapExpression<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5) 
			=> _mapper.MapExpression(func, p1, p2, p3, p4, p5);

		#endregion


	}
}
