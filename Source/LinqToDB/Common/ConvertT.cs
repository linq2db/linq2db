﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	using Expressions;

	/// <summary>
	/// Converters provider for value conversion from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> type.
	/// </summary>
	/// <typeparam name="TFrom">Source conversion type.</typeparam>
	/// <typeparam name="TTo">Target conversion type.</typeparam>
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

		private static Expression<Func<TFrom,TTo>>? _expression;
		/// <summary>
		/// Gets or sets an expression that converts a value of <i>TFrom</i> type to <i>TTo</i> type.
		/// Setter updates both expression and delegate forms of converter.
		/// Assigning <c>null</c> value will reset converter to default conversion logic.
		/// Assigning non-null value will also set converter as default converter.
		/// </summary>
		[AllowNull]
		public  static Expression<Func<TFrom,TTo>>  Expression
		{
			get => _expression!;
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
						new ConvertInfo.LambdaInfo(_expression!, null, _lambda!, false));
			}
		}

		private static Func<TFrom,TTo>? _lambda;
		/// <summary>
		/// Gets or sets a function that converts a value of <i>TFrom</i> type to <i>TTo</i> type.
		/// Setter updates both expression and delegate forms of converter.
		/// Assigning <c>null</c> value will reset converter to default conversion logic.
		/// Assigning non-null value will also set converter as default converter.
		/// </summary>
		[AllowNull]
		public static  Func<TFrom,TTo>  Lambda
		{
			get => _lambda!;
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
						new ConvertInfo.LambdaInfo(_expression!, null, _lambda!, false));
			}
		}

		/// <summary>
		/// Gets conversion function delegate.
		/// </summary>
		public static Func<TFrom,TTo> From => _lambda!;
	}
}
