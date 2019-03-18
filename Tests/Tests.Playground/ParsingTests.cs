using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqToDB;
using LinqToDB.Linq.Generator;
using LinqToDB.Linq.Parser;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Playground
{
	public class ParsingTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column, PrimaryKey] public int Id    { get; set; }
			[Column] public int? Value { get; set; }
			[Column] public int? ReferenceId { get; set; }
		}

		[Table]
		class OtherClass
		{
			[Column, PrimaryKey] public int OtherId    { get; set; }
			[Column] public int? OtherValue { get; set; }
		}

		class ParentSource
		{
			[PrimaryKey]
			public int PkParentId { get; set; }

			[Association(ThisKey = nameof(PkParentId), OtherKey = nameof(ChildSource.ParentId))]
			public IEnumerable<ChildSource> Children { get; set; }
		}

		class ChildSource
		{
			[PrimaryKey]
			public int PkChildId { get; set; }

			public int ParentId { get; set; }
			public int? AddressId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(ParentSource.PkParentId), CanBeNull = false)]
			public ParentSource Parent { get; set; }

			[Association(ThisKey = nameof(AddressId), OtherKey = nameof(AddressSource.PkAddressId))]
			public AddressSource Address { get; set; }

			public bool IsDeleted { get; set; }
		}

		class AddressSource
		{
			[PrimaryKey]
			public int PkAddressId { get; set; }

			public string City { get; set; }
		}

		private static void ProvideParsing<T>(IQueryable<T> query, ITestDataContext db)
		{
			var parameter = Expression.Parameter(db.GetType());
			var parser = new ModelTranslator(db.MappingSchema, parameter);
			var expression = parser.PrepareExpressionForTranslation(query.Expression);
			var model = parser.ParseModel(expression);

			var optimizer = new ModelOptimizer(new OptimizationFlags { CountFilterSupported = false });
			model = optimizer.OptimizeModel(model);

			var generator = new QueryGenerator(parser.TranslationContext, db);
			var sql = generator.GenerateStatement(model);

			var finalized = db.GetSqlOptimizer().Finalize(sql.Item1);

			var sbRaw = new StringBuilder();
			finalized.ToString(sbRaw, new Dictionary<IQueryElement, IQueryElement>());

			var sqlStr = sbRaw.ToString();
			Console.WriteLine(sqlStr);
			Console.WriteLine("---------------------------------");

			var sb = new StringBuilder();
			db.CreateSqlProvider().BuildSql(0, finalized, sb, 0);

			Console.WriteLine(sb.ToString());
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var limitedClass = db.GetTable<SampleClass>().Select(c => new SampleClass { Value = c.Value, Id = c.Id });

				var query1 =
					from s1 in db.GetTable<SampleClass>()
					from s2 in limitedClass.Where(ss => ss.Value == s1.Value)
					join o1 in db.GetTable<OtherClass>() on new { s1.Id, s1.Value } equals new
						{ Id = o1.OtherId, Value = o1.OtherValue }
					where s1.Id > 0
					select new
					{
						s1.Id,
						CalcValue = (1 + s1.Id) * 2,
						SubComplex = new
						{
							o1.OtherValue
						}
					};

				var query2 = from q in query1
					select new
					{
						q.CalcValue,
						Complex = new
						{
							ValidId = q.Id,
							Value = q.SubComplex.OtherValue
						},
					};

//				var query = from q in db.GetTable<SampleClass>()
//					where q.Id > 0
//					select q;

				ProvideParsing(query2, db);
			}
		}		

		[Test]
		public void UnionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
/*				var limitedClass = db.GetTable<SampleClass>().Select(c => new SampleClass { Id = c.Value ?? 0, ReferenceId = -1});

				var subQuery =
					db.GetTable<SampleClass>().Where(c => c.Id >= 0)
						.Select(c => new SampleClass { Value = c.Value })
						.Concat(limitedClass);

//				var query1 = subQuery.Select(c => new { c.Id });
				var query1 = subQuery.Select(c => new { c.Value, c.ReferenceId });
//				var query1 = subQuery;

				ProvideParsing(query1, db);
				*/


				var limitedClass2 = db.GetTable<SampleClass>().Select(c => new { CompId = new { c.Id, Value = (int?)null }});

				var query2 =
					db.GetTable<SampleClass>().Where(c => c.Id >= 0).Select(c => new { CompId = new { c.Id, c.Value }})
						.Union(limitedClass2);

				ProvideParsing(query2, db);
			}
		}		


		[Test]
		public void GroupByTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<SampleClass>();

				ProvideParsing(query, db);
			}
		}

		[Test]
		public void CountInSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<SampleClass>()
					.Select(c =>
						new
						{
							Count = db.GetTable<SampleClass>()
								.Where(t => t.Id > 0)
								.Count(_ => _.ReferenceId != null)
						}
					);

				ProvideParsing(query, db);
			}
		}

		[Test]
		public void TakeTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var take = 5;
				var query = db.GetTable<SampleClass>().Skip(2).Take(take);

				ProvideParsing(query, db);
			}
		}

		[Test]
		public void AssociationOneToManyTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from p in db.GetTable<ParentSource>()
					from c in p.Children
					select new
					{
						p,
						c
					};

				ProvideParsing(query, db);
			}
		}

		[Test]
		public void AssociationManyToOneTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from c in db.GetTable<ChildSource>()
					select new
					{
						c,
						c.Address.City
					};

				ProvideParsing(query, db);
			}
		}

		[Test]
		public void AssociationProjectionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from p in db.GetTable<ParentSource>()
					from c in p.Children 
					where c.Address.City == "NY"
					select new
					{
						c.Parent.PkParentId,
						c,
						c.Address.City,
						c.Address,
						p
					}; 

				ProvideParsing(query, db);
			}
		}

	}
}
