using System.Linq.Expressions;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Common;
	using Mapping;
	using SqlQuery;

	partial class Sql
	{
		/// <summary>
		/// An attribute used to define a static value or
		/// a Database side property/method that takes no parameters.
		/// </summary>
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class PropertyAttribute : ExpressionAttribute
		{
			/// <summary>
			/// Creates a property to be used in SQL
			/// The name of the Property/Method will be used.
			/// </summary>
			public PropertyAttribute()
				: base(null)
			{
			}

			/// <summary>
			/// Creates a Property to be used in SQL.
			/// </summary>
			/// <param name="name">The name of the property.</param>
			public PropertyAttribute(string name)
				: base(name)
			{
			}

			/// <summary>
			/// Creates a property to be used in SQL
			/// for the given <see cref="ProviderName"/>.
			/// </summary>
			/// <param name="configuration">The <see cref="ProviderName"/>
			/// the property will be used under.</param>
			/// <param name="name">The name of the property.</param>
			public PropertyAttribute(string configuration, string name)
				: base(configuration, name)
			{
			}

			/// <summary>
			/// The name of the Property.
			/// </summary>
			public string? Name
			{
				get => Expression;
				set => Expression = value;
			}

			public override ISqlExpression? GetExpression<TContext>(TContext context, IDataContext dataContext, SelectQuery query, Expression expression, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var name = Name;

				if (name == null)
				{
					if (expression is MethodCallExpression mc)
						name = mc.Method.Name;
					else if (expression is MemberExpression me)
						name = me.Member.Name;
				}

				if (string.IsNullOrEmpty(name))
					ThrowHelper.ThrowLinqToDBException($"Cannot retrieve property name for expression '{expression}'.");

				return new SqlExpression(expression.Type, name!, SqlQuery.Precedence.Primary)
				{
					CanBeNull = GetCanBeNull(Array<ISqlExpression>.Empty)
				};
			}
		}
	}
}
