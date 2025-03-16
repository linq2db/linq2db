using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Samples
{
	[TestFixture]
	public class ConcurrencyCheckTests : TestBase
	{
		sealed class InterceptDataConnection : DataConnection
		{
			public InterceptDataConnection(string providerName, string connectionString) : base(new DataOptions().UseConnectionString(providerName, connectionString))
			{
			}

			/// <summary>
			/// We need to use same paremeters as for original query
			/// </summary>
			/// <param name="original"></param>
			SqlStatement Clone(SqlStatement original)
			{
				var clone = original.Clone(e =>
				{
					if (!(e is IQueryElement queryElement))
						return false;
					return queryElement.ElementType != QueryElementType.SqlParameter;
				});

				return clone;
			}

			static SqlTable? GetUpdateTable(SqlUpdateStatement updateStatement)
			{
				var tableToUpdate = updateStatement.Update.Table;

				tableToUpdate ??= QueryHelper.EnumerateAccessibleSources(updateStatement.SelectQuery)
					.OfType<SqlTable>()
					.FirstOrDefault();

				return tableToUpdate;
			}

			static SqlTable? GetUpdateTable(SqlStatement statement)
			{
				if (statement is SqlUpdateStatement update)
					return GetUpdateTable(update);

				if (statement.SelectQuery == null)
					return null;

				if (statement.SelectQuery.From.Tables.Count > 0 &&
				    statement.SelectQuery?.From.Tables[0].Source is SqlTable source)
				{
					return source;
				}

				return null;
			}

			protected override SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
			{
				#region Update

				if (statement.QueryType == QueryType.Update || statement.QueryType == QueryType.InsertOrUpdate)
				{
					var query = statement.SelectQuery!;

					SqlTable? updateTable = GetUpdateTable(statement);

					if (updateTable == null)
						return statement;

					var descriptor = MappingSchema.GetEntityDescriptor(updateTable.ObjectType);
					if (descriptor == null)
						return statement;

					var rowVersion = descriptor.Columns.SingleOrDefault(c => c.MemberAccessor.MemberInfo.HasAttribute<RowVersionAttribute>());
					if (rowVersion == null)
						return statement;

					var newStatment = Clone(statement);
					updateTable = GetUpdateTable(newStatment) ?? throw new InvalidOperationException();

					var field = updateTable.FindFieldByMemberName(rowVersion.ColumnName) ?? throw new InvalidOperationException();

					// get real value of RowVersion
					var updateColumn = newStatment.RequireUpdateClause().Items.FirstOrDefault(ui => ui.Column is SqlField fld && fld.Equals(field));
					if (updateColumn == null)
					{
						updateColumn = new SqlSetExpression(field, field);
						newStatment.RequireUpdateClause().Items.Add(updateColumn);
					}

					updateColumn.Expression = new SqlBinaryExpression(typeof(int), field, "+", new SqlValue(1));

					return newStatment;

				}

				#endregion Update

				#region Insert

				else if (statement.QueryType == QueryType.Insert || statement.QueryType == QueryType.InsertOrUpdate)
				{
					var source          = statement.RequireInsertClause().Into!;
					var descriptor      = MappingSchema.GetEntityDescriptor(source.ObjectType);
					var rowVersion      = descriptor.Columns.SingleOrDefault(c => c.MemberAccessor.MemberInfo.HasAttribute<RowVersionAttribute>());

					if (rowVersion == null)
						return statement;

					var newInsertStatement = Clone(statement);
					var insertClause       = newInsertStatement.RequireInsertClause();
					var field              = insertClause.Into!.FindFieldByMemberName(rowVersion.ColumnName)!;

					var versionColumn = (from i in insertClause.Items
										 let f = i.Column as SqlField
										 where f != null && f.PhysicalName == field.PhysicalName
										 select i).FirstOrDefault();

					// if we do not try to insert version, lets suppose it should be done in database
					if (versionColumn != null)
					{
						versionColumn.Expression = new SqlValue(1);
						return newInsertStatement;
					}
				}
				#endregion Insert

				return statement;
			}
		}

		[AttributeUsage(AttributeTargets.Property)]
		public class RowVersionAttribute: Attribute
		{ }

		[Table("TestTable")]
		public class TestTable
		{
			[Column(Name = "ID", IsPrimaryKey = true, PrimaryKeyOrder = 0, IsIdentity = false)]
			public int ID { get; set; }

			[Column(Name = "Description")]
			public string? Description { get; set; }

			private int _rowVer;

			[Column(Name = "RowVer", Storage = "_rowVer", IsPrimaryKey = true, PrimaryKeyOrder = 1)]
			[RowVersion]
			public int RowVer => _rowVer;
		}

		private InterceptDataConnection _connection = null!;

		[OneTimeSetUp]
		public void SetUp()
		{
			_connection = new InterceptDataConnection(ProviderName.SQLiteClassic, "Data Source=:memory:;");

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

			for (int i = 0; i < 3; i++)
			{
				var row = table.First(t => t.ID == 1);
				row.Description = "Changed desc " + i;

				var result = db.Update(row);

				Assert.That(result, Is.EqualTo(1));

				var updated = table.First(t => t.ID == 1);
				Assert.That(updated.RowVer, Is.EqualTo(row.RowVer + 1));
			}
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

			Assert.That(result, Is.EqualTo(1));

			// 2nd change will fail as the version number is different to the one sent with the update
			row2.Description = "Another change";

			result = db.Update(row1);

			Assert.That(result, Is.EqualTo(0));
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

			Assert.Multiple(() =>
			{
				Assert.That(obj1000, Is.Not.Null);
				Assert.That(obj1001, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(obj1000.RowVer, Is.EqualTo(1));
				Assert.That(obj1001.RowVer, Is.EqualTo(1));
			});

			db.Update(obj1000);

			Assert.Multiple(() =>
			{
				Assert.That(db.Delete(obj1000), Is.EqualTo(0));
				Assert.That(db.Delete(obj1001), Is.EqualTo(1));
			});
		}

		[Test]
		public async Task InsertAndDeleteTestAsync()
		{
			var db    = _connection;
			var table = db.GetTable<TestTable>();

			await db.InsertAsync(new TestTable { ID = 2000, Description = "Delete Candidate 1000" });
			await db.InsertAsync(new TestTable { ID = 2001, Description = "Delete Candidate 1001" });

			var obj2000 = await table.FirstAsync(_ => _.ID == 2000);
			var obj2001 = await table.FirstAsync(_ => _.ID == 2001);

			Assert.Multiple(() =>
			{
				Assert.That(obj2000, Is.Not.Null);
				Assert.That(obj2001, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(obj2000.RowVer, Is.EqualTo(1));
				Assert.That(obj2001.RowVer, Is.EqualTo(1));
			});

			await db.UpdateAsync(obj2000);

			Assert.Multiple(async () =>
			{
				Assert.That(await db.DeleteAsync(obj2000), Is.EqualTo(0));
				Assert.That(await db.DeleteAsync(obj2001), Is.EqualTo(1));
			});
		}

		[Test]
		public void CheckInsertOrUpdate()
		{
			var db     = _connection;
			var table  = db.GetTable<TestTable>();

			var result = db.InsertOrReplace(new TestTable {ID = 3, Description = "Row 3"});

			Assert.Multiple(() =>
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(table.Count(), Is.EqualTo(3));
			});

			var newval = table.First(t => t.ID == 3);

			newval.Description = "Row 3 New description";

			result = db.InsertOrReplace(newval);
			Assert.Multiple(() =>
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(table.Count(), Is.EqualTo(3));
			});
		}
	}
}
