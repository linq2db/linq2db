using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB
{
	using Common;
	using Mapping;

	public partial class Sql
	{
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : Attribute
		{
			public QueryExtensionAttribute(QueryExtensionScope scope)
			{
				Scope = scope;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope)
			{
				Configuration = configuration;
				Scope         = scope;
			}

			public string?             Configuration { get; }
			public QueryExtensionScope Scope         { get; }

			public static QueryExtensionAttribute[] GetExtensionAttributes(Expression expression, MappingSchema mapping)
			{
				MemberInfo memberInfo;

				switch (expression.NodeType)
				{
					case ExpressionType.MemberAccess : memberInfo = ((MemberExpression)    expression).Member; break;
					case ExpressionType.Call         : memberInfo = ((MethodCallExpression)expression).Method; break;
					default                          : return Array<QueryExtensionAttribute>.Empty;
				}

				return mapping.GetAttributes<QueryExtensionAttribute>(memberInfo.ReflectedType!, memberInfo, a => a.Configuration, inherit: true, exactForConfiguration: true);
			}
		}
	}
}
