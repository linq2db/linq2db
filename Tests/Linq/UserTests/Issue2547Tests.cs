using System;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2547Tests : TestBase
	{
		public enum AA
		{
			A,
			B
		}

		[Table(Name = "Issue2547Item")]
		public partial class Item
		{
			[PrimaryKey, NotNull] public int Id { get; set; } // int
			[Column, NotNull] public string Name { get; set; } = null!; // nvarchar(50)
			[Column, NotNull] public TimeSpan TestSpan { get; set; }
			[Column, NotNull] public AA TestEnum { get; set; }
		}

		[Test]
		public void Issue2547Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var mappingSchema = new MappingSchema();

			mappingSchema.SetConverter<AA, string>((obj) =>
			{
				return obj.ToString();
			});
			mappingSchema.SetConverter<AA, DataParameter>((obj) =>
			{
				return new DataParameter { Value = obj.ToString() };
			});
			mappingSchema.SetConverter<string, AA>((txt) =>
			{
				return (AA)Enum.Parse(typeof(AA), txt, true);
			});

			mappingSchema.SetConverter<TimeSpan, long>((obj) =>
			{
				return obj.Ticks;
			});
			mappingSchema.SetConverter<TimeSpan, DataParameter>((obj) =>
			{
				return new DataParameter { Value = obj.Ticks };
			});
			mappingSchema.SetConverter<long, TimeSpan>((val) =>
			{
				return TimeSpan.FromTicks(val);
			});

			var items = new Item[]
			{
				new Item { Id = 1, Name = "Item 1", TestSpan = TimeSpan.FromSeconds(450), TestEnum = AA.A },
			};

			using (var db = GetDataContext(context, mappingSchema))
			using (db.CreateLocalTable(items))
			{
				
			}
		}
	}
}
