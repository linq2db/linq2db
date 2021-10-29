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

		[Sql.Extension("Sum({expr})", IsAggregate = true)]
		public static TV SumCustom<T, TV>(IEnumerable<T> items, [ExprParameter] Expression<Func<T, TV>> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("Sum({items})", IsAggregate = true)]
		public static T SumCustom<T>([ExprParameter] IEnumerable<T> items)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void SubqueryAggregation([DataSources(ProviderName.SqlCe, TestProvName.AllSybase)] string context)
		{
			var ms      = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

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

			using (var db = GetDataContext(context, ms))
			using (var employeeTimeOffBalances = db.CreateLocalTable<EmployeeTimeOffBalance>())
			using (db.CreateLocalTable<Employee>())
			using (db.CreateLocalTable<LeaveRequest>())
			using (db.CreateLocalTable<LeaveRequestDateEntry>())
			{
				var query = employeeTimeOffBalances
					.Select(tracking => new
					{
						WithParentReference = (decimal?)tracking.Employee.LeaveRequests
							.SelectMany(e => e.LeaveRequestDateEntries)
							.Select(e => tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
							.DefaultIfEmpty(0)
							.Sum(),
						WithParentReferenceCustom1 = SumCustom(tracking.Employee.LeaveRequests
							.SelectMany(e => e.LeaveRequestDateEntries)
							.Select(e => tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
							.DefaultIfEmpty(0), x => x),
						WithParentReferenceCustom2 = SumCustom(tracking.Employee.LeaveRequests
							.SelectMany(e => e.LeaveRequestDateEntries)
							.Select(e => tracking.TrackingTimeType == TrackingTimeType.Hour ? e.StartHour : e.EndHour)
							.DefaultIfEmpty(0)),
						WithoutParentReference = (decimal?)tracking.Employee.LeaveRequests
							.SelectMany(e => e.LeaveRequestDateEntries)
							.Select(e => e.StartHour != null ? e.StartHour : e.EndHour)
							.DefaultIfEmpty(0)
							.Sum()
					});

				FluentActions.Invoking(() => query.ToArray()).Should().NotThrow();
			}
		}
	}
}
