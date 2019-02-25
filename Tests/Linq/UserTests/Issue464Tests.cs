using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using System.Linq;

	using LinqToDB.DataProvider.Firebird;

	[TestFixture]
	public class Issue464Tests : TestBase
	{

		[Test]
		public void Test([DataSources(false)] string context)
		{
			var firebirdQuote = FirebirdSqlBuilder.IdentifierQuoteMode;

			var schema = new MappingSchema();

			schema.SetDataType(typeof(MyInt), DataType.Int32);

			schema.SetConvertExpression<MyInt,   int>          (x => x.Value);
			schema.SetConvertExpression<int,     MyInt>        (x => new MyInt { Value = x });
			schema.SetConvertExpression<Int64,   MyInt>        (x => new MyInt { Value = (int)x }); //SQLite
			schema.SetConvertExpression<decimal, MyInt>        (x => new MyInt { Value = (int)x }); //Oracle
			schema.SetConvertExpression<MyInt,   DataParameter>(x => new DataParameter { DataType = DataType.Int32, Value = x.Value });

			schema.GetFluentMappingBuilder()
				  .Entity<Entity>()
				  .HasTableName("Issue464")
				  .HasColumn(x => x.Id)
				  .HasColumn(x => x.Value);

			using (var db = new  DataConnection(context).AddMappingSchema(schema))
			{
				try
				{
					FirebirdSqlBuilder.IdentifierQuoteMode = FirebirdIdentifierQuoteMode.Auto;

					var temptable = db.CreateTable<Entity>();

					var data = new[]
					{
						new Entity {Id = 1, Value = new MyInt {Value = 1}},
						new Entity {Id = 2, Value = new MyInt {Value = 2}},
						new Entity {Id = 3, Value = new MyInt {Value = 3}}
					};

					temptable.BulkCopy(data);

					AreEqual(data, temptable.ToList());
				}
				finally
				{
					db.DropTable<Entity>();

					FirebirdSqlBuilder.IdentifierQuoteMode = firebirdQuote;
				}

			}
		}

		public class Entity
		{
			public int   Id    { get; set; }
			public MyInt Value { get; set; }

			public override bool Equals(object obj)
			{
				var e = (Entity) obj;
				return Id == e.Id && Value.Value == Id;
			}

			public override int GetHashCode()
			{
				return Id;
			}
		}

		public class MyInt
		{
			public int Value { get; set; }
		}
	}
}
