﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using LinqToDB.Data;

namespace LinqToDB.Common
{
	using Mapping;

	class ConvertInfo
	{
		public static ConvertInfo Default = new ConvertInfo();

		public class LambdaInfo
		{
			public LambdaInfo(
				LambdaExpression checkNullLambda,
				LambdaExpression lambda,
				Delegate         @delegate,
				bool             isSchemaSpecific)
			{
				CheckNullLambda  = checkNullLambda;
				Lambda           = lambda ?? checkNullLambda;
				Delegate         = @delegate;
				IsSchemaSpecific = isSchemaSpecific;
			}

			public LambdaExpression Lambda;
			public LambdaExpression CheckNullLambda;
			public Delegate         Delegate;
			public bool             IsSchemaSpecific;

			private Func<object, DataParameter> _convertValueToParameter = null;
			public Func<object, DataParameter> ConvertValueToParameter
			{
				get
				{
					if (_convertValueToParameter == null)
					{
						var type = this.Lambda.Parameters[0].Type;
						var parameterExpression = Expression.Parameter(typeof(object));
						var lambdaExpression = Expression.Lambda<Func<object, DataParameter>>(
							Expression.Invoke(this.Lambda, Expression.Convert(parameterExpression, type)), parameterExpression);
						var convertFunc = lambdaExpression.Compile();
						_convertValueToParameter = convertFunc;
					}

					return _convertValueToParameter;
				}
			}
		}

		readonly ConcurrentDictionary<Type,ConcurrentDictionary<Type,LambdaInfo>> _expressions =
			new ConcurrentDictionary<Type,ConcurrentDictionary<Type,LambdaInfo>>();

		public void Set(Type from, Type to, LambdaInfo expr)
		{
			Set(_expressions, from, to, expr);
		}

		static void Set(ConcurrentDictionary<Type,ConcurrentDictionary<Type,LambdaInfo>> expressions, Type from, Type to, LambdaInfo expr)
		{
			ConcurrentDictionary<Type,LambdaInfo> dic;

			if (!expressions.TryGetValue(from, out dic))
				expressions[from] = dic = new ConcurrentDictionary<Type, LambdaInfo>();

			dic[to] = expr;
		}

		public LambdaInfo Get(Type from, Type to)
		{
			ConcurrentDictionary<Type,LambdaInfo> dic;
			LambdaInfo li;

			return _expressions.TryGetValue(@from, out dic) && dic.TryGetValue(to, out li) ? li : null;
		}

		public LambdaInfo Create(MappingSchema mappingSchema, Type from, Type to)
		{
			var ex  = ConvertBuilder.GetConverter(mappingSchema, from, to);
			var lm  = ex.Item1.Compile();
			var ret = new LambdaInfo(ex.Item1, ex.Item2, lm, ex.Item3);

			Set(_expressions, from, to , ret);

			return ret;
		}
	}
}
