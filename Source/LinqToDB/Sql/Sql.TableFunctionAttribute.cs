using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Mapping;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;

	using SqlQuery;

	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
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

			public TableFunctionAttribute(string configuration, string name)
			{
				Configuration = configuration;
				Name          = name;
			}

			public TableFunctionAttribute(string configuration, string name, params int[] argIndices)
			{
				Configuration = configuration;
				Name          = name;
				ArgIndices    = argIndices;
			}

			public string Configuration { get; set; }
			public string Name          { get; set; }
			public string Schema        { get; set; }
			public string Database      { get; set; }
			public string Server        { get; set; }
			public int[]  ArgIndices    { get; set; }

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

			public virtual void SetTable(MappingSchema mappingSchema, SqlTable table, MemberInfo member, IEnumerable<Expression> arguments, IEnumerable<ISqlExpression> sqlArgs)
			{
				table.SqlTableType   = SqlTableType.Function;
				table.Name           = Name ?? member.Name;
				table.PhysicalName   = Name ?? member.Name;
				table.TableArguments = ConvertArgs(member, sqlArgs.ToArray());

				if (Schema   != null) table.Schema   = Schema;
				if (Database != null) table.Database = Database;
				if (Server   != null) table.Server   = Server;
			}
		}
	}
}
