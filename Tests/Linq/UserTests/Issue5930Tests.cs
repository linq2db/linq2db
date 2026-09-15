using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5930Tests : TestBase
	{
		[Table]
		sealed class TaskRow
		{
			[PrimaryKey] public int       Id              { get; set; }
			[Column]     public DateTime? StartDateTime   { get; set; }
			[Column]     public TimeSpan? PreNotification { get; set; }
		}

		static readonly DateTime Start = new DateTime(2026, 9, 15, 12, 0, 0);

		static TaskRow[] CreateData() =>
		[
			new TaskRow { Id = 1, StartDateTime = Start, PreNotification = TimeSpan.FromHours(2) },
			new TaskRow { Id = 2, StartDateTime = Start, PreNotification = null                  },
			new TaskRow { Id = 3, StartDateTime = null,  PreNotification = TimeSpan.FromHours(2) },
			new TaskRow { Id = 4, StartDateTime = null,  PreNotification = null                  },
		];

		static MappingSchema CreateMapping()
		{
			var schema = new MappingSchema();
			var builder = new FluentMappingBuilder(schema);
			builder.Entity<TaskRow>()
				.Property(r => r.PreNotification)
				.HasDataType(DataType.Int64)
				.HasDuration(DurationUnit.Tick)
				.IsNullable();

			builder.Build();
			return schema;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ReadTasksWithDeclaredDuration(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			var data = CreateData();
			using var db    = GetDataContext(context, CreateMapping());
			using var table = db.CreateLocalTable(data);

			var stored = table.OrderBy(r => r.Id).ToArray();
			stored.Select(r => r.StartDateTime).ShouldBe(data.Select(r => r.StartDateTime));
			stored.Select(r => r.PreNotification).ShouldBe(data.Select(r => r.PreNotification));

			var actual = table.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(r.StartDateTime - r.PreNotification))
				.ToArray();

			actual.ShouldBe(data.Select(r => r.StartDateTime - r.PreNotification));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void FilterTasksWithDeclaredDuration(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			using var db    = GetDataContext(context, CreateMapping());
			using var table = db.CreateLocalTable(CreateData());
			var cutoff = Start.AddMinutes(-30);

			var query = table.Select(r => new
			{
				r.Id,
				NotificationDateTime = r.StartDateTime - r.PreNotification,
			});

			var actual = query.Where(r => r.NotificationDateTime < cutoff).Select(r => r.Id).ToArray();

			actual.ShouldBe(new[] { 1 });
		}
	}
}
