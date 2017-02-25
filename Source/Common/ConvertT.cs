using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	using Expressions;

	[PublicAPI]
	public static class ConvertTo<TTo>
	{
		public static TTo From<TFrom>(TFrom o)
		{
			return Convert<TFrom,TTo>.From(o);
		}
	}

	[PublicAPI]
	public static class Convert<TFrom,TTo>
	{
		static Convert()
		{
			Init();
		}

		static void Init()
		{
			var expr = ConvertBuilder.GetConverter(null, typeof(TFrom), typeof(TTo));

			_expression = (Expression<Func<TFrom,TTo>>)expr.Item1;

			var rexpr = (Expression<Func<TFrom,TTo>>)expr.Item1.Transform(e => e is DefaultValueExpression ? e.Reduce() : e);

			_lambda = rexpr.Compile();
		}

		private static Expression<Func<TFrom,TTo>> _expression;
		public  static Expression<Func<TFrom,TTo>>  Expression
		{
			get { return _expression; }
			set
			{
				var setDefault = _expression != null;

				if (value == null)
				{
					Init();
				}
				else
				{
					_expression = value;
					_lambda = _expression.Compile();
				}

				if (setDefault)
					ConvertInfo.Default.Set(
						typeof(TFrom),
						typeof(TTo),
						new ConvertInfo.LambdaInfo(_expression, null, _lambda, false));
			}
		}

		private static Func<TFrom,TTo> _lambda;
		public static  Func<TFrom,TTo>  Lambda
		{
			get { return _lambda; }
			set
			{
				var setDefault = _expression != null;

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
				}

				if (setDefault)
					ConvertInfo.Default.Set(
						typeof(TFrom),
						typeof(TTo),
						new ConvertInfo.LambdaInfo(_expression, null, _lambda, false));
			}
		}

		public static Func<TFrom,TTo> From
		{
			get { return _lambda; }
		}
	}
}
