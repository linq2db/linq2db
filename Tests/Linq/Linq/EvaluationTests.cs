using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class EvaluationTests : TestBase
	{
		[Test]
		public void Evaluate_RemappedTimeSpan([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			const string CONFIG = "EvaluationTests_Evaluate_RemappedTimeSpan";

			var ms = new MappingSchema(CONFIG);
			LinqToDB.Linq.Expressions.MapMember<TimeSpan>(CONFIG, ts => ts.Ticks, (Expression<Func<TimeSpan, long>>)(ts => ToTicks(ts)));
			ms.SetDataType(typeof(TimeSpan), DataType.Int64);

			using var db = GetDataContext(context, ms);

			db.Person.Any(p => TimeSpan.Zero > Sql.AsSql(FromTicks((long)(((5.988M)) * ((new TimeSpan(88888888L)).Ticks)))));
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static TimeSpan FromTicks(long ticks)
		{
			return TimeSpan.FromTicks(ticks);
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static long ToTicks(TimeSpan _)
		{
			throw new InvalidOperationException();
		}

		[Test]
		public void Evaluate_BooleanExpression([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			const string GUID = "6a2f3fd40171402193f78b1a42b34e3b";
			using var db = GetDataContext(context);

			var result = db.Person.Any(p => NullableGuid(new Guid(GUID)) == null || new Guid(GUID) != NullableGuid(new Guid(GUID)));

			Assert.That(result, Is.False);
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static Guid? NullableGuid(Guid? value)
		{
			return value;
		}
	}
}
