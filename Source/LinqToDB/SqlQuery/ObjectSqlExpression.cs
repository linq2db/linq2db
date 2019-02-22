using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Expressions;
	using LinqToDB.Extensions;
	using Linq.Builder;
	using Mapping;
	using Reflection;

	public class ObjectSqlExpression : SqlExpression
	{
		readonly Dictionary<int, Func<object, object>> _getters = new Dictionary<int,Func<object,object>>();
		readonly MappingSchema                         _mappingSchema;
		readonly SqlInfo[]                             _parameters;

		public ObjectSqlExpression(MappingSchema mappingSchema, params SqlInfo[] parameters)
			: base(null, "", SqlQuery.Precedence.Unknown, parameters.Select(_ => _.Sql).ToArray())
		{
			_mappingSchema = mappingSchema;
			_parameters    = parameters;
		}

		public object GetValue(object obj, int index)
		{
			var p  = _parameters[index];
			var mi = p.MemberChain[p.MemberChain.Count - 1];

			if (!_getters.TryGetValue(index, out var getter))
			{
				var ta        = TypeAccessor.GetAccessor(mi.DeclaringType);
				var valueType = mi.GetMemberType();
				getter        = ta[mi.Name].Getter;

				if (valueType.ToNullableUnderlying().IsEnumEx())
				{
					var toType           = Converter.GetDefaultMappingFromEnumType(_mappingSchema, valueType);
					var convExpr         = _mappingSchema.GetConvertExpression(valueType, toType);
					var convParam        = Expression.Parameter(typeof(object));
					var getterExpression = Expression.Constant(getter);
					var callGetter       = Expression.Invoke(getterExpression, convParam);


					var lex = Expression.Lambda<Func<object, object>>(
						Expression.Convert(convExpr.GetBody(Expression.Convert(callGetter, valueType)), typeof(object)),
						convParam);

					getter = lex.Compile();
				}

				_getters.Add(index, getter);
			}

			return getter(obj);
		}
	}
}
