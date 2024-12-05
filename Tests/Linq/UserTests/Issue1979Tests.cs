using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1979Tests : TestBase
	{
		[Table]
		public class Issue
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(QueryExpressionMethod = nameof(TaggingImpl))]
			public List<TaggingIssue> Tagging { get; set; } = null!;


			public static Expression<Func<Issue, IDataContext, IQueryable<TaggingIssue>>> TaggingImpl()
			{
				return (y, db) => db.GetTable<TaggingIssue>().Where(_ => y.Id == _.TaggableId);
			}
		}

		[Table]
		[InheritanceMapping(Code = "Issue", Type = typeof(TaggingIssue))]
		public class Tagging
		{
			[PrimaryKey]                     public long    Id           { get; set; }
			[Column]                         public int     TagId        { get; set; }
			[Column]                         public int     TaggableId   { get; set; }
			[Column(IsDiscriminator = true)] public string? TaggableType { get; set; }

			[Association(QueryExpressionMethod = nameof(TagImpl))]
			public Tag Tag { get; set; } = null!;

			public static Expression<Func<Tagging, IDataContext, IQueryable<Tag>>> TagImpl()
			{
				return (y, db) => db.GetTable<Tag>().Where(_ => y.TagId == _.Id).Take(1);
			}
		}

		[Table]
		public class TaggingIssue : Tagging
		{
			[Association(QueryExpressionMethod = nameof(IssueImpl))]
			public Issue Issue { get; set; } = null!;

			public static Expression<Func<TaggingIssue, IDataContext, IQueryable<Issue>>> IssueImpl()
			{
				return (y, db) => db.GetTable<Issue>().Where(_ => y.TaggableId == _.Id).Take(1);
			}
		}

		[Table]
		public class Tag
		{
			[Column] public long    Id   { get; set; }
			[Column] public string? Name { get; set; }
		}

		[Test]
		public void Test_Linq([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Tag>())
			using (db.CreateLocalTable<Tagging>())
			using (db.CreateLocalTable<Issue>())
			{
				var tagFilter = from ti in db.GetTable<TaggingIssue>()
								join t in db.GetTable<Tag>() on ti.TagId equals t.Id
								where t.Name == "Visu"
								select ti;

				var query = from i in db.GetTable<Issue>()
							where tagFilter.Where(t => t.TaggableId == i.Id).Any()
							select i;

				var sql = query.ToSqlQuery().Sql;
				query.ToList();

				/*
				 * SQL:
SELECT
	[i].[Id]
FROM
	[Issue] [i]
WHERE
	EXISTS(
		SELECT
			*
		FROM
			[Tagging] [t_1]
				INNER JOIN [Tag] [t] ON [t_1].[TagId] = [t].[Id]
		WHERE
			[t].[Name] = N'Visu' AND [t_1].[TaggableId] = [i].[Id] AND
			[t_1].[TaggableType] = N'Issue'
	)
	*/
			}

		}
		[Test]
		public void Test_Associations([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Tag>())
			using (db.CreateLocalTable<Tagging>())
			using (db.CreateLocalTable<Issue>())
			{
				var query = from i in db.GetTable<Issue>()
							where i.Tagging.Any(x => x.Tag.Name == "Visu")
							select i;

				var sql = query.ToSqlQuery().Sql;
				query.ToList();

				/*

SELECT
	[i].[Id]
FROM
	[Issue] [i]
WHERE
	EXISTS(
		SELECT
			*
		FROM
			[Tagging] [_1]
				OUTER APPLY (
					SELECT
						[t1].[Name]
					FROM
						(
							SELECT
								[_].[Name],
								ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) as [RN]
							FROM
								[Tag] [_]
							WHERE
								Convert(BigInt, [_1].[TagId]) = [_].[Id]
						) [t1]
					WHERE
						[t1].[RN] <= @take
				) [a_Tag]
		WHERE
			[_1].[TaggableType] = N'Issue' AND [i].[Id] = [_1].[TaggableId] AND
			[a_Tag].[Name] = N'Visu'
	)
				 */
			}
		}
	}
}
