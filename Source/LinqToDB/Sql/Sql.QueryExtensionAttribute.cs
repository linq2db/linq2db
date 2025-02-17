using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common.Internal;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;

namespace LinqToDB
{
	public partial class Sql
	{
		/// <summary>
		/// Defines custom query extension builder.
		/// </summary>
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class QueryExtensionAttribute : MappingAttribute
		{
			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}

			public QueryExtensionAttribute(QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, params string[] extensionArguments)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = extensionArguments;
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument };
			}

			public QueryExtensionAttribute(string? configuration, QueryExtensionScope scope, Type extensionBuilderType, string extensionArgument0, string extensionArgument1)
			{
				Configuration        = configuration;
				Scope                = scope;
				ExtensionBuilderType = extensionBuilderType;
				ExtensionArguments   = new [] { extensionArgument0, extensionArgument1 };
			}

			public QueryExtensionScope Scope                { get; }
			/// <summary>
			/// Instance of <see cref="ISqlExtensionBuilder"/>.
			/// </summary>
			public Type?               ExtensionBuilderType { get; set; }
			public string[]?           ExtensionArguments   { get; set; }

			public virtual SqlQueryExtension GetExtension(List<SqlQueryExtensionData> parameters)
			{
				var arguments = new Dictionary<string,ISqlExpression>();

				foreach (var item in parameters)
					arguments.Add(item.Name, item.SqlExpression!);

				if (ExtensionArguments is not null)
				{
					arguments.Add(".ExtensionArguments.Count",  new SqlValue(ExtensionArguments.Length));

					for (var i = 0; i < ExtensionArguments.Length; i++)
						arguments.Add(FormattableString.Invariant($".ExtensionArguments.{i}"), new SqlValue(ExtensionArguments[i]));
				}

				return new SqlQueryExtension()
				{
					Configuration = Configuration,
					Scope         = Scope,
					BuilderType   = ExtensionBuilderType,
					Arguments     = arguments
				};
			}

			public virtual void ExtendTable(SqlTable table, List<SqlQueryExtensionData> parameters)
			{
				(table.SqlQueryExtensions ??= new()).Add(GetExtension(parameters));
			}

			public virtual void ExtendJoin(List<SqlQueryExtension> extensions, List<SqlQueryExtensionData> parameters)
			{
				extensions.Add(GetExtension(parameters));
			}

			public virtual void ExtendSubQuery(List<SqlQueryExtension> extensions, List<SqlQueryExtensionData> parameters)
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
					default                          : return [];
				}

				return mapping.GetAttributes<QueryExtensionAttribute>(memberInfo.ReflectedType!, memberInfo, forFirstConfiguration: true);
			}

			public override string GetObjectID()
			{
				return FormattableString.Invariant($".{Configuration}.{(int)Scope}.{IdentifierBuilder.GetObjectID(ExtensionBuilderType)}.{IdentifierBuilder.GetObjectID(ExtensionArguments)}.");
			}
		}
	}
}
