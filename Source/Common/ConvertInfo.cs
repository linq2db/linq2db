using System;
using System.Collections.Concurrent;
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

		readonly ConcurrentDictionary<Type,ConcurrentDictionary<Type,LambdaInfo>> _expressions =
			new ConcurrentDictionary<Type,ConcurrentDictionary<Type,LambdaInfo>>();

		public void Set(Type from, Type to, LambdaInfo expr)
		{
			ConcurrentDictionary<Type,LambdaInfo> dic;

			if (!_expressions.TryGetValue(from, out dic))
				_expressions[from] = dic = new ConcurrentDictionary<Type, LambdaInfo>();

			dic[to] = expr;
		}

		public LambdaInfo Get(Type from, Type to, bool create = true)
		{
			ConcurrentDictionary<Type,LambdaInfo> dic;
			LambdaInfo li;

			if (_expressions.TryGetValue(from, out dic) && dic.TryGetValue(to, out li))
				return li;

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
