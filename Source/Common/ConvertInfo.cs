using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	class ConvertInfo
	{
		public static ConvertInfo Default = new ConvertInfo();

		public class LambdaInfo
		{
			public LambdaInfo(LambdaExpression lambda, Delegate @delegate)
			{
				Lambda   = lambda;
				Delegate = @delegate;
			}

			public LambdaExpression Lambda;
			public Delegate         Delegate;
		}

		readonly Dictionary<Type,Dictionary<Type,LambdaInfo>> _expressions = new Dictionary<Type,Dictionary<Type,LambdaInfo>>();

		public void Set(Type from, Type to, LambdaInfo expr)
		{
			Dictionary<Type,LambdaInfo> dic;

			if (_expressions.TryGetValue(from, out dic))
				dic[to] = expr;
			else
				_expressions[from] = new Dictionary<Type,LambdaInfo> { { to, expr } };
		}

		public LambdaInfo Get(Type from, Type to, bool create = true)
		{
			Dictionary<Type,LambdaInfo> dic;

			if (_expressions.TryGetValue(from, out dic))
			{
				LambdaInfo li;
				dic.TryGetValue(to, out li);
				return li;
			}

			if (!create)
				return null;

			var ex  = ConverterMaker.GetConverter(from, to);
			var lm  = ex.Compile();
			var ret = new LambdaInfo(ex, lm);

			Set(from, to , ret);

			return ret;
		}
	}
}
