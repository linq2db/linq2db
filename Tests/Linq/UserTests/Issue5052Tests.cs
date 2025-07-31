using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue5052Tests : TestBase
	{
		public sealed class PersonEntity
		{
			public          int            Id        { get; set; }
			public required string         Name      { get; set; }
			public          List<Location> Locations { get; set; } = new();
		}

		[Table("test_person")]
		public sealed class PersonDto
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public required string Name { get; set; }

			[Association(QueryExpressionMethod = "GetLocationMappings")]
			public List<LocationMapping> Locations { get; set; } = new();

			public static Expression<Func<PersonDto, IDataContext, IQueryable<LocationMapping>>> GetLocationMappings =>
				(p, db) => from d in db.GetTable<LocationDto>()
					from l in db.GetTable<LinkPersonLocation>().LeftJoin(j => j.Id == 5) // does not exist
					where d.Id == 1
					select new LocationMapping { TheLink = l, Location = d };
		}

		[Table("test_link_person_location")]
		public sealed class LinkPersonLocation
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public int PersonId { get; set; }

			[Column]
			public int LocationId { get; set; }
		}

		[Table("test_location")]
		public sealed class LocationDto
		{
			[Column("id"), PrimaryKey, Identity]
			public int Id { get; set; }
			[Column]
			public required string Description { get; set; }
		}

		public sealed class Location
		{
			public int Id { get; set; }
			public required string Description { get; set; }
			public int LinkId { get; set; }
		}

		public sealed class LocationMapping
		{
			public LinkPersonLocation? TheLink  { get; set; }
			public LocationDto         Location { get; set; } = default!;
		}

		[Test]
		public void EagerLoadProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var personData = new List<PersonDto>
			{
				new() { Id = 1, Name = "John Doe" },
			};

			var locationData = new List<LocationDto>
			{
				new() { Id = 1, Description = "Location 1" },
			};

			using var db = GetDataContext(context);

			using var personTable = db.CreateLocalTable(personData);
			using var locationTable = db.CreateLocalTable(locationData);
			using var linkTable = db.CreateLocalTable<LinkPersonLocation>();

			var query = db.GetTable<PersonDto>()
				.Where(p => p.Id == 1)
				.Select(dtoPersonDto => new PersonEntity
					{
						Id   = dtoPersonDto.Id,
						Name = dtoPersonDto.Name,
						Locations = dtoPersonDto.Locations
							.Select(dtoLocationMapping => new Location
								{
									LinkId = dtoLocationMapping!.TheLink!.Id, Id = dtoLocationMapping.Location.Id, Description = dtoLocationMapping.Location.Description
								}
							)
							.ToList()
					}
				);

			Shouldly.Should.NotThrow(
				() => query.ToList(),
				"Projection should not throw an exception."
			);
		}
	}
}
