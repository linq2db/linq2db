using System;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;
using LinqToDB.Mapping;

namespace LinqToDB
{
	using System.Linq.Expressions;
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

			public string Expression       { get; set; }
			public int[]  ArgIndices       { get; set; }
			public int    Precedence       { get; set; }
			public string Configuration    { get; set; }
			public bool   ServerSideOnly   { get; set; }
			public bool   PreferServerSide { get; set; }
			public bool   InlineParameters { get; set; }
			public bool   ExpectExpression { get; set; }
			public bool   IsPredicate      { get; set; }

			private bool? _canBeNull;
			public  bool   CanBeNull
			{
				get { return _canBeNull ?? true;  }
				set { _canBeNull = value;         }
			}

			protected ISqlExpression[] ConvertArgs(MemberInfo member, ISqlExpression[] args)
			{
				if (member is MethodInfo)
				{
					var method = (MethodInfo)member;

					if (method.DeclaringType.IsGenericTypeEx())
						args = args.Concat(method.DeclaringType.GetGenericArgumentsEx().Select(t => (ISqlExpression)SqlDataType.GetDataType(t))).ToArray();

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
				return new SqlExpression(member.GetMemberType(), Expression ?? member.Name, Precedence, ConvertArgs(member, args)) { CanBeNull = CanBeNull };
			}

			public virtual ISqlExpression GetExpression(MappingSchema mapping, Expression expression, Func<Expression, ISqlExpression> converter)
			{
				return null;
			}
		}
	}
}
