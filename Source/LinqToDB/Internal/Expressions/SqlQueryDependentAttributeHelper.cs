using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Internal.Expressions
{
	public static class SqlQueryDependentAttributeHelper
	{
		static readonly ConcurrentDictionary<MethodInfo,IList<SqlQueryDependentAttribute?>?> _queryDependentMethods = new ();

		public static IList<SqlQueryDependentAttribute?>? GetQueryDependentAttributes(MethodInfo method)
		{
			var mi = method.GetGenericMethodDefinitionCached();
			var dependentParameters = _queryDependentMethods.GetOrAdd(
				mi, static mi =>
				{
					var parameters = mi.GetParameters();
					if (parameters.Length == 0)
						return null;
					SqlQueryDependentAttribute?[]? attributes = null;
					for (var i = 0; i < parameters.Length; i++)
					{
						var attr = parameters[i].GetAttribute<SqlQueryDependentAttribute>();
						if (attr != null)
						{
							attributes    ??= new SqlQueryDependentAttribute[parameters.Length];
							attributes[i] =   attr;
						}
					}

					return attributes;
				});

			return dependentParameters;
		}
		
	}
}
