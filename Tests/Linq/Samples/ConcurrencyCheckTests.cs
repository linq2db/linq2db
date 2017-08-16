using System;
using System.Linq;

#if !NOASYNC
using System.Threading.Tasks;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Samples
{
	[TestFixture]
	public class ConcurrencyCheckTests : TestBase
	{
#if !MONO
		class InterceptDataConnection : DataConnection
		{
			public InterceptDataConnection(string providerName, string connectionString) : base(providerName, connectionString)
			{
			}

			/// <summary>
			/// We need to use same paremeters as for original query
			/// </summary>
			/// <param name="original"></param>
			SelectQuery Clone(SelectQuery original)
			{
				var clone = original.Clone();

				var pairs = from o in original.Parameters.Distinct()
							join n in clone.Parameters.Distinct() on o.Name equals n.Name
							select new { Old = o, New = n };

				var dic = pairs.ToDictionary(p => p.New, p => p.Old);

				clone = new QueryVisitor().Convert(clone, e =>
							  {
								  var param = e as SqlParameter;
								  SqlParameter newParam;
								  if (param != null && dic.TryGetValue(param, out newParam))
								  {
									  return newParam;
								  }
								  return e;
							  });

				clone.Parameters.Clear();
				clone.Parameters.AddRange(original.Parameters);

				return clone;
			}

			protected override SelectQuery ProcessQuery(SelectQuery selectQuery)
			{
				#region Update

				if (selectQuery.IsUpdate)
				{
					var source = selectQuery.From.Tables[0].Source as SqlTable;
					if (source == null)
						return selectQuery;

					var descriptor = MappingSchema.GetEntityDescriptor(source.ObjectType);
					if (descriptor == null)
						return selectQuery;

					var rowVersion = descriptor.Columns.SingleOrDefault(c => c.MemberAccessor.GetAttribute<RowVersionAttribute>() != null);
					if (rowVersion == null)
						return selectQuery;

					var newQuery = Clone(selectQuery);
					source       = newQuery.From.Tables[0].Source as SqlTable;
					var field    = source.Fields[rowVersion.ColumnName];

					// get real value of RowVersion
					var updateColumn = newQuery.Update.Items.FirstOrDefault(ui => ui.Column is SqlField && ((SqlField)ui.Column).Equals(field));
					if (updateColumn == null)
					{
						updateColumn = new SelectQuery.SetExpression(field, field);
						newQuery.Update.Items.Add(updateColumn);
					}

					updateColumn.Expression = new SqlBinaryExpression(typeof(int), field, "+", new SqlValue(1));

					return newQuery;

				}

				#endregion Update

				#region Insert

				else if (selectQuery.IsInsert)
				{
					var source     = selectQuery.Insert.Into;
					var descriptor = MappingSchema.GetEntityDescriptor(source.ObjectType);
					var rowVersion = descriptor.Columns.SingleOrDefault(c => c.MemberAccessor.GetAttribute<RowVersionAttribute>() != null);

					if (rowVersion == null)
						return selectQuery;

					
					var newQuery = Clone(selectQuery);

					var field = newQuery.Insert.Into[rowVersion.ColumnName];

					var versionColumn = (from i in newQuery.Insert.Items
										 let f = i.Column as SqlField
										 where f != null && f.PhysicalName == field.PhysicalName
										 select i).FirstOrDefault();

					// if we do not try to insert version, lets suppose it should be done in database
					if (versionColumn != null)
					{
						versionColumn.Expression = new SqlValue(1);
						return newQuery;
					}
				}
				#endregion Insert

				return selectQuery;
			}
		}

		public class RowVersionAttribute: Attribute
		{ }

		[Table("TestTable")]
		public class TestTable
		{
			[Column(Name = "ID", IsPrimaryKey = true, PrimaryKeyOrder = 0, IsIdentity = false)]
			public int ID { get; set; }

			[Column(Name = "Description")]
			public string Description { get; set; }

			private int _rowVer;

			[Column(Name = "RowVer", Storage = "_rowVer", IsPrimaryKey = true, PrimaryKeyOrder = 1)]
			[RowVersion]
			public int RowVer { get { return _rowVer; } }
		}

		private InterceptDataConnection _connection;

		[OneTimeSetUp]
		public void SetUp()
		{
			_connection = new InterceptDataConnection(ProviderName.SQLite, "Data Source=:memory:;");

			_connection.CreateTable<TestTable>();

			_connection.Insert(new TestTable { ID = 1, Description = "Row 1" });
			_connection.Insert(new TestTable { ID = 2, Description = "Row 2" });
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_connection.Dispose();
		}

		[Test]
		public void CheckUpdateOK()
		{
			var db = _connection;

			var table = db.GetTable<TestTable>();

			var row = table.First(t => t.ID == 1);
			row.Description = "Changed desc";

			var result = db.Update(row);

			Assert.AreEqual(1, result);

			var updated = table.First(t => t.ID == 1);
			Assert.AreEqual(row.RowVer + 1, updated.RowVer);
		}

		[Test]
		public void CheckUpdateFail()
		{
			var db = _connection;
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

		[Test, Parallelizable(ParallelScope.None)]
		public void InsertAndDeleteTest()
		{
			var db    = _connection;
			var table = db.GetTable<TestTable>();

			db.Insert(new TestTable { ID = 1000, Description = "Delete Candidate 1000" });
			db.Insert(new TestTable { ID = 1001, Description = "Delete Candidate 1001" });

			var obj1000 = table.First(_ => _.ID == 1000);
			var obj1001 = table.First(_ => _.ID == 1001);

			Assert.IsNotNull(obj1000);
			Assert.IsNotNull(obj1001);
			Assert.AreEqual(1, obj1000.RowVer);
			Assert.AreEqual(1, obj1001.RowVer);

			db.Update(obj1000);

			Assert.AreEqual(0, db.Delete(obj1000));
			Assert.AreEqual(1, db.Delete(obj1001));
		}

#if !NOASYNC

		[Test, Parallelizable(ParallelScope.None)]
		public async Task InsertAndDeleteTestAsync()
		{
			var db    = _connection;
			var table = db.GetTable<TestTable>();

			await db.InsertAsync(new TestTable { ID = 2000, Description = "Delete Candidate 1000" });
			await db.InsertAsync(new TestTable { ID = 2001, Description = "Delete Candidate 1001" });

			var obj2000 = await table.FirstAsync(_ => _.ID == 2000);
			var obj2001 = await table.FirstAsync(_ => _.ID == 2001);

			Assert.IsNotNull(obj2000);
			Assert.IsNotNull(obj2001);
			Assert.AreEqual(1, obj2000.RowVer);
			Assert.AreEqual(1, obj2001.RowVer);

			await db.UpdateAsync(obj2000);

			Assert.AreEqual(0, await db.DeleteAsync(obj2000));
			Assert.AreEqual(1, await db.DeleteAsync(obj2001));
		}

#endif

		[Test]
		public void CheckInsertOrUpdate()
		{
			var db     = _connection;
			var table  = db.GetTable<TestTable>();

			var result = db.InsertOrReplace(new TestTable {ID = 3, Description = "Row 3"});

			Assert.AreEqual(1, result);
			Assert.AreEqual(3, table.Count());

			var newval = table.First(t => t.ID == 3);

			newval.Description = "Row 3 New description";

			result = db.InsertOrReplace(newval);
			Assert.AreEqual(1, result);
			Assert.AreEqual(3, table.Count());
		}
#endif
	}
}