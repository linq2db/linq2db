namespace Tests.Samples
{
	using System;
	using System.Linq;

	using LinqToDB;
	using LinqToDB.Data;
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;

	using NUnit.Framework;


	[TestFixture]
	public class ConcurrencyCheckTests : TestBase
	{
		private class InterceptDataConnection : DataConnection
		{
			public InterceptDataConnection(string configurationString) : base(configurationString)
			{
			}

			protected override SelectQuery ProcessQuery(SelectQuery selectQuery)
			{
				if (selectQuery.IsUpdate)
				{
					var table  = selectQuery.From.Tables[0];
					var source = table.Source as SqlTable;
					if (source != null)
					{
						var descriptor = MappingSchema.GetEntityDescriptor(source.ObjectType);
						if (descriptor != null)
						{
							var rowVersion = descriptor.Columns
								.SingleOrDefault(c => c.MemberAccessor.GetAttribute<RowVersionAttribute>() != null);
							if (rowVersion != null)
							{
								var newQuery = selectQuery.Clone();
								table        = newQuery.From.Tables[0];
								source       = table.Source as SqlTable;

								var field = source.Fields[rowVersion.MemberName];

								// get real value of RowVersion
								var updateColumn = newQuery.Update.Items.FirstOrDefault(ui => ui.Column is SqlField && ((SqlField)ui.Column).Equals(field));
								if (updateColumn != null)
								{
									var versionValue = updateColumn.Expression;

									updateColumn.Expression = new SqlBinaryExpression(typeof(int), field, "+", new SqlValue(1));

									var search  = newQuery.Where.SearchCondition;
									var current = search;

									if (search.Conditions.Count > 0 && search.Precedence < Precedence.LogicalConjunction)
									{
										 current = new SelectQuery.SearchCondition();
										var prev = new SelectQuery.SearchCondition();

										prev.  Conditions.AddRange(search.Conditions);
										search.Conditions.Clear();

										search.Conditions.Add(new SelectQuery.Condition(false, current, false));
										search.Conditions.Add(new SelectQuery.Condition(false, prev,    false));
									}

									current.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.ExprExpr(field, SelectQuery.Predicate.Operator.Equal, versionValue)));
									return newQuery;
								}

							}
						}
					}
				}
				return selectQuery;
			}
		}

		public class RowVersionAttribute: Attribute
		{ }

		[Table("TestTable")]
		public class TestTable
		{
			[Column(Name = "ID", IsPrimaryKey = true, IsIdentity = true)]
			public int ID { get; set; }

			[Column(Name = "Description")]
			public string Description { get; set; }

			private int _rowVer;

			[Column(Name = "RowVer", Storage = "_rowVer", SkipOnInsert = true)]
            [RowVersion]
			public int RowVer { get { return _rowVer; } }
		}

		[SetUp]
		public void TestInitialize()
		{
			var sql = @"
				DROP TABLE IF EXISTS TestTable;
				CREATE TABLE TestTable(
					ID INTEGER PRIMARY KEY,
					Description NVARCHAR(50),
					RowVer INTEGER DEFAULT 1)";

			using (var db = new InterceptDataConnection(ProviderName.SQLite))
			{
				db.Execute(sql);
				db.Insert(new TestTable { ID = 1, Description = "Row 1" });
				db.Insert(new TestTable { ID = 2, Description = "Row 2" });
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite)]
		public void CheckUpdateOK(string context)
		{
			using (var db = new InterceptDataConnection(context))
			{
				var table = db.GetTable<TestTable>();

				var row = table.First(t => t.ID == 1);
				row.Description = "Changed desc";

				var result = db.Update(row);

				Assert.AreEqual(1, result);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite)]
		public void CheckUpdateFail(string context)
		{
			using (var db = new InterceptDataConnection(context))
			{
				var table = db.GetTable<TestTable>();

				var row1 = table.First(t => t.ID == 1);
				var row2 = table.First(t => t.ID == 1);

				// 1st change of the record will modify the rowver to the rowver + 1
				row1.Description = "Changed desc";

				var result = db.Update(row1);

				Assert.AreEqual(1, result);

				// 2nd change will fail as the version number is different to the one sent with the update
				row2.Description = "Another change";

				result = db.Update(row1);

				Assert.AreEqual(0, result);
			}
		}		
	}
}