﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Extensions;
	using SqlQuery;

	partial class Sql
	{
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class ExpressionAttribute : Attribute
		{
			public ExpressionAttribute(string expression)
			{
				Expression = expression;
				Precedence = SqlQuery.Precedence.Primary;
			}

			public ExpressionAttribute(string expression, params int[] argIndices)
			{
				Expression = expression;
				ArgIndices = argIndices;
				Precedence = SqlQuery.Precedence.Primary;
			}

			public ExpressionAttribute(string configuration, string expression)
			{
				Configuration = configuration;
				Expression    = expression;
				Precedence    = SqlQuery.Precedence.Primary;
			}

			public ExpressionAttribute(string configuration, string expression, params int[] argIndices)
			{
				Configuration = configuration;
				Expression    = expression;
				ArgIndices    = argIndices;
				Precedence    = SqlQuery.Precedence.Primary;
			}

			public string         Expression       { get; set; }
			public int[]?         ArgIndices       { get; set; }
			public int            Precedence       { get; set; }
			public string?        Configuration    { get; set; }
			public bool           ServerSideOnly   { get; set; }
			public bool           PreferServerSide { get; set; }
			public bool           InlineParameters { get; set; }
			public bool           ExpectExpression { get; set; }
			public bool           IsPredicate      { get; set; }
			public bool           IsAggregate      { get; set; }
			public IsNullableType IsNullable       { get; set; }

			internal  bool? _canBeNull;
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
					if (method.DeclaringType.IsGenericType)
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
					IsAggregate, sqlExpressions)
				{
					CanBeNull = GetCanBeNull(sqlExpressions)
				};
			}

			public virtual ISqlExpression? GetExpression(IDataContext dataContext, SelectQuery query,
				Expression expression, Func<Expression, ISqlExpression> converter)
			{
				return null;
			}

			public virtual bool GetIsPredicate(Expression expression) => IsPredicate;
		}
	}
}
