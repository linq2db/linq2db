using System;
using System.Linq;

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
		private class InterceptDataConnection : DataConnection, IDataContext
		{
			public InterceptDataConnection(string providerName, string connectionString) : base(providerName, connectionString)
			{
			}

			void IDataContext.QueryExecuted(int count, string action)
			{
				// TODO: determine that the query is on a table with RowVer
				if (count == 0)
				{
					throw new ConcurrencyException();
				}
			}

			/// <summary>
			/// We need to use same paremeters as for original query
			/// </summary>
			/// <param name="original"></param>
			private SelectQuery Clone(SelectQuery original)
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

					var rowverAttrib = rowVersion.MemberAccessor.GetAttribute<RowVersionAttribute>();

					var newQuery = Clone(selectQuery);
					source = newQuery.From.Tables[0].Source as SqlTable;
					var field = source.Fields[rowVersion.ColumnName];

					// get real value of RowVersion
					var updateColumn = newQuery.Update.Items.FirstOrDefault(ui => ui.Column is SqlField && ((SqlField)ui.Column).Equals(field));
					ISqlExpression versionValue;
					
					versionValue = updateColumn == null ? null: updateColumn.Expression;

					if (updateColumn == null || !rowverAttrib.DBManaged)
					{
						updateColumn = new SelectQuery.SetExpression(field, field);
						newQuery.Update.Items.Add(updateColumn);

						updateColumn.Expression = new SqlBinaryExpression(typeof(int), field, "+", new SqlValue(1));
					}

					if (!selectQuery.OverrideConcurrencyCheck && versionValue != null)
					{
						var search = newQuery.Where.SearchCondition;
						var current = search;

						if (search.Conditions.Count > 0 && search.Precedence < Precedence.LogicalConjunction)
						{
							current = new SelectQuery.SearchCondition();
							var prev = new SelectQuery.SearchCondition();

							prev.Conditions.AddRange(search.Conditions);
							search.Conditions.Clear();

							search.Conditions.Add(new SelectQuery.Condition(false, current, false));
							search.Conditions.Add(new SelectQuery.Condition(false, prev, false));
						}

						current.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.ExprExpr(field, SelectQuery.Predicate.Operator.Equal, versionValue)));
					}

					return newQuery;
				}

				#endregion Update

				#region Insert

				else if (selectQuery.IsInsert)
				{
					var source = selectQuery.Insert.Into;
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

				#region Delete

				else if (selectQuery.IsDelete)
				{
					var source = selectQuery.From.Tables[0];
					var descriptor = MappingSchema.GetEntityDescriptor(source.SystemType);
					var rowVersion = descriptor.Columns.SingleOrDefault(c => c.MemberAccessor.GetAttribute<RowVersionAttribute>() != null);

					if (rowVersion == null)
						return selectQuery;

					// TODO: add in the RowVer value check

				}
				#endregion Delete

				return selectQuery;
			}
		}

		[AttributeUsage(
			AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
			AllowMultiple = false, Inherited = true)]
		public class RowVersionAttribute : Attribute
		{
			public bool DBManaged { get; set; }
		}

		public class ConcurrencyException : Exception
		{
			public object obj { get; set; }

			public ConcurrencyException()
			{
			}

			public ConcurrencyException(string message)
				: base(message)
			{
			}

			public ConcurrencyException(string message, Exception inner)
				: base(message, inner)
			{
			}
		}


		[Table("TestTable")]
		public class TestTable
		{
			[Column(Name = "ID", IsPrimaryKey = true, PrimaryKeyOrder = 0, IsIdentity = false)]
			public int ID { get; set; }

			[Column(Name = "Description")]
			public string Description { get; set; }

			private int _rowVer;

			[Column(Name = "RowVer", Storage = "_rowVer")]
			[RowVersion(DBManaged = false)]
			public int RowVer { get { return _rowVer; } }
		}

		private InterceptDataConnection _connection;

		[SetUp]
		public void SetUp()
		{
			_connection = new InterceptDataConnection(ProviderName.SQLite, "Data Source=:memory:;");

			_connection.CreateTable<TestTable>();

			_connection.Insert(new TestTable { ID = 1, Description = "Row 1" });
			_connection.Insert(new TestTable { ID = 2, Description = "Row 2" });
		}

		[TearDown]
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
		public void CheckUpdateMultiple()
		{
			var db = _connection;

			var table = db.GetTable<TestTable>();

			db.Insert(new TestTable { ID = 1000, Description = "Row 1000" });
			db.Insert(new TestTable { ID = 1001, Description = "Row 1001" });

			var results = table
				.Where(t => t.ID >= 1000)
				.Set(t => t.Description, "Changed description")
				.Update();

			Assert.AreEqual(2, results);

			var row1000 = table.First(t => t.ID == 1000);

			Assert.AreEqual(2, row1000.RowVer);
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

			Assert.Throws(typeof(ConcurrencyException), () => db.Update(row1));
		}

		[Test]
		public void CheckUpdateFailWithOverride()
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

			try
			{
				db.Update(row2);
				Assert.Fail("Should not get to here");
			}
			catch (ConcurrencyException)
			{
				// got concurreny excption but now override it
				result = db.Update(row2, true);
				Assert.AreEqual(1, result);

				var resultrow = table.First(t => t.ID == 1);
				Assert.AreEqual("Another change", resultrow.Description);
				Assert.AreEqual(3, resultrow.RowVer);
			}
		}

		[Test]
		public void InsertAndDeleteTest()
		{
			var db = _connection;
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

		[Test]
		public void CheckInsertOrUpdate()
		{
			var db = _connection;
			var table = db.GetTable<TestTable>();

			var result = db.InsertOrReplace(new TestTable { ID = 3, Description = "Row 3" });

			Assert.AreEqual(1, result);
			Assert.AreEqual(3, table.Count());

			var newval = table.First(t => t.ID == 3);

			newval.Description = "Row 3 New description";

			result = db.InsertOrReplace(newval);
			Assert.AreEqual(1, result);
			Assert.AreEqual(3, table.Count());
		}
	}
}