using System;
using System.Linq;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.SqlBuilder;

namespace LinqToDB.Data.Linq
{
	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
	public class SqlFunctionAttribute : Attribute
	{
		public SqlFunctionAttribute()
		{
		}

		public SqlFunctionAttribute(string name)
		{
			Name = name;
		}

		public SqlFunctionAttribute(string name, params int[] argIndices)
		{
			Name        = name;
			ArgIndices  = argIndices;
		}

		public SqlFunctionAttribute(string sqlProvider, string name)
		{
			SqlProvider = sqlProvider;
			Name        = name;
		}

		public SqlFunctionAttribute(string sqlProvider, string name, params int[] argIndices)
		{
			SqlProvider = sqlProvider;
			Name        = name;
			ArgIndices  = argIndices;
		}

		public string SqlProvider      { get; set; }
		public string Name             { get; set; }
		public bool   ServerSideOnly   { get; set; }
		public bool   PreferServerSide { get; set; }
		public int[]  ArgIndices       { get; set; }

		protected ISqlExpression[] ConvertArgs(MemberInfo member, ISqlExpression[] args)
		{
			if (member is MethodInfo)
			{
				var method = (MethodInfo)member;

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
			return new SqlFunction(member.GetMemberType(), Name ?? member.Name, ConvertArgs(member, args));
		}
	}
}
