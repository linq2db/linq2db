using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	using Mapping;

	class ConvertInfo
	{
		public static ConvertInfo Default = new ConvertInfo();

		public class LambdaInfo
		{
			public LambdaInfo(LambdaExpression lambda, Delegate @delegate, bool isSchemaSpecific)
			{
				Lambda           = lambda;
				Delegate         = @delegate;
				IsSchemaSpecific = isSchemaSpecific;
			}

			public LambdaExpression Lambda;
			public Delegate         Delegate;
			public bool             IsSchemaSpecific;
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
			var ex  = ConverterMaker.GetConverter(mappingSchema, from, to);
			var lm  = ex.Item1.Compile();
			var ret = new LambdaInfo(ex.Item1, lm, ex.Item2);

			Set(_expressions, from, to , ret);

			return ret;
		}
	}
}
