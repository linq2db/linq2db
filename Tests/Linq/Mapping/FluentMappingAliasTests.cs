﻿using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class FluentMappingAliasTests : TestBase
	{
		class InstanceClass : IProjected
		{
			public int     Id       { get; set; }
			public int     Value    { get; set; }
			public string? ValueStr { get; set; }

			public int     EntityValue       { get => Value;    set => Value    = value; }
			public string? EntityValueStr    { get => ValueStr; set => ValueStr = value; }

			[ColumnAlias("Id")]
			public int    EntityId          { get => Id;       set => Id       = value; }
		}

		interface IProjected
		{
			int     EntityId       { get; set; }
			int     EntityValue    { get; set; }
			string? EntityValueStr { get; set; }
		}

		MappingSchema CreateMappingSchemaWithAlias()
		{
			var schema = new MappingSchema();
			var fluent = schema.GetFluentMappingBuilder();

			fluent.Entity<InstanceClass>().IsColumnRequired()
				.IsColumnRequired()
				.Property(e => e.Id)
				.Property(e => e.Value)
				.Property(e => e.ValueStr).HasLength(10)
				.Member(e => e.EntityValue).IsAlias(e => e.Value)
				.Member(e => e.EntityValueStr).IsAlias("ValueStr");

			return fluent.MappingSchema;
		}

		InstanceClass[] GenerateData()
		{
			return Enumerable.Range(1, 20)
				.Select(i => new InstanceClass { Id = i, Value = 100 + i, ValueStr = "Str_" + i }).ToArray();
		}

		[Test]
		public void AliasingTest([DataSources] string context)
		{
			using (new DisableBaseline("TODO: debug reason for inconsistent column order"))
			using (var db = GetDataContext(context, CreateMappingSchemaWithAlias()))
			{
				var testData = GenerateData();
				using (var table = db.CreateLocalTable(testData))
				{
					IQueryable<IProjected> queryable = table;

					var items = queryable.Where(t => t.EntityId > 1 & t.EntityValue >= 104 && t.EntityValue <= 115 && t.EntityValueStr!.StartsWith("S")).ToArray();
					var expected = table .Where(t => t.EntityId > 1 & t.EntityValue >= 104 && t.EntityValue <= 115 && t.EntityValueStr!.StartsWith("S")).OfType<IProjected>().ToArray();

					AreEqualWithComparer(expected, items);
				}
			}
		}
	}
}
