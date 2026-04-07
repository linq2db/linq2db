using System.Linq;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	/// <summary>
	/// Regression: LINQ join fails with struct wrapper when comparing RawValue to underlying type.
	/// <see href="https://github.com/linq2db/linq2db/issues/5454"/>
	/// </summary>
	[TestFixture]
	public class Issue5454Tests : TestBase
	{
		public readonly struct WrappedShort
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public WrappedShort(short value) => RawValue = value;

			public short RawValue
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get;
			}
		}

		[Table]
		sealed class GroupStatsType
		{
			[Column, PrimaryKey]       public int    Id          { get; set; }
			[Column]                   public int    GroupId     { get; set; }
			[Column(CanBeNull = true)] public short? StatsTypeId { get; set; }
		}

		[Table]
		sealed class StatsType
		{
			[Column(DataType = DataType.Int16), PrimaryKey] public WrappedShort Id       { get; set; }
			[Column(CanBeNull = false)]                     public string       FullName { get; set; } = default!;
		}

		static MappingSchema CreateMappingSchema()
		{
			var ms      = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<StatsType>()
				.Property(e => e.Id)
				.HasConversion(v => v.RawValue, v => new WrappedShort(v));

			builder.Build();
			return ms;
		}

		[Test]
		public void StructRawValueLeftJoin([DataSources] string context)
		{
			var groupData = new[]
			{
				new GroupStatsType { Id = 1, GroupId = 10, StatsTypeId = 1 },
				new GroupStatsType { Id = 2, GroupId = 20, StatsTypeId = 2 },
				new GroupStatsType { Id = 3, GroupId = 30, StatsTypeId = null },
			};

			var typeData = new[]
			{
				new StatsType { Id = new WrappedShort(1), FullName = "Type1" },
				new StatsType { Id = new WrappedShort(2), FullName = "Type2" },
			};

			using var db     = GetDataContext(context, CreateMappingSchema());
			using var groups = db.CreateLocalTable(groupData);
			using var types  = db.CreateLocalTable(typeData);

			// Issue #5454: x.Id.RawValue == g.StatsTypeId throws
			// "The LINQ expression '(int?)(int)x.Id.RawValue' could not be converted to SQL"
			var query =
				from g in groups
				from st in types.LeftJoin(x => x.Id.RawValue == g.StatsTypeId)
				select new
				{
					g.StatsTypeId,
					StatsTypeName = st.FullName,
					g.GroupId,
				};

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(3));
		}
	}
}
