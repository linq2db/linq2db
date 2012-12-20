using System;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	using Expressions;

	public static class ConvertTo<TTo>
	{
		public static TTo From<TFrom>(TFrom o)
		{
			return Convert<TFrom,TTo>.From(o);
		}
	}

	public static class Convert<TFrom,TTo>
	{
		static Convert()
		{
			Init();
		}

		static void Init()
		{
			var expr = ConverterMaker.GetConverter(null, typeof(TFrom), typeof(TTo));

			_expression = (Expression<Func<TFrom,TTo>>)expr.Item1;

			var rexpr = (Expression<Func<TFrom,TTo>>)expr.Item1.Transform(e => e is DefaultValueExpression ? e.Reduce() : e);

			_lambda = rexpr.Compile();
		}

		static private Expression<Func<TFrom,TTo>> _expression;
		static public  Expression<Func<TFrom,TTo>>  Expression
		{
			get { return _expression; }
			set
			{
				if (value == null)
				{
					Init();
				}
				else
				{
					_expression = value;
					_lambda     = _expression.Compile();

					ConvertInfo.Default.Set(
						typeof(TFrom),
						typeof(TTo),
						new ConvertInfo.LambdaInfo(_expression, null, _lambda, false));
				}
			}
		}

		static private Func<TFrom,TTo> _lambda;
		static public  Func<TFrom,TTo>  Lambda
		{
			get { return _lambda; }
			set
			{
				if (value == null)
				{
					Init();
				}
				else
				{
					var p = System.Linq.Expressions.Expression.Parameter(typeof(TFrom), "p");

					_lambda     = value;
					_expression =
						System.Linq.Expressions.Expression.Lambda<Func<TFrom,TTo>>(
							System.Linq.Expressions.Expression.Invoke(
								System.Linq.Expressions.Expression.Constant(value),
								p),
							p);

					ConvertInfo.Default.Set(
						typeof(TFrom),
						typeof(TTo),
						new ConvertInfo.LambdaInfo(_expression, null, _lambda, false));
				}
			}
		}

		public static Func<TFrom,TTo> From
		{
			get { return _lambda; }
		}
	}
}
