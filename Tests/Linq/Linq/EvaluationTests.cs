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
			throw new ServerSideOnlyException(nameof(ToTicks));
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

		[Table("ClientEvaluatedRecord")]
		public class ClientEvaluatedRecord
		{
			[Column(IsPrimaryKey = true)]
			public int ID { get; set; }
		}

		private static int GetFixedValue()
		{
			return 5;
		}

		private static Tuple<int, int> GetFixedValueTuple()
		{
			return Tuple.Create(5, 3);
		}

		public class EvaluatedNode
		{
			public EvaluatedNode? Child;

			public int Id;

			public int GetId() => Id;
			public EvaluatedNode? GetChild() => Child;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1316")]
		public void CapturedObjectChainWithMethodCalls([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.CreateLocalTable<ClientEvaluatedRecord>();
			db.Insert(new ClientEvaluatedRecord { ID = 5 }, __.TableName);

			var obj = new EvaluatedNode()
			{
				Child = new EvaluatedNode()
				{
					Child = new EvaluatedNode()
					{
						Child = new EvaluatedNode()
						{
							Id = 5
						}
					}
				}
			};

			db.GetTable<ClientEvaluatedRecord>().TableName(__.TableName).Where(_ => _.ID == obj.Child.GetChild()!.Child!.GetId()).Single();
			db.GetTable<ClientEvaluatedRecord>().TableName(__.TableName).Where(_ => _.ID == obj.Child.GetChild()!.Child!.Id).Single();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1316")]
		public void CapturedObjectMemberNavigation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.CreateLocalTable<ClientEvaluatedRecord>();
			db.Insert(new ClientEvaluatedRecord { ID = 5 }, __.TableName);

			var obj = new EvaluatedNode
			{
				Child = new EvaluatedNode
				{
					Id = 5
				}
			};

			var ___ = db.GetTable<ClientEvaluatedRecord>()
					.TableName(__.TableName)
					.Where(_ => _.ID == obj.Child.Id)
					.Single();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1316")]
		public void CapturedMethodResultMember([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.CreateLocalTable<ClientEvaluatedRecord>();
			db.Insert(new ClientEvaluatedRecord { ID = 5 }, __.TableName);

			var ___ = db.GetTable<ClientEvaluatedRecord>()
					.TableName(__.TableName)
					.Where(_ => _.ID == GetFixedValueTuple().Item1)
					.Single();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1316")]
		public void CapturedMethodCall([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.CreateLocalTable<ClientEvaluatedRecord>();
			db.Insert(new ClientEvaluatedRecord { ID = 5 }, __.TableName);

			var ___ = db.GetTable<ClientEvaluatedRecord>()
					.TableName(__.TableName)
					.Where(_ => _.ID == GetFixedValue())
					.Single();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1316")]
		public void CapturedArrayWithLinqPredicate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var __ = db.CreateLocalTable<ClientEvaluatedRecord>();
			db.Insert(new ClientEvaluatedRecord { ID = 5 }, __.TableName);

			var ids = new[] { 1, 2, 3, 4, 5, 6 };

			var ___ = db.GetTable<ClientEvaluatedRecord>()
					.TableName(__.TableName)
					.Where(_ => ids.Where(id => id > 3).Contains(_.ID))
					.Single();
		}
	}
}
