using System;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace Tests.Linq.Helpers.JsonObjectSet
{
	internal sealed class DbObjectSetExtensionCallBuilder : Sql.IExtensionCallBuilder
	{
		public void Build(Sql.ISqlExtensionBuilder builder)
		{
			builder.Expression = "JSON_MODIFY({source}, {path}, {value})";

			builder.AddParameter("source", builder.GetExpression(0)!);

			var member = (MemberExpression) ((LambdaExpression) ((UnaryExpression) builder.Arguments[1]).Operand).Body;

			builder.AddParameter("path", $"$.{member.Member.Name}");

			var value = builder.EvaluateExpression(builder.Arguments[2]);

			builder.AddParameter("value", new SqlValue(builder.Arguments[2].Type, value));
		}
	}

	public static class DbObjectExtensions
	{
		[Sql.Extension("Set", ServerSideOnly = true, BuilderType = typeof(DbObjectSetExtensionCallBuilder))]
		public static JsonDbObject<T> Set<T, TValue>(this JsonDbObject<T> dbObject, Expression<Func<T, TValue>> propertyExpression, [SqlQueryDependent] TValue propertyValue)
			where T : class
		{
			return dbObject;
		}
	}

	public class JsonDbObject<T>
		where T : class
	{
		public JsonDbObject(T value)
		{
			Value = value;
		}

		public T Value { get; }
	}
}
