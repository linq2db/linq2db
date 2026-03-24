using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingStrategyPostQueryTests : TestBase
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

		// Separate entity for second association tests (avoids table name collision)
		[Table]
		sealed class Contractor
		{
			[Column, PrimaryKey] public int     Id           { get; set; }
			[Column]             public int     DepartmentId { get; set; }
			[Column]             public string? Name         { get; set; }
			[Column]             public int     Rate         { get; set; }
		}

		static (Company[], Department[], Employee[], Contractor[]) GenerateHierarchy()
		{
			var companies = Enumerable.Range(1, 3)
				.Select(i => new Company { Id = i, Name = "Company" + i })
				.ToArray();

			var departments = companies
				.SelectMany(c => Enumerable.Range(1, 2 + c.Id) // 3,4,5 departments per company
					.Select(j => new Department
					{
						Id        = c.Id * 100 + j,
						CompanyId = c.Id,
						Name      = $"Dept{c.Id}_{j}",
						IsActive  = j % 2 == 1, // odd = active
					}))
				.ToArray();

			var employees = departments
				.SelectMany(d => Enumerable.Range(1, (d.Id % 10)) // variable count per dept
					.Select(k => new Employee
					{
						Id           = d.Id * 100 + k,
						DepartmentId = d.Id,
						Name         = $"Emp{d.Id}_{k}",
						Salary       = 40000 + k * 5000,
					}))
				.ToArray();

			var contractors = departments
				.Where(d => d.IsActive) // only active depts have contractors
				.SelectMany(d => Enumerable.Range(1, 2)
					.Select(k => new Contractor
					{
						Id           = d.Id * 100 + k + 50,
						DepartmentId = d.Id,
						Name         = $"Ctr{d.Id}_{k}",
						Rate         = 100 + k * 25,
					}))
				.ToArray();

			return (companies, departments, employees, contractors);
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

		#region Basic PostQuery — single level

		[Test]
		public void LoadWith_PostQuery_SingleLevel(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var result = tCo
				.LoadWith(c => c.Departments.AsKeyedQuery())
				.OrderBy(c => c.Id)
				.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expected = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList();
				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expected, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		#endregion

		#region Select with inline eager loading — single level

		[Test]
		public void Select_PostQuery_InlineCollection(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

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

		#region Select with filter on children

		[Test]
		public void Select_PostQuery_FilteredChildren(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Only load active departments
			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					ActiveDepts = departments
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Multiple associations in same Select

		[Test]
		public void Select_PostQuery_MultipleAssociations(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors) = GenerateHierarchy();

			// Use only a subset of departments as the root
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var result = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(c => c.Id).ToList(),
				}
			).ToList();

			var expected = rootDepts
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					Employees   = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
					Contractors = contractors.Where(c => c.DepartmentId == d.Id).OrderBy(c => c.Id).ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region 3-level flat: root loads 2 levels of children independently

		[Test]
		public void Select_PostQuery_ThreeLevelFlat(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Root query loads departments AND employees independently (3 entity types, PostQuery)
			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
					AllEmployees = tEmp
						.Where(e => tDep.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.AsKeyedQuery()
						.OrderBy(e => e.Id)
						.ToList(),
				}
			).ToList();

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
					AllEmployees = employees
						.Where(e => departments.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Filtered parent + multiple PostQuery collections

		[Test]
		public void Select_PostQuery_FilteredParentMultipleCollections(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, contractors) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			// Filter companies, load departments + contractors at same level
			var result = (
				from c in tCo
				where c.Id >= 2
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
					InactiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && !d.IsActive)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

			var expected = companies
				.Where(c => c.Id >= 2)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					ActiveDepts = departments
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
					InactiveDepts = departments
						.Where(d => d.CompanyId == c.Id && !d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Scalar aggregates alongside PostQuery collections

		[Test]
		public void Select_PostQuery_ScalarAndCollection(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Mix scalar projections with PostQuery collection
			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DeptCount = tDep.Count(d => d.CompanyId == c.Id),
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					DeptCount = departments.Count(d => d.CompanyId == c.Id),
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region PostQuery with Take/Skip on parent

		[Test]
		public void Select_PostQuery_ParentWithTake(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Take first 2 companies, load departments via PostQuery
			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).Take(2).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Take(2)
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

		#region Nested: 2-level Company → Departments(with Employees)

		[Test]
		public void Select_PostQuery_NestedTwoLevel(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = employees
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Nested: 3-level Company → Departments → Employees + Contractors

		[Test]
		public void Select_PostQuery_NestedThreeLevel(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, contractors) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					CompanyName = c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							DeptName = d.Name,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.AsKeyedQuery()
								.OrderBy(e => e.Id)
								.ToList(),
							Contractors = tCtr
								.Where(ct => ct.DepartmentId == d.Id)
								.AsKeyedQuery()
								.OrderBy(ct => ct.Id)
								.ToList(),
						})
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					CompanyName = c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							DeptName = d.Name,
							Employees   = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
							Contractors = contractors.Where(ct => ct.DepartmentId == d.Id).OrderBy(ct => ct.Id).ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Nested with filters at each level

		[Test]
		public void Select_PostQuery_NestedWithFilters(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Filter at each level: companies >= 2, active departments only, high-salary employees
			var result = (
				from c in tCo
				where c.Id >= 2
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepartments = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							HighPaidEmployees = tEmp
								.Where(e => e.DepartmentId == d.Id && e.Salary > 45000)
								.AsKeyedQuery()
								.OrderByDescending(e => e.Salary)
								.ToList(),
						})
						.ToList(),
				}
			).ToList();

			var expected = companies
				.Where(c => c.Id >= 2)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					ActiveDepartments = departments
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							HighPaidEmployees = employees
								.Where(e => e.DepartmentId == d.Id && e.Salary > 45000)
								.OrderByDescending(e => e.Salary)
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Nested: scalar + collection mix at each level

		[Test]
		public void Select_PostQuery_NestedScalarAndCollection(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DeptCount = tDep.Count(d => d.CompanyId == c.Id),
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							EmpCount  = tEmp.Count(e => e.DepartmentId == d.Id),
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.AsKeyedQuery()
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					DeptCount = departments.Count(d => d.CompanyId == c.Id),
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							EmpCount  = employees.Count(e => e.DepartmentId == d.Id),
							Employees = employees
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region FirstOrDefault — single association, verify query count

		[Test]
		public void Select_PostQuery_FirstOrDefault_SingleAssociation(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0; // reset after DDL

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).FirstOrDefault();

			result.ShouldNotBeNull();

			var firstCompany = companies.OrderBy(c => c.Id).First();
			result.Id.ShouldBe(firstCompany.Id);
			result.Name.ShouldBe(firstCompany.Name);

			var expectedDepts = departments
				.Where(d => d.CompanyId == firstCompany.Id)
				.OrderBy(d => d.Id)
				.ToList();

			AreEqual(expectedDepts, result.Departments, ComparerBuilder.GetEqualityComparer(expectedDepts));

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		#endregion

		#region FirstOrDefault — no matching children, verify empty list

		[Test]
		public void Select_PostQuery_FirstOrDefault_NoChildren(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			// Only one company, no departments match
			var companies   = new[] { new Company { Id = 999, Name = "Lonely" } };
			var departments = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).FirstOrDefault();

			result.ShouldNotBeNull();
			result.Id.ShouldBe(999);
			result.Departments.ShouldNotBeNull();
			result.Departments.Count.ShouldBe(0);

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		#endregion

		#region FirstOrDefault — multiple associations, verify query count

		[Test]
		public void Select_PostQuery_FirstOrDefault_MultipleAssociations(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			counter.Count = 0;

			var result = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(c => c.Id).ToList(),
				}
			).FirstOrDefault();

			result.ShouldNotBeNull();

			var firstDept = rootDepts.OrderBy(d => d.Id).First();
			result.Id.ShouldBe(firstDept.Id);

			var expectedEmps = employees.Where(e => e.DepartmentId == firstDept.Id).OrderBy(e => e.Id).ToList();
			var expectedCtrs = contractors.Where(c => c.DepartmentId == firstDept.Id).OrderBy(c => c.Id).ToList();

			AreEqual(expectedEmps, result.Employees, ComparerBuilder.GetEqualityComparer(expectedEmps));
			AreEqual(expectedCtrs, result.Contractors, ComparerBuilder.GetEqualityComparer(expectedCtrs));

			// 1 buffer preamble + 2 child queries = 3 SELECT queries
			counter.Count.ShouldBe(3);
		}

		#endregion

		#region Empty master — no rows returned, only 1 query executed

		[Test]
		public void Select_PostQuery_EmptyMaster_OnlyOneQuery(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			// Empty companies table — master returns nothing
			var companies   = Array.Empty<Company>();
			var departments = GenerateHierarchy().Item2;

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

			result.Count.ShouldBe(0);

			// Buffer preamble returns empty, child query is skipped = 1 SELECT only
			counter.Count.ShouldBe(1);
		}

		[Test]
		public void Select_PostQuery_EmptyMaster_MultipleAssociations_OnlyOneQuery(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (_, _, employees, contractors) = GenerateHierarchy();

			// Empty departments — master returns nothing
			var rootDepts = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			counter.Count = 0;

			var result = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
						.AsKeyedQuery()
						.OrderBy(c => c.Id).ToList(),
				}
			).ToList();

			result.Count.ShouldBe(0);

			// Buffer preamble returns empty, both child queries skipped = 1 SELECT only
			counter.Count.ShouldBe(1);
		}

		[Test]
		public void Select_PostQuery_EmptyMaster_FirstOrDefault_OnlyOneQuery(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var companies   = Array.Empty<Company>();
			var departments = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).FirstOrDefault();

			result.ShouldBeNull();

			// Buffer preamble returns empty, child query skipped = 1 SELECT only
			counter.Count.ShouldBe(1);
		}

		#endregion

		#region Global DefaultEagerLoadingStrategy — no AsKeyedQuery() needed

		[Test]
		public void Select_GlobalPostQuery_InlineCollection(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);

			// Enable PostQuery globally — no AsKeyedQuery() calls needed
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).ToList();

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

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalPostQuery_MultipleAssociations(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			counter.Count = 0;

			var result = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id)
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
						.OrderBy(c => c.Id).ToList(),
				}
			).ToList();

			var expected = rootDepts
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					Employees   = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
					Contractors = contractors.Where(c => c.DepartmentId == d.Id).OrderBy(c => c.Id).ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));

			// 1 buffer preamble + 2 child queries = 3 SELECT queries
			counter.Count.ShouldBe(3);
		}

		[Test]
		public void Select_GlobalPostQuery_NestedTwoLevel(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			).ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = employees
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void LoadWith_GlobalPostQuery_SingleLevel(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			// No AsKeyedQuery() — global strategy applies
			var result = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id)
				.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expected = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList();
				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expected, ComparerBuilder.GetEqualityComparer(expected));
			}

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalPostQuery_FirstOrDefault(
			[DataSources(false, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			var result = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).FirstOrDefault();

			result.ShouldNotBeNull();

			var firstCompany = companies.OrderBy(c => c.Id).First();
			result.Id.ShouldBe(firstCompany.Id);

			var expectedDepts = departments
				.Where(d => d.CompanyId == firstCompany.Id)
				.OrderBy(d => d.Id)
				.ToList();

			AreEqual(expectedDepts, result.Departments, ComparerBuilder.GetEqualityComparer(expectedDepts));

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		#endregion
	}
}
