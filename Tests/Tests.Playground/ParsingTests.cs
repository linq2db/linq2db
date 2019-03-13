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

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<OtherClass>())
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

				var parameter = Expression.Parameter(db.GetType());
				var parser = new ModelTranslator(db.MappingSchema, parameter);
				var expression = parser.PrepareExpressionForTranslation(query2.Expression);
				var model = parser.ParseModel(expression);

				var generator = new QueryGenerator(parser, db);
				var sql = generator.GenerateStatement(model);

				var finalized = db.GetSqlOptimizer().Finalize(sql.Item1);
//				var finalized = sql.Item1;

				var sbRaw = new StringBuilder();
				finalized.ToString(sbRaw, new Dictionary<IQueryElement, IQueryElement>());

				var sqlStr = sbRaw.ToString();
				Console.WriteLine(sqlStr);

				var sb = new StringBuilder();
				db.CreateSqlProvider().BuildSql(0, finalized, sb, 0);

				Console.WriteLine(sb.ToString());
			}
		}		

		[Test]
		public void UnionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var limitedClass = db.GetTable<SampleClass>().Select(c => new SampleClass { Id = c.Value ?? 0, ReferenceId = -1});

				var subQuery =
					db.GetTable<SampleClass>().Where(c => c.Id >= 0)
						.Select(c => new SampleClass { Value = c.Value })
						.Concat(limitedClass);

//				var query1 = subQuery.Select(c => new { c.Id });
				var query1 = subQuery.Select(c => new { c.Value, c.ReferenceId });
//				var query1 = subQuery;

//				var limitedClass = db.GetTable<SampleClass>().Select(c => new { Id = new { c.Id, Value = (int?)null }});
//
//				var query1 =
//					db.GetTable<SampleClass>().Where(c => c.Id >= 0).Select(c => new { Id = new { c.Id, c.Value }})
//						.Union(limitedClass);

				var parameter = Expression.Parameter(db.GetType());
				var parser = new ModelTranslator(db.MappingSchema, parameter);
				var expression = parser.PrepareExpressionForTranslation(query1.Expression);
				var model = parser.ParseModel(expression);

				var generator = new QueryGenerator(parser, db);
				var sql = generator.GenerateStatement(model);

				var finalized = db.GetSqlOptimizer().Finalize(sql.Item1);
//				var finalized = sql.Item1;

				var sbRaw = new StringBuilder();
				finalized.ToString(sbRaw, new Dictionary<IQueryElement, IQueryElement>());

				var sqlStr = sbRaw.ToString();
				Console.WriteLine(sqlStr);
				Console.WriteLine("---------------------------------");

				var sb = new StringBuilder();
				db.CreateSqlProvider().BuildSql(0, finalized, sb, 0);

				Console.WriteLine(sb.ToString());
			}
		}		

	}
}
