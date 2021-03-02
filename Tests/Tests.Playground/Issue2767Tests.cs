using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class Issue2767Tests : TestBase
	{
		[Table("exercise")]
		public class DBExercise
		{
			[Column("id"), PrimaryKey] public int Id { get; set; }
			[Column("name"), Nullable] public string Name { get; set; } = null!;
			[Column("number"), Nullable] public int? Number { get; set; }
			[Column("startexpl"), Nullable] public string Startexpl { get; set; } = null!;
			[Column("level"), NotNull] public int Level { get; set; }
			[Column("expl"), NotNull] public string Description { get; set; } = null!;
			[Column("date"), NotNull] public DateTime Date { get; set; }
			[Column("image"), Nullable] public string Image { get; set; } = null!;
			[Column("video"), Nullable] public string Video { get; set; } = null!;
			[Column("num"), NotNull] public int Num { get; set; }
			[Column("side"), NotNull] public int Side { get; set; }
			[Column("reeks"), NotNull] public int Reeks { get; set; }
			[Column("time"), NotNull] public int Time { get; set; }
			[Column("rest"), NotNull] public int Rest { get; set; }
			[Column("weight"), NotNull] public int Weight { get; set; }
			[Column("is_private"), NotNull] public bool IsPrivate { get; set; }
			[Column("timestamp", SkipOnUpdate = true), NotNull] public DateTime Timestamp { get; set; } = DateTime.Now;

			public static DBExercise[] Seed()
			{
				return new DBExercise[]
				{
					new DBExercise
					{
						Id          = 1,
						Name        = "Exercise1",
						Level       = 10,
						Description = "Exercise1 Description",
						Date        = TestData.Date,
						Num         = 100,
						Side        = 1000,
						Reeks       = 10000,
						Time        = 100000,
						Rest        = 1000000,
						Weight      = 10000000,
						IsPrivate   = false,
						Timestamp   = TestData.DateTime
					},
					new DBExercise
					{
						Id          = 2,
						Name        = "Exercise1",
						Level       = 20,
						Description = "Exercise1 Description",
						Date        = TestData.Date,
						Num         = 200,
						Side        = 2000,
						Reeks       = 20000,
						Time        = 200000,
						Rest        = 2000000,
						Weight      = 20000000,
						IsPrivate   = false,
						Timestamp   = TestData.DateTime
					}
				};
			}
		}
		[Table("ext_translations")]
		public class DBDescription
		{
			[Column("id"), PrimaryKey] public int Id { get; set; }
			[Column("locale"), NotNull] public string? Locale { get; set; }
			[Column("object_class"), NotNull] public string ObjectClass { get; set; } = null!;
			[Column("field"), NotNull] public string Field { get; set; } = null!;
			[Column("foreign_key"), NotNull] public string? ForeignKey { get; set; }
			[Column("content"), Nullable] public string Content { get; set; } = null!;
		}
		[Table("exercise_equipment_linker")]
		public class DBExerciseEquipmentLinker
		{
			[Column("exercise_id"), PrimaryKey(1), NotNull] public int ExerciseId { get; set; }
			[Column("equipment_id"), PrimaryKey(2), NotNull] public int EquipmentId { get; set; }

			public static DBExerciseEquipmentLinker[] Seed()
			{
				return new[]
				{
					new DBExerciseEquipmentLinker {EquipmentId = 11, ExerciseId = 1},
					new DBExerciseEquipmentLinker {EquipmentId = 31, ExerciseId = 1},
					new DBExerciseEquipmentLinker {EquipmentId = 21, ExerciseId = 2},
				};
			}
		}
		[Table("exercise_equipment")]
		public class DBEquipment
		{
			[Column("id"), PrimaryKey] public int Id { get; set; }
			[Column("parent_id"), Nullable] public int? ParentId { get; set; }
			[Column("name"), NotNull] public string Name { get; set; } = null!;
			[Column("icon"), Nullable] public string Icon { get; set; } = null!;
			[Column("online"), NotNull] public bool Online { get; set; }
			[Column("user_id"), Nullable] public int? UserId { get; set; }
			[Column("original_creator_id"), Nullable] public int? OriginalCreatorId { get; set; }
			[Column("is_private"), NotNull] public bool IsPrivate { get; set; }
			[Column("organisation_id"), Nullable] public int? OrganisationId { get; set; }
			[Column("original_organisation_id"), Nullable] public int? OriginalOrganisationId { get; set; }

			public static DBEquipment[] Seed()
			{
				return new DBEquipment[]
				{
					new DBEquipment()
					{
						Id = 11, Name = "Equipment1", Online = true, IsPrivate = false,
					},
					new DBEquipment()
					{
						Id = 21, Name = "Equipment2", Online = true, IsPrivate = false,
					},
					new DBEquipment()
					{
						Id = 31, Name = "Equipment3", Online = true, IsPrivate = false,
					}
				};
			}
		}

		public class DescriptionJoinModel<T>
		{
			public T Object { get; set; } = default!;

			public int? DescriptionId { get; set; }

			public string Description { get; set; } = null!;
			public DescriptionJoinModel<T> Child { get; set; } = null!;
		}
		public class ExerciseInfoSelectModel
		{
			public int Id { get; set; }
			public int? Number { get; set; }
			public string Name { get; set; } = null!;
			public int Level { get; set; }
			public string Description { get; set; } = null!;
			public int? Set { get; set; }
			public int? Times { get; set; }
			public int? Time { get; set; }
			public int? Rest { get; set; }
			public int? Side { get; set; }
			public int EffectiveTime { get; set; }
			public string Image { get; set; } = null!;
			public string Video { get; set; } = null!;
			public List<DescriptionJoinModel<DBEquipment>> Equipments { get; set; } = null!;
			public string StartPositionDescription { get; set; } = null!;
			public bool IsPrivate { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<DBExercise>(DBExercise.Seed()))
			using (db.CreateLocalTable<DBDescription>())
			using (db.CreateLocalTable<DBExerciseEquipmentLinker>(DBExerciseEquipmentLinker.Seed()))
			using (db.CreateLocalTable<DBEquipment>(DBEquipment.Seed()))
			{
				var language = "en";
				var currentLanguage = language;
				var OriginId = new {Exercise = "1", Equipment = "2"};

				var query = from exercise in db.GetTable<DBExercise>()
					join exerciseDescription in db.GetTable<DBDescription>().Where(x => x.Locale == language && x.ObjectClass == OriginId.Exercise && x.Field == "expl") on Sql.Convert<string, int>(exercise.Id) equals exerciseDescription.ForeignKey into exDesc
					from exerciseDescription in exDesc.DefaultIfEmpty()
					join exerciseStartDescription in db.GetTable<DBDescription>().Where(x => x.Locale == language && x.ObjectClass == OriginId.Exercise && x.Field == "startexpl") on Sql.Convert<string, int>(exercise.Id) equals exerciseStartDescription.ForeignKey into exStartDesc
					from exerciseStartDescription in exStartDesc.DefaultIfEmpty()
					orderby exercise.Timestamp descending, exercise.Id descending
					select new ExerciseInfoSelectModel
					{
						Id = exercise.Id,
						IsPrivate = exercise.IsPrivate,
						Number = exercise.Number,
						Level = exercise.Level,
						Description = exerciseDescription != null ? exerciseDescription.Content : exercise.Description,
						Set = exercise.Reeks,
						Times = exercise.Num,
						Time = exercise.Time,
						Rest = exercise.Rest,
						Side = exercise.Side,
						Image = exercise.Image,
						Video = exercise.Video,
						Equipments = (from equipmentLinker in db.GetTable<DBExerciseEquipmentLinker>()
								join equipment in db.GetTable<DBEquipment>() on equipmentLinker.EquipmentId equals equipment.Id
								join description in db.GetTable<DBDescription>().Where(x => x.Locale == currentLanguage && x.ObjectClass == OriginId.Equipment && x.Field == "name") on Sql.Convert<string, int>(equipment.Id) equals description.ForeignKey into desc
								from description in desc.DefaultIfEmpty()
								where exercise.Id == equipmentLinker.ExerciseId
								select new DescriptionJoinModel<DBEquipment>
								{
									Object = equipment,
									Description = description.Content,
									DescriptionId = description.Id
								})
							.ToList(),
						StartPositionDescription = exerciseStartDescription != null ? exerciseStartDescription.Content : exercise.Startexpl,
					};


				var result = query.ToList();

			}
		}
	}
}
