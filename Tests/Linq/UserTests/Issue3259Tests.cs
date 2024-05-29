using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3259Tests : TestBase
	{
		public class EmployeeTimeOffBalance
		{
			public virtual int              Id               { get; set; }
			public virtual TrackingTimeType TrackingTimeType { get; set; }
			public virtual Employee         Employee         { get; set; } = null!;
			public virtual int              EmployeeId       { get; set; }
		}

		public class Employee
		{
			public virtual int Id { get; set; }

			public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = null!;
		}

		public class LeaveRequest
		{
			public virtual int                                Id                      { get; set; }
			public virtual int                                EmployeeId              { get; set; }
			public virtual ICollection<LeaveRequestDateEntry> LeaveRequestDateEntries { get; set; } = null!;
		}

		public class LeaveRequestDateEntry
		{
			public virtual int      Id             { get; set; }
			public virtual decimal? EndHour        { get; set; }
			public virtual decimal? StartHour      { get; set; }
			public virtual int      LeaveRequestId { get; set; }
		}

		public enum TrackingTimeType
		{
			Hour,
			Day
		}

		[Sql.Extension("Sum({expr})", IsAggregate = true, ServerSideOnly = true)]
		private static TV SumCustom<T, TV>(IEnumerable<T> items, [ExprParameter] Expression<Func<T, TV>> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("Sum({items})", IsAggregate = true, ServerSideOnly = true)]
		private static T SumCustom<T>([ExprParameter] IEnumerable<T> items)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void SubqueryAggregation([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms      = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<EmployeeTimeOffBalance>()
				.HasPrimaryKey(x => x.Id)
				.Association(x => x.Employee, x => x.EmployeeId, x => x.Id, canBeNull: false);

			builder.Entity<Employee>()
				.Property(e => e.Id).HasColumnName("EmployeeId").IsPrimaryKey()
				.Association(x => x.LeaveRequests, x => x.Id, x => x.EmployeeId, canBeNull: false);

			builder.Entity<LeaveRequest>()
				.HasPrimaryKey(x => x.Id)
				.Association(x => x.LeaveRequestDateEntries, x => x.Id, x => x.LeaveRequestId, canBeNull: false);

			builder.Entity<LeaveRequestDateEntry>()
				.HasPrimaryKey(x => x.Id);

			builder.Build();

			var timeOffBalances = new EmployeeTimeOffBalance[]{new()
				{
					Id               = 1,
					EmployeeId       = 1,
					TrackingTimeType = TrackingTimeType.Hour
				},
				new ()
				{
					Id               = 2,
					EmployeeId       = 2,
					TrackingTimeType = TrackingTimeType.Day
				}};

			var employees = new Employee[] { new() { Id = 1 }, new() { Id = 2 } };

			var leaveRequests = new LeaveRequest[]
			{
				new() { EmployeeId = 1, Id = 1 }, new() { EmployeeId = 1, Id = 2 },
				new() { EmployeeId = 2, Id = 3 }, new() { EmployeeId = 2, Id = 4 }
			};

			var dateEntry = new LeaveRequestDateEntry[]
			{
				new() { Id = 1, StartHour = 1, EndHour = 12, LeaveRequestId = 1 },
				new() { Id = 2, StartHour = 2, EndHour = 13, LeaveRequestId = 1 },
				new() { Id = 3, StartHour = 3, EndHour = 14, LeaveRequestId = 2 },
				new() { Id = 4, StartHour = 4, EndHour = 15, LeaveRequestId = 2 },
			};

			using (var db = GetDataContext(context, ms))
			{
				using (var employeeTimeOffBalances = db.CreateLocalTable(timeOffBalances))
				using (db.CreateLocalTable(employees))
				using (db.CreateLocalTable(leaveRequests))
				using (db.CreateLocalTable(dateEntry))
				{
					var query = employeeTimeOffBalances
						.Select(tracking => new
						{
							WithParentReference = (decimal?)tracking.Employee.LeaveRequests
								.SelectMany(e => e.LeaveRequestDateEntries)
								.Select(e =>
									tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0)
								.Sum(),
							WithParentReferenceCustom1 = SumCustom(tracking.Employee.LeaveRequests
								.SelectMany(e => e.LeaveRequestDateEntries)
								.Select(e =>
									tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0), x => x),
							WithParentReferenceCustom2 = SumCustom(tracking.Employee.LeaveRequests
								.SelectMany(e => e.LeaveRequestDateEntries)
								.Select(e =>
									tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0)),
							WithoutParentReference = (decimal?)tracking.Employee.LeaveRequests
								.SelectMany(e => e.LeaveRequestDateEntries)
								.Select(e => e.StartHour != null ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0)
								.Sum()
						});

					var result = query.OrderBy(x => x.WithParentReference ?? 0)
						.ThenBy(x => x.WithParentReferenceCustom1         ?? 0)
						.ThenBy(x => x.WithParentReferenceCustom2         ?? 0)
						.ThenByDescending(x => x.WithoutParentReference   ?? 0)
						.ToArray();

					var expectedQuery = timeOffBalances
						.Select(tracking => new
						{
							WithParentReference = leaveRequests.Where(lr => lr.EmployeeId == tracking.EmployeeId)
								.SelectMany(e => dateEntry.Where(d => d.LeaveRequestId == e.Id))
								.Select(e =>
									tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0)
								.Sum(),

							WithoutParentReference = leaveRequests.Where(lr => lr.EmployeeId == tracking.EmployeeId)
								.SelectMany(e => dateEntry.Where(d => d.LeaveRequestId == e.Id))
								.Select(e => e.StartHour != null ? e.StartHour : e.EndHour)
								.DefaultIfEmpty(0)
								.Sum()

						});

					var expected = expectedQuery
						.OrderBy(x => x.WithParentReference             ?? 0)
						.ThenByDescending(x => x.WithoutParentReference ?? 0)
						.ToArray();

					result.Should().HaveCount(expected.Length);

					for (int i = 0; i < result.Length; i++)
					{
						result[i].WithParentReference.Should().Be(expected[i].WithParentReference);
						result[i].WithoutParentReference.Should().Be(expected[i].WithoutParentReference);
					}

				}
			}
		}
	}
}
