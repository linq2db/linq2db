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
						q2.OtherValue, 
						AnyMember = db.GetTable<OtherClass>().Where(s => s.OtherId > q.Id).Any()
					};

				var query2 = from q in query1
					where q.AnyMember
					select new
					{
						ValidId = q.Id,
						Value = q.OtherValue
					};

//				var query = from q in db.GetTable<SampleClass>()
//					where q.Id > 0
//					select q;

				var parameter = Expression.Parameter(db.GetType());
				var parser = new ModelTranslator(db.MappingSchema, parameter);
				var expression = parser.PrepareExpressionForTranslation(query2.Expression);
				var model = parser.ParseModel(expression);

				var generator = new AstGenerator(db);
				var sql = generator.GenerateStatement(model);

				var sb = new StringBuilder();
				sql.ToString(sb, new Dictionary<IQueryElement, IQueryElement>());

				Console.WriteLine(sb.ToString());
			}
		}		
	}
}
