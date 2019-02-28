using System;
using System.Collections.Generic;
using System.Linq;
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

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var query = from q in db.GetTable<SampleClass>()
					where q.Id > 0
					select q;

				var parser = new ModelParser();
				var model = parser.ParseModel(query.Expression);

				var generator = new AstGenerator(db);
				var sql = generator.GenerateStatement(model);

				var sb = new StringBuilder();
				sql.ToString(sb, new Dictionary<IQueryElement, IQueryElement>());

				Console.WriteLine(sb.ToString());
			}
		}		
	}
}
