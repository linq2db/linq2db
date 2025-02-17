using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3926Tests : TestBase
	{
		public abstract class BaseDataObject
		{
			public abstract bool IsNew { get; }
		}

		public abstract class GuidDataObject: BaseDataObject
		{
			public Guid Id { get; internal set; }

			protected GuidDataObject()
			{
				Id = Guid.Empty;
			}

			public override bool IsNew => Id == Guid.Empty;
		}

		public class CALL_META : GuidDataObject
		{
			public int ProfileId { get; set; }
			// a lot of simple scalar attributes

			public CALL_RECORD?        CallRecord        { get; set; }
			public CALL_TRANSCRIPTION? CallTranscription { get; set; }

			public Guid?           DialogCategoryId { get; set; }
			public DIALOG_CATEGORY DialogCategory   { get; set; } = null!;
		}

		public class CALL_RECORD : GuidDataObject
		{
		}

		public class CALL_TRANSCRIPTION : GuidDataObject
		{

		}

		public class DIALOG_CATEGORY: GuidDataObject
		{
			public string Category { get; set; } = null!;

			public Guid CategoryGroupId { get; set; }

			public CATEGORY_GROUP CategoryGroup { get; set; } = null!;
		}

		public class CATEGORY_GROUP: GuidDataObject
		{
			public string GroupIcon { get; set; } = null!;

			public string TelegramBotName { get; set; } = null!;

			public List<DIALOG_CATEGORY> Categories { get; set; } = null!;
		}

		private static EntityMappingBuilder<T> MapDataObject<T>(FluentMappingBuilder builder, string tableName)
			where T : GuidDataObject
		{
			return builder.Entity<T>()
				.HasTableName(tableName)
				.HasPrimaryKey(x => x.Id)
				.Ignore(x => x.IsNew);
		}

		private static MappingSchema GetShema()
		{
			var result  = new MappingSchema();
			var builder = new FluentMappingBuilder(result);

			// other entities

			MapDataObject<CATEGORY_GROUP>(builder, "t_category_groups")
				.Property(x => x.Id).HasColumnName("Id").HasDataType(DataType.Guid)
				.Property(x => x.TelegramBotName).HasColumnName("TelegramBotName")
				.Property(x => x.GroupIcon).HasColumnName("GroupIcon")
				.Association(x => x.Categories, x => x.Id, x => x.CategoryGroupId);

			MapDataObject<DIALOG_CATEGORY>(builder, "t_dialog_categories")
				.Property(x => x.Id).HasColumnName("Id").HasDataType(DataType.Guid)
				.Property(x => x.Category).HasColumnName("Category")
				.Property(x => x.CategoryGroupId).HasColumnName("CategoryGroupId").HasDataType(DataType.Guid)
				.Association(x => x.CategoryGroup, x => x.CategoryGroupId, x => x.Id);

			MapDataObject<CALL_META>(builder, "t_call_metas")
				.Property(x => x.Id).HasColumnName("Id").HasDataType(DataType.Guid)
				// a lot of simple scalar fields
				.Association(x => x.CallRecord!, x => x.Id, x => x.Id)
				.Association(x => x.CallTranscription!, x => x.Id, x => x.Id)
				.Association(x => x.DialogCategory, x => x.DialogCategoryId, x => x.Id);

			builder.Build();

			return result;
		}

		[Test]
		public void LoadWithAfterFilterByAssociation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = GetShema();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<CATEGORY_GROUP>())
			using (db.CreateLocalTable<DIALOG_CATEGORY>())
			using (db.CreateLocalTable<CALL_RECORD>())
			using (db.CreateLocalTable<CALL_TRANSCRIPTION>())
			using (db.CreateLocalTable<CALL_META>())
			{
				var query = db.GetTable<CALL_META>()
					.Where(x => x.DialogCategory.CategoryGroup.TelegramBotName == new { ProfileId = 1, Category = "Some", Count = 2 }.Category)
					.LoadWith(x => x.DialogCategory.CategoryGroup)
					.LoadWith(x => x.CallTranscription)
					.LoadWith(x => x.CallRecord)
					.OrderByDescending(x => x.ProfileId).Take(new { ProfileId = 1, Category = "Some", Count = 2 }.Count);

				_ = query.ToList();

				var selectQuery = query.GetSelectQuery();

				selectQuery.Select.Columns.Any(c =>
					(c.Expression is SqlField field) && field.Name == "TelegramBotName").Should().BeTrue();
			}
		}
	}
}
