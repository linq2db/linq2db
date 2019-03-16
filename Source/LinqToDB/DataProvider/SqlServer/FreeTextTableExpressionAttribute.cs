using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlQuery;
	using Expressions;

	[Obsolete("Use LinqToDB.DataProvider.SqlServer.SqlServerExtensions.FreeTextTable extension method")]
	public class FreeTextTableExpressionAttribute : Sql.TableExpressionAttribute
	{
		public FreeTextTableExpressionAttribute()
			: base("")
		{
		}

		static string Convert(string value)
		{
			if (!string.IsNullOrEmpty(value) && value[0] != '[')
				return "[" + value + "]";
			return value;
		}

		public override void SetTable(MappingSchema mappingSchema, SqlTable table, MemberInfo member, IEnumerable<Expression> expArgs, IEnumerable<ISqlExpression> sqlArgs)
		{
			var aargs  = sqlArgs.ToArray();
			var arr    = ConvertArgs(member, aargs).ToList();
			var method = (MethodInfo)member;

			var ttype  = method.GetGenericArguments()[0];
			var tbl    = new SqlTable(ttype);

			arr.Add(tbl);

			var fieldExpr = expArgs.First().Unwrap();
			object field = fieldExpr;
			if (fieldExpr.NodeType == ExpressionType.Constant)
				field = ((ConstantExpression)fieldExpr).Value;

			ISqlExpression fieldExpression = null;

			if (field is LambdaExpression lambdaExpression)
			{
				var memberInfo = MemberHelper.GetMemberInfo(lambdaExpression.Body);

				var ed = mappingSchema.GetEntityDescriptor(ttype);

				var column = ed.Columns.FirstOrDefault(c => c.MemberInfo == memberInfo);
				if (column != null) 
					fieldExpression = new SqlField(tbl.Fields[column.MemberName]);
			}
			else if (field is string fieldName)
				fieldExpression = new SqlExpression(fieldName, Precedence.Primary);

			arr[0] = fieldExpression ?? throw new LinqToDBException($"Can not retrieve Field Name from expression {field}");

			table.SqlTableType   = SqlTableType.Expression;
			table.Name           = "FREETEXTTABLE({6}, {2}, {3}) {1}";
			table.TableArguments = arr.ToArray();
		}
	}
}
