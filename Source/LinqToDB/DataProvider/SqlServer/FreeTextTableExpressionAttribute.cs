using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
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

		public override void SetTable(MappingSchema mappingSchema, SqlTable table, MemberInfo member, IEnumerable<Expression> expArgs, IEnumerable<ISqlExpression> sqlArgs)
		{
			var aargs  = sqlArgs.ToArray();
			var arr    = ConvertArgs(member, aargs).ToList();
			var method = (MethodInfo)member;

			{
				var ttype  = method.GetGenericArguments()[0];
				var tbl    = new SqlTable(ttype);

				var server       = Convert(tbl.Server);
				var database     = Convert(tbl.Database);
				var schema       = Convert(tbl.Schema);
				var physicalName = Convert(tbl.PhysicalName);

				var name = "";

				if (server != null)
					name = server + "." + (database != null ? database + "." + (schema == null ? "." : schema + ".") : "." + (schema == null ? "." : schema + "."));
				if (database != null)
					name = database + "." + (schema == null ? "." : schema + ".");
				else if (schema != null)
					name = schema + ".";

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
