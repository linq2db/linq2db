using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Mapping;
	using Extensions;
	using SqlQuery;

	
	partial class Sql
	{
		/// <summary>
		/// An Attribute that allows custom Expressions to be defined
		/// for a Method used within a Linq Expression. 
		/// </summary>
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class ExpressionAttribute : Attribute
		{
			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// in place of the method call decorated by this attribute. 
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			public ExpressionAttribute(string? expression)
			{
				Expression = expression;
				Precedence = SqlQuery.Precedence.Primary;
				IsPure     = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public ExpressionAttribute(string expression, params int[] argIndices)
			{
				Expression = expression;
				ArgIndices = argIndices;
				Precedence = SqlQuery.Precedence.Primary;
				IsPure     = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// for the <see cref="ProviderName"/> specified,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			public ExpressionAttribute(string configuration, string expression)
			{
				Configuration = configuration;
				Expression    = expression;
				Precedence    = SqlQuery.Precedence.Primary;
				IsPure        = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// for the <see cref="ProviderName"/> specified,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public ExpressionAttribute(string configuration, string expression, params int[] argIndices)
			{
				Configuration = configuration;
				Expression    = expression;
				ArgIndices    = argIndices;
				Precedence    = SqlQuery.Precedence.Primary;
				IsPure        = true;
			}

			/// <summary>
			/// The expression to be used in building the SQL.
			/// </summary>
			public string?        Expression       { get; set; }
			/// <summary>
			/// The order of Arguments to be passed
			/// into the function from the method call.
			/// </summary>
			public int[]?         ArgIndices       { get; set; }
			/// <summary>
			/// Determines the priority of the expression in evaluation.
			/// Refer to <see cref="LinqToDB.SqlQuery.Precedence"/>.
			/// </summary>
			public int            Precedence       { get; set; }
			/// <summary>
			/// If <c>null</c>, this will be treated as the default
			/// evaluation for the expression. If set to a <see cref="ProviderName"/>,
			/// It will only be used for that provider configuration.
			/// </summary>
			public string?        Configuration    { get; set; }
			/// <summary>
			/// If <c>true</c> The expression will only be evaluated on the
			/// database server. If it cannot, an exception will
			/// be thrown. 
			/// </summary>
			public bool           ServerSideOnly   { get; set; }
			/// <summary>
			/// If <c>true</c> a greater effort will be made to execute
			/// the expression on the DB server instead of in .NET.
			/// </summary>
			public bool           PreferServerSide { get; set; }
			/// <summary>
			/// If <c>true</c> inline all parameters passed into the expression.
			/// </summary>
			public bool           InlineParameters { get; set; }
			/// <summary>
			/// Used internally by <see cref="ExtensionAttribute"/>.
			/// </summary>
			public bool           ExpectExpression { get; set; }
			/// <summary>
			/// If <c>true</c> the expression is treated as a Predicate
			/// And when used in a Where clause will not have
			/// an added comparison to 'true' in the database.
			/// </summary>
			public bool           IsPredicate      { get; set; }
			/// <summary>
			/// If <c>true</c>, this expression represents an aggregate result
			/// Examples would be SUM(),COUNT().
			/// </summary>
			public bool           IsAggregate      { get; set; }
			/// <summary>
			/// If <c>true</c>, it notifies SQL Optimizer that expression returns same result if the same values/parameters are used. It gives optimizer additional information how to simplify query.
			/// For example ORDER BY PureFunction("Str") can be removed because PureFunction function uses constant value.
			/// <example>
			/// For example Random function is NOT Pure function because it returns different result all time.
			/// But expression <see cref="Sql.CurrentTimestamp"/> is Pure in case of executed query.
			/// <see cref="Sql.DateAdd(LinqToDB.Sql.DateParts,System.Nullable{double},System.Nullable{System.DateTime})"/> is also Pure function because it returns the same result with the same parameters.  
			/// </example>
			/// </summary>
			public bool           IsPure          { get; set; }
			/// <summary>
			/// Used to determine whether the return type should be treated as
			/// something that can be null If CanBeNull is not explicitly set.
			/// <para>Default is <see cref="IsNullableType.Undefined"/>,
			/// which will be treated as <c>true</c></para> 
			/// </summary>
			public IsNullableType IsNullable       { get; set; }

			internal  bool? _canBeNull;
			/// <summary>
			/// If <c>true</c>, result can be null
			/// </summary>
			public    bool   CanBeNull
			{
				get => _canBeNull ?? true;
				set => _canBeNull = value;
			}

			protected bool GetCanBeNull(ISqlExpression[] parameters)
			{
				if (_canBeNull != null)
					return _canBeNull.Value;

				return CalcCanBeNull(IsNullable, parameters.Select(p => p.CanBeNull)) ?? true;
			}

			public static bool? CalcCanBeNull(IsNullableType isNullable, IEnumerable<bool> nullInfo)
			{
				switch (isNullable)
				{
					case IsNullableType.Undefined              : return null;
					case IsNullableType.Nullable               : return true;
					case IsNullableType.NotNullable            : return false;
				}

				var parameters = nullInfo.ToArray();

				switch (isNullable)
				{
					case IsNullableType.SameAsFirstParameter   : return SameAs(0);
					case IsNullableType.SameAsSecondParameter  : return SameAs(1);
					case IsNullableType.SameAsThirdParameter   : return SameAs(2);
					case IsNullableType.SameAsLastParameter    : return SameAs(parameters.Length - 1);
					case IsNullableType.IfAnyParameterNullable : return parameters.Any(p => p);
				}

				bool SameAs(int parameterNumber)
				{
					if (parameterNumber >= 0 && parameters.Length > parameterNumber)
						return parameters[parameterNumber];
					return true;
				}

				return null;
			}

			protected ISqlExpression[] ConvertArgs(MemberInfo member, ISqlExpression[] args)
			{
				if (member is MethodInfo method)
				{
					if (method.DeclaringType!.IsGenericType)
						args = args.Concat(method.DeclaringType.GetGenericArguments().Select(t => (ISqlExpression)SqlDataType.GetDataType(t))).ToArray();

					if (method.IsGenericMethod)
						args = args.Concat(method.GetGenericArguments().Select(t => (ISqlExpression)SqlDataType.GetDataType(t))).ToArray();
				}

				if (ArgIndices != null)
				{
					var idxs = new ISqlExpression[ArgIndices.Length];

					for (var i = 0; i < ArgIndices.Length; i++)
						idxs[i] = args[ArgIndices[i]];

					return idxs;
				}

				return args;
			}

			public virtual ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				var sqlExpressions = ConvertArgs(member, args);

				return new SqlExpression(member.GetMemberType(), Expression ?? member.Name, Precedence,
					IsAggregate, IsPure, sqlExpressions)
				{
					CanBeNull = GetCanBeNull(sqlExpressions)
				};
			}

			public virtual ISqlExpression? GetExpression(IDataContext dataContext, SelectQuery query,
				Expression expression, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				return null;
			}

			public virtual bool GetIsPredicate(Expression expression) => IsPredicate;
		}
	}
}
