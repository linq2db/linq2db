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
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Table]
		class OtherClass
		{
			[Column] public int OtherId    { get; set; }
			[Column] public int OtherValue { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<OtherClass>())
			{
				var query1 = from q in db.GetTable<SampleClass>()
					join q2 in db.GetTable<OtherClass>() on q.Id equals q2.OtherId
					where q.Id > 0
					select new
					{
						q.Id, 
						CalcValue = (1 + q.Id) * 2,
						SubComplex = new
						{
							q2.OtherValue
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
						}
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
	}
}
