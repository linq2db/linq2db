using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Internal.Conversion;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Metadata
{
	sealed class AttributeInfo
	{
		public AttributeInfo(Type type, Dictionary<string,object?> values)
		{
			Type   = type;
			Values = values;
		}

		public Type                       Type;
		public Dictionary<string,object?> Values;

		Func<MappingAttribute>? _func;

		public MappingAttribute MakeAttribute()
		{
			if (_func == null)
			{
				var ctors = Type.GetConstructors();
				var ctor  = ctors.FirstOrDefault(c => c.GetParameters().Length == 0);

				if (ctor != null)
				{
					var expr = Expression.Lambda<Func<MappingAttribute>>(
						Expression.Convert(
							Expression.MemberInit(
								Expression.New(ctor),
								Values.Select(k =>
								{
									var member = Type.GetPublicMemberEx(k.Key)[0];
									var mtype  = member.GetMemberType();

									return Expression.Bind(
										member,
										Expression.Constant(Converter.ChangeType(k.Value, mtype), mtype));
								})),
							typeof(MappingAttribute)));

					_func = expr.CompileExpression();
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			return _func();
		}
	}
}
