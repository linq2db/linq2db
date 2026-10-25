using System;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Mapping
{
	public class UseMappingSchemaTests : TestBase
	{
		[UsedImplicitly]
		class UseMappingSchemaTestTable
		{
			[PrimaryKey]
			public int Field1 { get; set; }
			public int Field2 { get; set; }
			[Column(Name = "Column3")]
			public int Field3 { get; set; }
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<UseMappingSchemaTestTable>()
					.Property(e => e.Field1)
						.HasColumnName("Column1")
				.Build()
				.MappingSchema))
			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Column1"));
			}

			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			using var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>();

			_ = tmp.ToList();

			Assert.That(LastQuery, Does.Contain("Field1"));

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<UseMappingSchemaTestTable>()
					.HasTableName("UseMappingSchemaTestTable2")
					.Property(e => e.Field1)
						.HasColumnName("Column1")
				.Build()
				.MappingSchema))
			{
				using var tmp2 = db.CreateLocalTable<UseMappingSchemaTestTable>();

				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));

				_ = tmp2.ToList();

				Assert.That(LastQuery, Does.Contain("Column1"));

				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}

			using var tmp3 = db.CreateLocalTable<UseMappingSchemaTestTable>("UseMappingSchemaTestTable3");

			_ = tmp.ToList();

			Assert.That(LastQuery, Does.Contain("Field1"));

			_ = tmp3.ToList();

			Assert.That(LastQuery, Does.Contain("Field1"));
		}

		[Table("SourceSchemaRow")]
		sealed class SourceSchemaRow
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column]     public int     Value { get; set; }
			[Column]     public string? Name  { get; set; }
		}

		static MappingSchema Scope(System.Action<EntityMappingBuilder<SourceSchemaRow>> configure)
		{
			var builder = new FluentMappingBuilder(new());

			configure(builder.Entity<SourceSchemaRow>());

			return builder.Build().MappingSchema;
		}

		// A handle created before the scope keeps its snapshot: the scope's SkipOnInsert must not drop the column.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void EntityInsertFollowsHandleSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable<SourceSchemaRow>();

			using (db.UseMappingSchema(Scope(e => e.Property(p => p.Value).HasSkipOnInsert())))
			{
				tmp.Insert(new SourceSchemaRow { Id = 1, Value = 5, Name = "a" }, b => b);
			}

			tmp
				.Single()
				.Value
				.ShouldBe(5);
		}

		// The scope's extra primary key must not reach the WHERE of an update through a pre-scope handle.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void EntityUpdateFollowsHandleSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new SourceSchemaRow { Id = 1, Value = 5, Name = "a" } });

			using (db.UseMappingSchema(Scope(e => e.Property(p => p.Value).IsPrimaryKey())))
			{
				tmp.Update(new SourceSchemaRow { Id = 1, Value = 6, Name = "b" }, b => b);
			}

			tmp
				.Single()
				.Value
				.ShouldBe(6);
		}

		// Upsert matches on the handle's key and inserts the handle's columns.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void UpsertFollowsHandleSchema([InsertOrUpdateDataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new SourceSchemaRow { Id = 1, Value = 5, Name = "a" } });

			using (db.UseMappingSchema(Scope(e => e.Property(p => p.Value).IsPrimaryKey())))
			{
				tmp.Upsert(new SourceSchemaRow { Id = 1, Value = 6, Name = "b" });
			}

			var rows = tmp
				.OrderBy(r => r.Id)
				.ToList();

			rows.Count.ShouldBe(1);
			rows[0].Value.ShouldBe(6);
		}

		// Merge's default insert setter is built from the target's columns, so the scope's SkipOnInsert must not apply.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void MergeDefaultInsertFollowsHandleSchema([MergeDataContextSource] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable<SourceSchemaRow>();

			using (db.UseMappingSchema(Scope(e => e.Property(p => p.Value).HasSkipOnInsert())))
			{
				tmp
					.Merge()
					.Using(new[] { new SourceSchemaRow { Id = 1, Value = 5, Name = "a" } })
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();
			}

			tmp
				.Single()
				.Value
				.ShouldBe(5);
		}

		[Table("Child")]
		sealed class ChildWithoutAssociation
		{
			[Column]    public int           ParentID { get; set; }
			[Column]    public int           ChildID  { get; set; }
			[NotColumn] public Model.Parent? Parent   { get; set; }
		}

		[Table("Child")]
		sealed class ChildWithAlias
		{
			[Column]    public int ParentID { get; set; }
			[Column]    public int ChildID  { get; set; }
			[NotColumn] public int Code     { get; set; }
		}

		// The handles below are created inside the scope and queried after it: the snapshot then carries a facet the
		// data context's schema no longer has, so a step that reads the live schema loses it.

		// An association declared only in the handle's schema is detected and resolved through it.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void AssociationFollowsHandleSchema([DataSources] string context)
		{
			using var db = GetDataContext(context);

			ITable<ChildWithoutAssociation> children;

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<ChildWithoutAssociation>()
					.Association(c => c.Parent, c => c.ParentID, p => p!.ParentID)
				.Build()
				.MappingSchema))
			{
				children = db.GetTable<ChildWithoutAssociation>();
			}

			// Not AssertQuery: its in-memory side would read c.Parent, which only the handle's schema maps.
			var expected = Child
				.Select(c => new { c.ChildID, Parent.Single(p => p.ParentID == c.ParentID).Value1 })
				.OrderBy(r => r.ChildID)
				.ToList();

			var actual = children
				.Select(c => new { c.ChildID, c.Parent!.Value1 })
				.OrderBy(r => r.ChildID)
				.ToList();

			actual.ShouldBe(expected);
		}

		// In a join the association belongs to the second handle, whose schema differs from the join's own.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void AssociationOnSecondHandleFollowsItsSchema([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var parents = db.Parent;

			ITable<ChildWithoutAssociation> children;

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<ChildWithoutAssociation>()
					.Association(c => c.Parent, c => c.ParentID, p => p!.ParentID)
				.Build()
				.MappingSchema))
			{
				children = db.GetTable<ChildWithoutAssociation>();
			}

			// Not AssertQuery: its in-memory side would read c.Parent, which only the handle's schema maps.
			var expected =
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID
				orderby c.ChildID
				select new { c.ChildID, p.Value1 };

			var query =
				from p in parents
				join c in children on p.ParentID equals c.ParentID
				orderby c.ChildID
				select new
				{
					c.ChildID,
					c.Parent!.Value1,
				};

			query
				.ToList()
				.ShouldBe(expected.ToList());
		}

		// A member alias declared only in the handle's schema resolves to its column.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void AliasFollowsHandleSchema([DataSources] string context)
		{
			using var db = GetDataContext(context);

			ITable<ChildWithAlias> children;

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<ChildWithAlias>()
					.Property(c => c.Code)
						.IsAlias(c => c.ChildID)
				.Build()
				.MappingSchema))
			{
				children = db.GetTable<ChildWithAlias>();
			}

			children
				.Where(c => c.Code == 11)
				.Select(c => c.ChildID)
				.ToList()
				.ShouldBe([11]);
		}

		[Table("SourceSchemaShape")]
		sealed class ShapeRow
		{
			[PrimaryKey] public int    Id   { get; set; }
			[Column]     public string Kind { get; set; } = null!;
		}

		// Integer codes on a string column: turning a code into the column's value is a schema conversion.
		[Table("SourceSchemaShape")]
		[InheritanceMapping(Code = 1, Type = typeof(ShapeCircle))]
		[InheritanceMapping(Code = 2, Type = typeof(ShapeSquare))]
		abstract class Shape
		{
			[PrimaryKey]                    public int    Id   { get; set; }
			[Column(IsDiscriminator = true)] public string Kind { get; set; } = null!;
		}

		sealed class ShapeCircle : Shape
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(Model.Parent.ParentID))]
			public Model.Parent? Parent { get; set; }
		}

		sealed class ShapeSquare : Shape;

		static ShapeRow[] ShapeRows => [new() { Id = 1, Kind = "C1" }, new() { Id = 2, Kind = "S2" }];

		static MappingSchema ShapeCodes()
		{
			var schema = new MappingSchema();
			schema.SetConvertExpression<int, string>(code => code == 1 ? "C1" : "S2");
			return schema;
		}

		public enum ShapeKind
		{
			Circle,
			Square,
		}

		[Table("SourceSchemaShape")]
		sealed class ShapeCodeRow
		{
			[PrimaryKey] public int Id   { get; set; }
			[Column]     public int Kind { get; set; }
		}

		// Enum codes on an int column without MapValue: the code's value comes from the schema's enum conversion.
		[Table("SourceSchemaShape")]
		[InheritanceMapping(Code = ShapeKind.Circle, Type = typeof(EnumCircle))]
		[InheritanceMapping(Code = ShapeKind.Square, Type = typeof(EnumSquare))]
		abstract class EnumShape
		{
			[PrimaryKey]                     public int       Id   { get; set; }
			[Column(IsDiscriminator = true)] public ShapeKind Kind { get; set; }
		}

		sealed class EnumCircle : EnumShape;

		sealed class EnumSquare : EnumShape;

		// OfType filters on the discriminator value the handle's schema converts the code to.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void OfTypeFollowsHandleSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new ShapeCodeRow { Id = 1, Kind = 101 }, new ShapeCodeRow { Id = 2, Kind = 102 } });

			var schema = new MappingSchema();
			schema.SetConvertExpression<ShapeKind, int>(kind => (int)kind + 101);

			ITable<EnumShape> shapes;

			using (db.UseMappingSchema(schema))
			{
				shapes = db.GetTable<EnumShape>();
			}

			shapes
				.OfType<EnumCircle>()
				.Select(s => s.Id)
				.ToList()
				.ShouldBe([1]);
		}

		// In a join the type test belongs to the second handle, whose schema differs from the join's own.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void TypeTestOnSecondHandleFollowsItsSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(ShapeRows);

			var parents = db.Parent;

			ITable<Shape> shapes;

			using (db.UseMappingSchema(ShapeCodes()))
			{
				shapes = db.GetTable<Shape>();
			}

			var query =
				from p in parents
				join s in shapes on p.ParentID equals s.Id
				where s is ShapeCircle
				select s.Id;

			query
				.ToList()
				.ShouldBe([1]);
		}

		[Table("SourceSchemaShape")]
		abstract class PlainShape
		{
			[PrimaryKey] public int    Id   { get; set; }
			[Column]     public string Kind { get; set; } = null!;
		}

		sealed class PlainCircle : PlainShape
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(Model.Parent.ParentID))]
			public Model.Parent? Parent { get; set; }
		}

		sealed class PlainSquare : PlainShape;

		// An association declared on a derived type joins on that type's discriminator, which only the handle's
		// schema knows here.
		[ActiveIssue(Configuration = TestProvName.AllAccess, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}[1, null]{1}but was",
			Details = "no-issue: Access filters the outer row on an ON predicate that references only the outer side of a LEFT JOIN; the generated SQL keeps it in ON.")]
		[ActiveIssue(6148, Configuration = TestProvName.AllYdb, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}[1, null]{1}but was",
			Details = "OptimizeJoinConditions moves the LEFT JOIN's outer-only discriminator predicate from ON to WHERE.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void DerivedAssociationFollowsHandleSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(ShapeRows);

			ITable<PlainShape> shapes;

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<PlainShape>()
					.Inheritance(s => s.Kind, "C1", typeof(PlainCircle))
					.Inheritance(s => s.Kind, "S2", typeof(PlainSquare))
				.Build()
				.MappingSchema))
			{
				shapes = db.GetTable<PlainShape>();
			}

			shapes
				.Select(s => new { s.Id, ParentID = (int?)((PlainCircle)s).Parent!.ParentID })
				.OrderBy(x => x.Id)
				.Select(x => x.ParentID)
				.ToList()
				.ShouldBe([1, null]);
		}

		// A parameter compared with a column of another type is converted by the handle's schema.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void ParameterConversionFollowsHandleSchema([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var schema = new MappingSchema();
			schema.SetConvertExpression<long, int>(value => (int)value + 1);

			ITable<Model.Parent> parents;

			using (db.UseMappingSchema(schema))
			{
				parents = db.GetTable<Model.Parent>();
			}

			var id = 1L;

			parents
				.Where(p => p.ParentID == id)
				.Select(p => p.ParentID)
				.ToList()
				.ShouldBe([2]);
		}

		public sealed class Token
		{
			public string Text { get; set; } = null!;
		}

		// A value with no column to take its conversion from is bound by the handle's schema. The base schema reads it
		// back on both arms; only the binding conversion differs between the base and the scope.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void UnboundParameterFollowsHandleSchema([DataSources] string context)
		{
			var baseSchema = new MappingSchema();
			baseSchema.SetConvertExpression<Token, DataParameter>(t => new DataParameter { Value = "A" + t.Text, DataType = DataType.NVarChar });
			baseSchema.SetConvertExpression<string, Token>(s => new Token { Text = s });

			var scopeSchema = new MappingSchema();
			scopeSchema.SetConvertExpression<Token, DataParameter>(t => new DataParameter { Value = "B" + t.Text, DataType = DataType.NVarChar });

			using var db = GetDataContext(context, o => o.UseMappingSchema(baseSchema));

			ITable<Model.Parent> parents;

			using (db.UseMappingSchema(scopeSchema))
			{
				parents = db.GetTable<Model.Parent>();
			}

			var token = new Token { Text = "x" };

			parents
				.Where(p => p.ParentID == 1)
				.Select(p => Sql.AsSql(token))
				.ToList()
				.Select(t => t.Text)
				.ShouldBe(["Bx"]);
		}

		// FromSql text is provider SQL: unquoted identifiers resolve only where the table name is case-insensitive.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void FromSqlFollowsHandleSchema([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer, ProviderName.DuckDB)] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new SourceSchemaRow { Id = 1, Value = 5, Name = "a" } });

			var query = db.FromSql<SourceSchemaRow>("SELECT Id, Value, Name FROM SourceSchemaRow");

			using (db.UseMappingSchema(Scope(e => e.Property(p => p.Value).HasColumnName("Value2"))))
			{
				query
					.Single()
					.Value
					.ShouldBe(5);
			}
		}

		// FromSql text is provider SQL (see above). Its arguments convert under the same schema as its columns.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void FromSqlArgumentFollowsHandleSchema([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer, ProviderName.DuckDB)] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new ShapeCodeRow { Id = 1, Kind = 101 }, new ShapeCodeRow { Id = 2, Kind = 102 } });

			var schema = new MappingSchema();
			schema.SetConvertExpression<ShapeKind, int>(kind => (int)kind + 101);

			var kind = ShapeKind.Circle;

			IQueryable<ShapeCodeRow> query;

			using (db.UseMappingSchema(schema))
			{
				query = db.FromSql<ShapeCodeRow>($"SELECT Id, Kind FROM SourceSchemaShape WHERE Kind = {kind}");
			}

			query
				.Select(r => r.Id)
				.ToList()
				.ShouldBe([1]);
		}

		// CreateTempTable with setTable swaps the data context's schema while the temp table lives, so a handle
		// created before it keeps its own key.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void EntityUpdateFollowsHandleSchemaUnderTempTable([DataSources(false)] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(new[] { new SourceSchemaRow { Id = 1, Value = 5, Name = "a" } });

			using (db.CreateTempTable(Array.Empty<SourceSchemaRow>(), e => e.Property(p => p.Value).IsPrimaryKey(), tableName: "SourceSchemaRowTemp", tableOptions: TableOptions.NotSet))
			{
				tmp.Update(new SourceSchemaRow { Id = 1, Value = 6, Name = "b" }, b => b);
			}

			tmp
				.Single()
				.Value
				.ShouldBe(6);
		}

		// A reference member projected through an as-cast tests the type through the discriminator conversion.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5951")]
		public void TypeAsFollowsHandleSchema([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable(ShapeRows);

			ITable<Shape> shapes;

			using (db.UseMappingSchema(ShapeCodes()))
			{
				shapes = db.GetTable<Shape>();
			}

			shapes
				.OrderBy(s => s.Id)
				.Select(s => s as ShapeCircle)
				.Select(c => (string?)c!.Kind)
				.ToList()
				.ShouldBe(["C1", null]);
		}
	}
}
