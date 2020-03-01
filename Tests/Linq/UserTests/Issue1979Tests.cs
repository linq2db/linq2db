using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1979Tests : TestBase
	{
		public class Issue
		{
			public int Id { get; set; }

			public List<TaggingIssue> Tagging { get; set; }

			public int AssignedToId { get; set; }

			public static void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<Issue>()
					.HasTableName("issues")
						.Property(x => x.AssignedToId)
							.HasColumnName("assigned_to_id")
							.IsNullable()
						.Association(x => x.Tagging, (y, z) => y.Id == z.TaggableId)
						.Property(x => x.Id)
							.HasColumnName("id")
							.IsPrimaryKey();
			}
		}

		public class Tagging
		{
			public long Id { get; set; }

			public int TagId { get; set; }

			public Tag Tag { get; set; }

			public int TaggableId { get; set; }

			public string TaggableType { get; set; }

			public static void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<Tagging>()
					.HasTableName("taggings")
					.Inheritance(x => x.TaggableType, "Issue", typeof(TaggingIssue))
						.Property(x => x.Id)
							.HasColumnName("id")
							.IsPrimaryKey()
						.Association(x => x.Tag, (y, z) => y.TagId == z.Id)
						.Property(x => x.TagId)
							.HasColumnName("tag_id")
							.IsNullable()
						.Property(x => x.TaggableId)
							.HasColumnName("taggable_id")
							.IsNullable()
						.Property(x => x.TaggableType)
							.HasColumnName("taggable_type")
							.IsNullable();
			}
		}

		public class TaggingIssue : Tagging
		{
			public Issue Issue { get; set; }

			public static new void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<TaggingIssue>()
					.Association(x => x.Issue, (y, z) => y.TaggableId == z.Id);
			}
		}

		public class Tag
		{
			public long Id { get; set; }

			public string Name { get; set; }

			public static void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<Tag>()
				   .HasTableName("tags")
						.Property(x => x.Id)
							.HasColumnName("id")
							.IsPrimaryKey()
						.Property(x => x.Name)
							.HasColumnName("name")
							.IsNullable();
			}
		}

		public class User
		{
			public int Id { get; set; }

			public static void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<User>()
					.HasTableName("users")
						.Property(x => x.Id)
							.HasColumnName("id")
							.IsPrimaryKey();
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms      = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

			Issue       .Map(builder);
			Tagging     .Map(builder);
			TaggingIssue.Map(builder);
			Tag         .Map(builder);

			using (var db = GetDataContext(context, ms))
			{
				var query = from i in db.GetTable<Issue>()
							join u in db.GetTable<User>() on i.AssignedToId equals u.Id into uj
							from u in uj.DefaultIfEmpty()
							where i.Tagging.Any(x => x.Tag.Name == "Visu")
							select new { Issue = i, User = u };

				var lst = query.ToList();

				/*
				 * Expected Query
				 *
				 *   SELECT issues.*, users.* FROM ISSUE
				 *   JOIN users ON issues.assigned_to_id = users.id
				 *   INNER JOIN taggings ON issues.id = taggings.taggable_id
				 *   INNER JOIN tags ON tag_id.id = tags.id  
				 *   WHERE taggings.taggable_type = 'Issue' AND tag.name = 'Visu'
				 * 
				 */
			}
		}
	}
}
