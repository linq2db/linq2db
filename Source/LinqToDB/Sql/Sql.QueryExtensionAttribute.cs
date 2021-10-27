using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB
{
	using Common;
	using Mapping;
	using SqlQuery;

	public partial class Sql
	{
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : Attribute
		{
			public QueryExtensionAttribute(QueryExtensionScope scope, int id = 0)
			{
				Scope = scope;
				ID    = id;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, int id = 0)
			{
				Configuration = configuration;
				Scope         = scope;
				ID            = id;
			}

			public string?             Configuration { get; }
			public QueryExtensionScope Scope         { get; }
			public int                 ID            { get; }

			public virtual SqlQueryExtension GetExtension(Dictionary<string,ISqlExpression> parameters)
			{
				var ext = new SqlQueryExtension
				{
					Scope = Scope,
					ID    = ID,
				};

				foreach (var item in parameters)
					ext.Arguments.Add(item.Key, item.Value);

				return ext;
			}

			public virtual void ExtendTable(SqlTable table, Dictionary<string,ISqlExpression> parameters)
			{
				(table.SqlQueryExtensions ??= new()).Add(GetExtension(parameters));
			}

			public virtual void ExtendJoin(List<SqlQueryExtension> extensions, Dictionary<string,ISqlExpression> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public virtual void ExtendQuery(List<SqlQueryExtension> extensions, Dictionary<string,ISqlExpression> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

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
