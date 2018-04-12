using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{

	public class Issue1083Tests: TestBase
	{
		public static class MySql
		{
			class WithPriceBuilder : Sql.IExtensionCallBuilder
			{
				public void Build(Sql.ISqExtensionBuilder builder)
				{
					var index = builder.GetValue<int>(1);
					var field = builder.GetExpression(0) as SqlField;
					if (field == null)
						throw new Exception("Entity required as paramater");

					var fieldName = "PRICE" + index;
					var table = (SqlTable)field.Table;
					if (!table.Fields.TryGetValue(fieldName, out var newField))
					{
						newField = new SqlField();
						newField.Name = fieldName;
						newField.Table = table;
						table.Fields.Add(fieldName, newField);
					}

					builder.ResultExpression = newField;
				}
			}

			[Sql.Extension("", BuilderType = typeof(WithPriceBuilder), ServerSideOnly = true)]
			public static double WithPrice<T>(T entity, [SqlQueryDependent] int index)
			{
				throw new NotImplementedException();
			}
			
		}

		[Table("PRODUCT")]
		public class Product
		{
			[Column]
			public int PriceID { get; set; }

			[Column]
			public double PRICE1 { get; set; }

			[Column]
			public double PRICE2 { get; set; }

			[Column]
			public double PRICE3 { get; set; }

			[Column]
			public double PRICE4 { get; set; }

			[Column]
			public double PRICE5 { get; set; }
		}

		public class ProductDTO
		{
			public int ID { get; set; }
			public double PRICE { get; set; }
		}

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.CreateTable<Product>();
				try
				{
					var whichPriceName = 1;
					var query = from p in db.GetTable<Product>()
						select new ProductDTO
						{
							ID = p.PriceID,
							PRICE = MySql.WithPrice(p, whichPriceName)
						};

					var result = query.ToArray();
				}
				finally
				{
					db.DropTable<Product>();
				}
			}
		}
	}
}
