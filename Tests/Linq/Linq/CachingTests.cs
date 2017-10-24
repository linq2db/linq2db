using System;
using System.Linq;
using System.Data.Linq;
using LinqToDB.Expressions;
using NUnit.Framework;
using Tests.DataProvider;
using Tests.Model;

namespace Tests.Linq
{
	using LinqToDB;

	public class CachingTests: TestBase
	{
		class AggregateFuncBuilder: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				builder.AddExpression("funcName",  builder.GetValue<string>("funcName"));
				builder.AddExpression("fieldName", builder.GetValue<string>("fieldName"));
			}
		}

		[Sql.Extension("{funcName}({fieldName})", BuilderType = typeof(AggregateFuncBuilder), ServerSideOnly = true)]
		static double AggregateFunc([SqlEvaluate] string funcName, [SqlEvaluate] string fieldName)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void TesEvaluateAttribute(
			[DataSources] string context,
			[Values(
				"MIN",
				"MAX",
				"AVG",
				"COUNT"
			)] string funcName,
			[Values(
				nameof(ALLTYPE.ID),
				nameof(ALLTYPE.BIGINTDATATYPE),
				nameof(ALLTYPE.SMALLINTDATATYPE),
				nameof(ALLTYPE.DECIMALDATATYPE),
				nameof(ALLTYPE.DECFLOATDATATYPE),
				nameof(ALLTYPE.INTDATATYPE),
				nameof(ALLTYPE.REALDATATYPE),
				nameof(ALLTYPE.TIMEDATATYPE)
			)] string fieldName)
		{
			using (var db = GetDataContext(context))
			{
				var query = 
					from t in db.GetTable<ALLTYPE>()
					from c in db.GetTable<Child>()
					select new
					{
						Aggregate = AggregateFunc(funcName, fieldName)
					};

				var sql = query.ToString();
				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring(funcName).And.Contains(fieldName));
			}
		}
	}
}