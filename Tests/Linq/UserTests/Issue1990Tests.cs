using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1990Tests : TestBase
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
					.Property(x => x.AssignedToId).HasColumnName("assigned_to_id").IsNullable()
					.Association(x => x.Tagging, (y, z) => y.Id == z.TaggableId)
					.Property(x => x.Id).HasColumnName("id").IsPrimaryKey();
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
					.Property(x => x.Id).HasColumnName("id").IsPrimaryKey()
					.Association(x => x.Tag, (y, z) => y.TagId == z.Id)
					.Property(x => x.TagId).HasColumnName("tag_id").IsNullable()
					.Property(x => x.TaggableId).HasColumnName("taggable_id").IsNullable()
					.Property(x => x.TaggableType).HasColumnName("taggable_type").IsNullable();
			}
		}

		public class TaggingIssue : Tagging
		{
			public Issue Issue { get; set; }

			public static void Map(FluentMappingBuilder fmb)
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
				   .Property(x => x.Id).HasColumnName("id").IsPrimaryKey()
				   .Property(x => x.Name).HasColumnName("name").IsNullable();
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();
			Issue.Map(builder);
			Tagging.Map(builder);
			TaggingIssue.Map(builder);
			Tag.Map(builder);
			using (var dc = GetDataContext(context, ms))
			{
				try { dc.DropTable<Issue>(); } catch { }
				try { dc.DropTable<Tagging>(); } catch { }
				try { dc.DropTable<Tag>(); } catch { }
				dc.CreateTable<Issue>();
				dc.CreateTable<Tagging>();
				dc.CreateTable<Tag>();
				var tagname = "";
				var query = from i in dc.GetTable<Issue>()
					join t in (dc.GetTable<Tagging>().Where(x => x.TaggableType == "Issue")) on i.Id equals t.TaggableId into tj
					from t in tj.DefaultIfEmpty()
					join tg in dc.GetTable<Tag>() on t.TagId equals tg.Id into tgj
					from tg in tgj.DefaultIfEmpty()
					where
						i.Id == 478356 
						&& !string.IsNullOrEmpty(tagname) ? tg.Name == tagname : true
					select new { Issue = i };
				query.ToList();
				var sql = ((DataConnection)dc).LastQuery;

				Assert.IsTrue(sql.Contains("478356"));
			}
		}
	}
}
