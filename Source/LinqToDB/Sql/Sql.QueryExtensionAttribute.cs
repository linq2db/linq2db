using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.SqlQuery;

namespace LinqToDB
{
	using Common;
	using Mapping;

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

			public virtual SqlQueryExtension GetExtension(ParameterInfo[] parameters, ISqlExpression[] arguments)
			{
				var ext = new SqlQueryExtension
				{
					Scope = Scope,
					ID    = ID,
				};

				for (var i = 0; i < parameters.Length; i++)
					ext.Arguments.Add(parameters[i].Name!, arguments[i]);

				return ext;
			}

			public virtual void ExtendTable(SqlTable table, ParameterInfo[] parameters, ISqlExpression[] arguments)
			{
				(table.SqlQueryExtensions ??= new List<SqlQueryExtension>()).Add(GetExtension(parameters, arguments));
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
