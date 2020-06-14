using System;
using System.Reflection;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
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

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlExpression(member.GetMemberType(), Name ?? member.Name, SqlQuery.Precedence.Primary)
				{
					CanBeNull = GetCanBeNull(new ISqlExpression[0])
				};
			}
		}
	}
}
