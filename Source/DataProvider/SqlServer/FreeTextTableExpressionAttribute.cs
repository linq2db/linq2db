using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

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

		public override void SetTable(SqlTable table, MemberInfo member, IEnumerable<Expression> expArgs, IEnumerable<ISqlExpression> sqlArgs)
		{
			var aargs  = sqlArgs.ToArray();
			var arr    = ConvertArgs(member, aargs).ToList();
			var method = (MethodInfo)member;

			{
				var ttype  = method.GetGenericArguments()[0];
				var tbl    = new SqlTable(ttype);

				var database     = Convert(tbl.Database);
				var owner        = Convert(tbl.Owner);
				var physicalName = Convert(tbl.PhysicalName);

				var name = "";

				if (database != null)
					name = database + "." + (owner == null ? "." : owner + ".");
				else if (owner != null)
					name = owner + ".";

				name += physicalName;

				arr.Add(new SqlExpression(name, Precedence.Primary));
			}

			{
				var field = ((ConstantExpression)expArgs.First()).Value;

				if (field is string)
				{
					arr[0] = new SqlExpression(field.ToString(), Precedence.Primary);
				}
				else if (field is LambdaExpression)
				{
					var body = ((LambdaExpression)field).Body;

					if (body is MemberExpression)
					{
						var name = ((MemberExpression)body).Member.Name;

						if (name.Length > 0 && name[0] != '[')
							name = "[" + name + "]";

						arr[0] = new SqlExpression(name, Precedence.Primary);
					}
				}
			}

			table.SqlTableType   = SqlTableType.Expression;
			table.Name           = "FREETEXTTABLE({6}, {2}, {3}) {1}";
			table.TableArguments = arr.ToArray();
		}
	}
}
