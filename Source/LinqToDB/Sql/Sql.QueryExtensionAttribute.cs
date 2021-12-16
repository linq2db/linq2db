using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB
{
	using Common;
	using Linq.Builder;
	using Mapping;
	using SqlQuery;

	public partial class Sql
	{
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : Attribute
		{
			public QueryExtensionAttribute(QueryExtensionScope scope, int id = QueryExtensionID.None)
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

			public string?             Configuration        { get; }
			public QueryExtensionScope Scope                { get; }
			public int                 ID                   { get; }
			/// <summary>
			/// Instance of <see cref="ISqlExtensionBuilder"/>.
			/// </summary>
			public Type?               ExtensionBuilderType { get; set; }

			public virtual SqlQueryExtension GetExtension(List<SqlQueryExtensionData> parameters)
			{
				var ext = new SqlQueryExtension
				{
					Configuration = Configuration,
					Scope         = Scope,
					ID            = ID,
					BuilderType   = ExtensionBuilderType,
				};

				foreach (var item in parameters)
					ext.Arguments.Add(item.Name, item.SqlExpression!);

				return ext;
			}

			public virtual void ExtendTable(SqlTable table, List<SqlQueryExtensionData> parameters)
			{
				(table.SqlQueryExtensions ??= new()).Add(GetExtension(parameters));
			}

			public virtual void ExtendJoin(List<SqlQueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public virtual void ExtendQuery(List<SqlQueryExtension> extensions, List<SqlQueryExtensionData> parameters)
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
