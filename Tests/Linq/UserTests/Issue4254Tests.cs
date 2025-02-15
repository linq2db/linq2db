using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4254Tests : TestBase
	{
		// verifies that the proper SQL parameter name is used when constructing the query
		[Test]
		public void VerifyParameterNameIsCorrect([IncludeDataSources(TestProvName.AllPostgreSQL95Plus, TestProvName.AllSqlServer2008Plus)] string context)
		{
			// prep tables
			{
				using var db = GetDataConnection(context);
				// drop tables if they already exist
				db.GetTable<MediaItemToMediaItemCategory>().Drop(false);
				db.GetTable<MediaItemUserShare>().Drop(false);
				db.GetTable<MediaItem>().Drop(false);
				// create tables
				db.CreateTable<MediaItem>();
				db.CreateTable<MediaItemUserShare>();
				db.CreateTable<MediaItemToMediaItemCategory>();
			}
			// run tests
			try
			{
				// test 1
				{
					using var db = GetDataConnection(context);

					var now = TestData.DateTime;
					var userId = TestData.Guid1;

					var result = GetQuery(db, userId, now)
						.Select(MediaItemSearchSharedResultProjection(now))
						.ToList();
				}

				// test 2 (same query; should also work properly)
				{
					using var db = GetDataConnection(context);

					var now = TestData.DateTime3;
					var userId = TestData.Guid2;

					var result = GetQuery(db, userId, now)
						.Select(MediaItemSearchSharedResultProjection(now))
						.ToList();
				}
			}
			finally
			{
				// drop tables
				using var db = GetDataConnection(context);
				db.GetTable<MediaItemToMediaItemCategory>().Drop(false);
				db.GetTable<MediaItemUserShare>().Drop(false);
				db.GetTable<MediaItem>().Drop(false);
			}

			static Expression<Func<MediaItem, MediaItemSearchSharedResult>> MediaItemSearchSharedResultProjection(DateTime now)
			{
				return x => new MediaItemSearchSharedResult
				{
					CategoryIds = x.Categories.Select(y => y.CategoryId).ToList(),
					IsUnvisited = x.UserShare.Any(y => y.ExpiresAt > now)
				};
			}

			static IQueryable<MediaItem> GetQuery(DataConnection context, Guid userId, DateTime now)
			{
				return context.GetTable<MediaItem>()
					.Where(x =>
						x.UserShare.Any(y => y.UserId == userId && y.ExpiresAt > now) ||
						x.UserShare.Any(y => y.CreatedById == userId && y.ExpiresAt > now));
			}
		}

		public sealed class MediaItemSearchSharedResult
		{
			public IList<Guid> CategoryIds { get; set; } = new List<Guid>();

			public bool IsUnvisited { get; set; }
		}

		[Table("issue_4254_media_item_to_media_item_categories")]
		public sealed class MediaItemToMediaItemCategory
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column("category_id")]
			public Guid CategoryId { get; set; }

			[Column("media_item_id")]
			public Guid MediaItemId { get; set; }
		}

		[Table("issue_4254_media_item_user_share")]
		public sealed class MediaItemUserShare
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column("media_item_id")]
			public Guid MediaItemId { get; set; }

			[Column("created_by_id")]
			public Guid CreatedById { get; set; }

			[Column("user_id")]
			public Guid UserId { get; set; }

			[Column("expires_at")]
			public DateTime ExpiresAt { get; set; }
		}

		[Table("issue_4254_media_items")]
		public sealed class MediaItem
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(MediaItemToMediaItemCategory.MediaItemId))]
			public IList<MediaItemToMediaItemCategory> Categories { get; set; } = new List<MediaItemToMediaItemCategory>();

			[Association(ThisKey = nameof(Id), OtherKey = nameof(MediaItemUserShare.MediaItemId))]
			public IList<MediaItemUserShare> UserShare { get; set; } = new List<MediaItemUserShare>();
		}
	}
}
