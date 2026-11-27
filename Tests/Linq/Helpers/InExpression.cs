using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace Tests.Linq.Helpers.InExpression
{
	/// <summary>
	/// Builder for parameterized sql IN expression
	/// </summary>
	/// <seealso cref="Sql.IExtensionCallBuilder" />
	public class InExpressionItemBuilder : Sql.IExtensionCallBuilder
	{
		/// <summary>
		/// Builds the parameterized sql IN expression
		/// </summary>
		/// <param name="builder">The builder.</param>
		/// <exception cref="ArgumentNullException">Values for \"In\" operation should not be empty - values</exception>
		/// <see cref="SqlExtensions.In{T}(Sql.ISqlExtension,T,IEnumerable{T})"/>
		/// <seealso cref="SqlExtensions.In{T}(Sql.ISqlExtension,T,IEnumerable{T})"/>
		public void Build(Sql.ISqlExtensionBuilder builder)
		{
			var parameterName = (builder.Arguments[2] as MemberExpression)?.Member.Name ?? "p";

			var values = builder.GetValue<System.Collections.IEnumerable>("values")
				?.OfType<object>()
				.ToArray();

			if (values == null || values.Length == 0)
			{
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
				throw new ArgumentNullException(nameof(values), "Values for \"In\" operation should not be empty");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
			}

			foreach (var value in values)
			{
				var param = new SqlParameter(new DbDataType(value?.GetType() ?? typeof(object)), parameterName, value);
				builder.AddParameter("values", param);
			}
		}
	}

	public static class SqlExtensions
	{
		/// <summary>
		/// Sql IN expression extension
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ext">The ext.</param>
		/// <param name="field">The field.</param>
		/// <param name="values">The values.</param>
		/// <returns></returns>
		[Sql.Extension("{field} IN ({values, ', '})", IsPredicate = true, BuilderType = typeof(InExpressionItemBuilder), ServerSideOnly = true)]
		public static bool In<T>(this Sql.ISqlExtension? ext, [ExprParameter] T field, [SqlQueryDependent] IEnumerable<T> values)
		{
			return values.Contains(field);
		}

		[Sql.Extension("{field} IN ({values, ', '})", IsPredicate = true, BuilderType = typeof(InExpressionItemBuilder), ServerSideOnly = true)]
		public static bool In<T>(this Sql.ISqlExtension? ext, [ExprParameter] T field, [SqlQueryDependent] params T[] values)
		{
			return values.Contains(field);
		}
	}
}
