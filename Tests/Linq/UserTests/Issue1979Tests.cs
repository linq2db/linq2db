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
			public List<TaggingIssue> Tagging { get; set; }


			public static Expression<Func<Issue, TaggingIssue, bool>> TaggingImpl()
			{
				return (y, z) => y.Id == z.TaggableId;
			}
		}

		[Table]
		[InheritanceMapping(Code = "Issue", Type = typeof(TaggingIssue))]
		public class Tagging
		{
			[PrimaryKey]                     public long   Id           { get; set; }
			[Column]                         public int    TagId        { get; set; }
			[Column]                         public int    TaggableId   { get; set; }
			[Column(IsDiscriminator = true)] public string TaggableType { get; set; }

			[Association(QueryExpressionMethod = nameof(TagImpl))]
			public Tag Tag { get; set; }

			public static Expression<Func<Tagging, Tag, bool>> TagImpl()
			{
				return (y, z) => y.TagId == z.Id;
			}
		}

		[Table]
		public class TaggingIssue : Tagging
		{
			[Association(QueryExpressionMethod = nameof(IssueImpl))]
			public Issue Issue { get; set; }

			public static Expression<Func<TaggingIssue, Issue, bool>> IssueImpl()
			{
				return (y, z) => y.TaggableId == z.Id;
			}
		}

		[Table]
		public class Tag
		{
			[Column] public long   Id   { get; set; }
			[Column] public string Name { get; set; }
		}

		[Test]
		public void Test_Linq([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

				var sql = query.ToString();
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
			[Tagging] [t_2]
				INNER JOIN [Tag] [t_1] ON [t_2].[TagId] = [t_1].[Id]
		WHERE
			[t_1].[Name] = 'Visu' AND [t_2].[TaggableId] = [i].[Id] AND
			[t_2].[TaggableType] = 'Issue'
	)
	*/
			}

		}
		[Test]
		public void Test_Associations([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Tag>())
			using (db.CreateLocalTable<Tagging>())
			using (db.CreateLocalTable<Issue>())
			{
				var query = from i in db.GetTable<Issue>()
							where i.Tagging.Any(x => x.Tag.Name == "Visu")
							select i;

				var sql = query.ToString();
				query.ToList();
			}
		}
	}
}
