using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

using Newtonsoft.Json;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.JsonConverter
{
	public sealed class JsonConvertContext : DbContext
	{
		public JsonConvertContext()
		{
		}

		public JsonConvertContext(DbContextOptions<JsonConvertContext> options)
			: base(options)
		{
		}

		public DbSet<EventScheduleItem> EventScheduleItems { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<EventScheduleItem>(entity =>
			{
				entity.ToTable("EventScheduleItem");
				entity.Property(e => e.NameLocalized)
					.HasColumnName("NameLocalized_JSON")
					.HasConversion(v => JsonConvert.SerializeObject(v),
						v => JsonConvert.DeserializeObject<LocalizedString>(v) ?? new());
				entity.Property(e => e.CrashEnum).HasColumnType("tinyint");
				entity.Property(e => e.GuidColumn).HasColumnType("uniqueidentifier");
			});

#if !NETFRAMEWORK
				modelBuilder.HasDbFunction(typeof(JsonConvertTests).GetMethod(nameof(JsonValue))!)
					.HasTranslation(e => new SqlFunctionExpression("JSON_VALUE", e, true, e.Select(_ => false), typeof(string), null));
#else
				modelBuilder.HasDbFunction(typeof(JsonConvertTests).GetMethod(nameof(JsonValue))!)
					.HasTranslation(e => new SqlFunctionExpression(null, null, "JSON_VALUE", false, e, true, typeof(string), null));

#endif
		}
	}
}
