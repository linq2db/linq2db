using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;
using LinqToDB.Data.Sql;
using LinqToDB.Data.Sql.SqlProvider;

namespace Tests.Model
{
	public class NorthwindDB : DbManager
	{
		public NorthwindDB() : base("Northwind")
		{
		}

		public Table<Northwind.Category>            Category            { get { return GetTable<Northwind.Category>();            } }
		public Table<Northwind.Customer>            Customer            { get { return GetTable<Northwind.Customer>();            } }
		public Table<Northwind.Employee>            Employee            { get { return GetTable<Northwind.Employee>();            } }
		public Table<Northwind.EmployeeTerritory>   EmployeeTerritory   { get { return GetTable<Northwind.EmployeeTerritory>();   } }
		public Table<Northwind.OrderDetail>         OrderDetail         { get { return GetTable<Northwind.OrderDetail>();         } }
		public Table<Northwind.Order>               Order               { get { return GetTable<Northwind.Order>();               } }
		public Table<Northwind.Product>             Product             { get { return GetTable<Northwind.Product>();             } }
		public Table<Northwind.ActiveProduct>       ActiveProduct       { get { return GetTable<Northwind.ActiveProduct>();       } }
		public Table<Northwind.DiscontinuedProduct> DiscontinuedProduct { get { return GetTable<Northwind.DiscontinuedProduct>(); } }
		public Table<Northwind.Region>              Region              { get { return GetTable<Northwind.Region>();              } }
		public Table<Northwind.Shipper>             Shipper             { get { return GetTable<Northwind.Shipper>();             } }
		public Table<Northwind.Supplier>            Supplier            { get { return GetTable<Northwind.Supplier>();            } }
		public Table<Northwind.Territory>           Territory           { get { return GetTable<Northwind.Territory>();           } }
		
#if !MONO
		
		public class FreeTextKey<T>
		{
			public T   Key;
			public int Rank;
		}

		class FreeTextTableExpressionAttribute : TableExpressionAttribute
		{
			public FreeTextTableExpressionAttribute()
				: base("")
			{
			}

			public override void SetTable(SqlTable table, MemberInfo member, IEnumerable<Expression> expArgs, IEnumerable<ISqlExpression> sqlArgs)
			{
				var aargs  = sqlArgs.ToArray();
				var arr    = ConvertArgs(member, aargs).ToList();
				var method = (MethodInfo)member;
				var sp     = new MsSql2008SqlProvider();

				{
					var ttype  = method.GetGenericArguments()[0];
					var tbl    = new SqlTable(ttype);

					var database     = tbl.Database     == null ? null : sp.Convert(tbl.Database,     ConvertType.NameToDatabase).  ToString();
					var owner        = tbl.Owner        == null ? null : sp.Convert(tbl.Owner,        ConvertType.NameToOwner).     ToString();
					var physicalName = tbl.PhysicalName == null ? null : sp.Convert(tbl.PhysicalName, ConvertType.NameToQueryTable).ToString();

					var name   = sp.BuildTableName(new StringBuilder(), database, owner, physicalName);

					arr.Add(new SqlExpression(name.ToString(), Precedence.Primary));
				}

				{
					var field = ((ConstantExpression)expArgs.First()).Value;

					if (field is string)
						arr[0] = new SqlExpression(field.ToString(), Precedence.Primary);
					else if (field is LambdaExpression)
					{
						var body = ((LambdaExpression)field).Body;

						if (body is MemberExpression)
						{
							var name = ((MemberExpression)body).Member.Name;

							name = sp.Convert(name, ConvertType.NameToQueryField).ToString();

							arr[0] = new SqlExpression(name, Precedence.Primary);
						}
					}
				}

				table.SqlTableType   = SqlTableType.Expression;
				table.Name           = "FREETEXTTABLE({6}, {2}, {3}) {1}";
				table.TableArguments = arr.ToArray();
			}
		}

		[FreeTextTableExpressionAttribute]
		public Table<FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(string field, string text)
		{
			return GetTable<FreeTextKey<TKey>>(
				this,
				((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(TTable), typeof(TKey)),
				field,
				text);
		}

		[FreeTextTableExpressionAttribute]
		public Table<FreeTextKey<TKey>> FreeTextTable<TTable,TKey>(Expression<Func<TTable,string>> fieldSelector, string text)
		{
			return GetTable<FreeTextKey<TKey>>(
				this,
				((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(TTable), typeof(TKey)),
				fieldSelector,
				text);
		}
		
#endif
	}
}
