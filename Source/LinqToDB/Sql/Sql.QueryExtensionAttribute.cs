using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB;

using Common;
using Linq.Builder;
using Mapping;
using SqlQuery;

public partial class Sql
{
	/// <summary>
	/// Defines custom query extension builder.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
	public class QueryExtensionAttribute : Attribute
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

		public string?             Configuration        { get; }
		public QueryExtensionScope Scope                { get; }
		/// <summary>
		/// Instance of <see cref="ISqlExtensionBuilder"/>.
		/// </summary>
		public Type?               ExtensionBuilderType { get; set; }
		public string[]?           ExtensionArguments   { get; set; }

		public virtual SqlQueryExtension GetExtension(List<SqlQueryExtensionData> parameters)
		{
			var ext = new SqlQueryExtension
			{
				Configuration = Configuration,
				Scope         = Scope,
				BuilderType   = ExtensionBuilderType,
			};

			foreach (var item in parameters)
				ext.Arguments.Add(item.Name, item.SqlExpression!);

			if (ExtensionArguments is not null)
			{
				ext.Arguments.Add(".ExtensionArguments.Count",  new SqlValue(ExtensionArguments.Length));

				for (var i = 0; i < ExtensionArguments.Length; i++)
					ext.Arguments.Add($".ExtensionArguments.{i}", new SqlValue(ExtensionArguments[i]));
			}

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
				default                          : return Array<QueryExtensionAttribute>.Empty;
			}

			return mapping.GetAttributes<QueryExtensionAttribute>(memberInfo.ReflectedType!, memberInfo, a => a.Configuration, inherit: true, exactForConfiguration: true);
		}
	}
}
