using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions.Types;

namespace LinqToDB.Expressions
{
	public class ExpressionGenerator
	{
		public static TypeMapper NoOpTypeMapper = new TypeMapper();

		List<ParameterExpression>? _variables;
		List<Expression>?          _expressions;
		readonly TypeMapper        _mapper;

		public ExpressionGenerator(TypeMapper mapper)
		{
			_mapper = mapper;
		}

		public ExpressionGenerator() : this(NoOpTypeMapper)
		{
		}

		public Expression ResultExpression => Build();

		public ParameterExpression DeclareVariable(Type type, string? name = default)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var variable = Expression.Variable(type, name);

			_variables ??= new();
			_variables.Add(variable);
			return variable;
		}

		public ParameterExpression AddVariable(ParameterExpression variable)
		{
			if (variable == null) throw new ArgumentNullException(nameof(variable));

			_variables ??= new();
			_variables.Add(variable);
			return variable;
		}

		public Expression AddExpression(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			_expressions ??= new();
			_expressions.Add(expression);
			return expression;
		}

		public Expression Build()
		{
			if ((_variables?.Count ?? 0) == 0 && _expressions?.Count == 1)
				return _expressions[0];

			var block = Expression.Block(_variables, _expressions ?? Enumerable.Empty<Expression>());
			return block;
		}

		public static Expression Build(Action<ExpressionGenerator> buildFunc, TypeMapper? typeMapper = default)
		{
			if (buildFunc == null) throw new ArgumentNullException(nameof(buildFunc));

			var generator = new ExpressionGenerator(typeMapper ?? new TypeMapper());
			buildFunc(generator);
			return generator.Build();
		}

		public Expression Assign(Expression left, Expression right)
		{
			if (left  == null) throw new ArgumentNullException(nameof(left));
			if (right == null) throw new ArgumentNullException(nameof(right));

			if (left.Type != right.Type)
				right = Expression.Convert(right, left.Type);

			return AddExpression(Expression.Assign(left, right));
		}

		public ParameterExpression AssignToVariable(Expression expression, string? name = default)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var variable = DeclareVariable(expression.Type, name);
			Assign(variable, expression);
			return variable;
		}

		public Expression Throw(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return AddExpression(Expression.Throw(expression));
		}

		public Expression IfThen(Expression test, Expression ifTrue)
		{
			if (test   == null) throw new ArgumentNullException(nameof(test));
			if (ifTrue == null) throw new ArgumentNullException(nameof(ifTrue));

			return AddExpression(Expression.IfThen(test, ifTrue));
		}

		public Expression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse)
		{
			if (test    == null) throw new ArgumentNullException(nameof(test));
			if (ifTrue  == null) throw new ArgumentNullException(nameof(ifTrue));
			if (ifFalse == null) throw new ArgumentNullException(nameof(ifFalse));

			return AddExpression(Expression.IfThenElse(test, ifTrue, ifFalse));
		}

		public Expression Condition(Expression test, Expression ifTrue, Expression ifFalse)
		{
			if (test    == null) throw new ArgumentNullException(nameof(test));
			if (ifTrue  == null) throw new ArgumentNullException(nameof(ifTrue));
			if (ifFalse == null) throw new ArgumentNullException(nameof(ifFalse));

			return AddExpression(Expression.Condition(test, ifTrue, ifFalse));
		}

		public Expression TryCatch(Expression body, params CatchBlock[] catchBlocks)
		{
			if (body        == null) throw new ArgumentNullException(nameof(body));
			if (catchBlocks == null) throw new ArgumentNullException(nameof(catchBlocks));

			return AddExpression(Expression.TryCatch(body, catchBlocks));
		}

		public MemberExpression MemberAccess<T>(Expression<Func<T, object>> memberExpression,
			Expression obj)
		{
			if (memberExpression == null) throw new ArgumentNullException(nameof(memberExpression));
			if (obj              == null) throw new ArgumentNullException(nameof(obj));

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

		#region MapAction
		public Expression MapAction(Expression<Action> action)
			=> _mapper.MapAction(action);

		public Expression MapAction<T>(Expression<Action<T>> action, Expression p)
			=> _mapper.MapAction(action, p);

		public Expression MapAction<T1, T2>(Expression<Action<T1, T2>> action, Expression p1, Expression p2)
			=> _mapper.MapAction(action, p1, p2);

		public Expression MapAction<T1, T2, T3>(Expression<Action<T1, T2, T3>> action, Expression p1, Expression p2, Expression p3)
			=> _mapper.MapAction(action, p1, p2, p3);

		public Expression MapAction<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action, Expression p1, Expression p2, Expression p3, Expression p4)
			=> _mapper.MapAction(action, p1, p2, p3, p4);

		public Expression MapAction<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5)
			=> _mapper.MapAction(action, p1, p2, p3, p4, p5);
		#endregion

	}
}
