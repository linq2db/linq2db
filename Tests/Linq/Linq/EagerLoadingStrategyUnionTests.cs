using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingStrategyUnionTests : TestBase
	{
		#region Entities — 3-level hierarchy: Company → Department → Employee

		[Table]
		sealed class Company
		{
			[Column, PrimaryKey] public int     Id   { get; set; }
			[Column]             public string? Name { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Department.CompanyId))]
			public List<Department> Departments { get; set; } = null!;
		}

		[Table]
		sealed class Department
		{
			[Column, PrimaryKey] public int     Id        { get; set; }
			[Column]             public int     CompanyId { get; set; }
			[Column]             public string? Name      { get; set; }
			[Column]             public bool    IsActive  { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Employee.DepartmentId))]
			public List<Employee> Employees { get; set; } = null!;
		}

		[Table]
		sealed class Employee
		{
			[Column, PrimaryKey] public int     Id           { get; set; }
			[Column]             public int     DepartmentId { get; set; }
			[Column]             public string? Name         { get; set; }
			[Column]             public int     Salary       { get; set; }
		}

		[Table]
		sealed class Contractor
		{
			[Column, PrimaryKey] public int     Id           { get; set; }
			[Column]             public int     DepartmentId { get; set; }
			[Column]             public string? Name         { get; set; }
			[Column]             public int     Rate         { get; set; }
		}

		[Table]
		sealed class Intern
		{
			[Column, PrimaryKey] public int     Id           { get; set; }
			[Column]             public int     DepartmentId { get; set; }
			[Column]             public string? Name         { get; set; }
			[Column]             public string? School       { get; set; }
		}

		static (Company[], Department[], Employee[], Contractor[], Intern[]) GenerateHierarchy()
		{
			var companies = Enumerable.Range(1, 3)
				.Select(i => new Company { Id = i, Name = "Company" + i })
				.ToArray();

			var departments = companies
				.SelectMany(c => Enumerable.Range(1, 2 + c.Id)
					.Select(j => new Department
					{
						Id        = c.Id * 100 + j,
						CompanyId = c.Id,
						Name      = $"Dept{c.Id}_{j}",
						IsActive  = j % 2 == 1,
					}))
				.ToArray();

			var employees = departments
				.SelectMany(d => Enumerable.Range(1, (d.Id % 10))
					.Select(k => new Employee
					{
						Id           = d.Id * 100 + k,
						DepartmentId = d.Id,
						Name         = $"Emp{d.Id}_{k}",
						Salary       = 40000 + k * 5000,
					}))
				.ToArray();

			var contractors = departments
				.Where(d => d.IsActive)
				.SelectMany(d => Enumerable.Range(1, 2)
					.Select(k => new Contractor
					{
						Id           = d.Id * 100 + k + 50,
						DepartmentId = d.Id,
						Name         = $"Ctr{d.Id}_{k}",
						Rate         = 100 + k * 25,
					}))
				.ToArray();

			var interns = departments
				.Where(d => d.Id % 2 == 0)
				.SelectMany(d => Enumerable.Range(1, 1)
					.Select(k => new Intern
					{
						Id           = d.Id * 100 + k + 80,
						DepartmentId = d.Id,
						Name         = $"Int{d.Id}_{k}",
						School       = $"School{k}",
					}))
				.ToArray();

			return (companies, departments, employees, contractors, interns);
		}

		#endregion

		#region Query count interceptor

		sealed class SelectQueryCounter : CommandInterceptor
		{
			public int Count { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				var sql = command.CommandText;

				if (sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("CREATE", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("DROP", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
				{
					Count++;
				}

				return command;
			}
		}

		#endregion

		#region Basic Union — single level

		[Test]
		public void Select_Union_InlineCollection(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsUnionQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion
	}
}
