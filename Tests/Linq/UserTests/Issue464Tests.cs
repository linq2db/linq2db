using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue464Tests : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void Test(string context)
		{
			Exception exception = null;

			try
			{
				var schema = new MappingSchema();

				schema.SetDataType(typeof(MyInt), DataType.Int32);
				schema.SetConvertExpression<MyInt, int>(x => x.Value);
				schema.SetConvertExpression<int, MyInt>(x => new MyInt { Value = x });
				schema.SetConvertExpression<MyInt, DataParameter>(x => new DataParameter { DataType = DataType.Int32, Value = x.Value });

				schema.GetFluentMappingBuilder()
					  .Entity<Entity>()
					  .HasColumn(x => x.Id)
					  .HasColumn(x => x.Value);

				using (var db = new DataConnection(context).AddMappingSchema(schema))
				{
					var data = new[] { new Entity { Id = 1, Value = new MyInt { Value = 1 } } };
					var temptable = db.CreateTable<Entity>("#temptable");
					temptable.BulkCopy(data);
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			Assert.IsNull(exception);
		}

		public class Entity
		{
			public int Id { get; set; }
			public MyInt Value { get; set; }
		}

		public class MyInt
		{
			public int Value { get; set; }
		}
	}
}
