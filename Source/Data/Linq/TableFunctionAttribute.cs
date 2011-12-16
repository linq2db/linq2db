using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data.Linq
{
	using SqlBuilder;

	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class TableFunctionAttribute : Attribute
	{
		public TableFunctionAttribute()
		{
		}

		public TableFunctionAttribute(string name)
		{
			Name = name;
		}

		public TableFunctionAttribute(string name, params int[] argIndices)
		{
			Name        = name;
			ArgIndices  = argIndices;
		}

		public TableFunctionAttribute(string sqlProvider, string name)
		{
			SqlProvider = sqlProvider;
			Name        = name;
		}

		public TableFunctionAttribute(string sqlProvider, string name, params int[] argIndices)
		{
			SqlProvider = sqlProvider;
			Name        = name;
			ArgIndices  = argIndices;
		}

		public string SqlProvider      { get; set; }
		public string Name             { get; set; }
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

		public virtual void SetTable(SqlTable table, MemberInfo member, IEnumerable<Expression> arguments, IEnumerable<ISqlExpression> sqlArgs)
		{
			table.SqlTableType   = SqlTableType.Function;
			table.Name           = Name ?? member.Name;
			table.PhysicalName   = Name ?? member.Name;
			table.TableArguments = ConvertArgs(member, sqlArgs.ToArray());
		}
	}
}
