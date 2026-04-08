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
		#region Entities — 4-level hierarchy: Company → Department → Employee → EmployeeTask

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

			[Association(ThisKey = nameof(Id), OtherKey = nameof(EmployeeTask.EmployeeId))]
			public List<EmployeeTask> Tasks { get; set; } = null!;
		}

		[Table]
		sealed class EmployeeTask
		{
			[Column, PrimaryKey] public int     Id         { get; set; }
			[Column]             public int     EmployeeId { get; set; }
			[Column]             public string? Title      { get; set; }
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

		static (Company[], Department[], Employee[], Contractor[], Intern[], EmployeeTask[]) GenerateHierarchy()
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

			var tasks = employees
				.SelectMany(e => Enumerable.Range(1, 1 + (e.Id % 3))
					.Select(t => new EmployeeTask
					{
						Id         = e.Id * 100 + t,
						EmployeeId = e.Id,
						Title      = $"Task{e.Id}_{t}",
					}))
				.ToArray();

			return (companies, departments, employees, contractors, interns, tasks);
		}

		#endregion

		/// <summary>
		/// Returns <see langword="true"/> when the provider supports Common Table Expressions.
		/// Non-CTE providers (SqlCe, MySQL 5.7) fall back from CteUnion to KeyedQuery.
		/// </summary>
		static bool IsCteSupported(string context)
			=> !context.IsAnyOf(TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllMySql57);

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

		#region Basic Union — single level (LoadWith)

		[Test]
		public void LoadWith_Union_SingleLevel(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id)
				.AsUnionQuery();

			var result = query.ToList();

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
		public void Select_Union_InlineCollection(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

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
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Select_Union_FilteredChildren(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Only load active departments
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Select_Union_MultipleAssociations(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors, _, _) = GenerateHierarchy();

			// Use only a subset of departments as the root
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var query = (
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
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Select_Union_ThreeLevelFlat(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Root query loads departments AND employees independently (3 entity types, CteUnion)
			var query = (
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
					AllEmployees = tEmp
						.Where(e => tDep.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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

		#region Filtered parent + multiple Union collections

		[Test]
		public void Select_Union_FilteredParentMultipleCollections(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, contractors, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			// Filter companies, load departments + contractors at same level
			var query = (
				from c in tCo
				where c.Id >= 2
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
					InactiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && !d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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

		#region Scalar aggregates alongside Union collections

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select_Union_ScalarAndCollection(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Mix scalar projections with Union collection
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DeptCount = tDep.Count(d => d.CompanyId == c.Id),
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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

		#region Union with Take/Skip on parent

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		public void Select_Union_ParentWithTake(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllInformix)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Take first 2 companies, load departments via Union
			var query = (
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
			).Take(2);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Select_Union_NestedTwoLevel(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			var query = (
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
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			// CteUnion: single UNION ALL query; non-CTE providers fall back to KeyedQuery (buffer + 2 child queries)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 3 : 1);

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
		public void Select_Union_NestedThreeLevel(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, contractors, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					CompanyName = c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							DeptName = d.Name,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
							Contractors = tCtr
								.Where(ct => ct.DepartmentId == d.Id)
								.OrderBy(ct => ct.Id)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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

		[Test]
		public void Select_Union_NestedThreeCollectionsAtThirdLevel(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, contractors, interns, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);
			using var tInt = db.CreateLocalTable(interns);

			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					CompanyName = c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							DeptName = d.Name,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
							Contractors = tCtr
								.Where(ct => ct.DepartmentId == d.Id)
								.OrderBy(ct => ct.Id)
								.ToList(),
							Interns = tInt
								.Where(i => i.DepartmentId == d.Id)
								.OrderBy(i => i.Id)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
							Interns     = interns.Where(i => i.DepartmentId == d.Id).OrderBy(i => i.Id).ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Nested with filters at each level

		[Test]
		public void Select_Union_NestedWithFilters(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Filter at each level: companies >= 2, active departments only, high-salary employees
			var query = (
				from c in tCo
				where c.Id >= 2
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepartments = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							HighPaidEmployees = tEmp
								.Where(e => e.DepartmentId == d.Id && e.Salary > 45000)
								.OrderByDescending(e => e.Salary)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select_Union_NestedScalarAndCollection(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DeptCount = tDep.Count(d => d.CompanyId == c.Id),
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							EmpCount  = tEmp.Count(e => e.DepartmentId == d.Id),
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Select_Union_FirstOrDefault_SingleAssociation(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0; // reset after DDL

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
				)
				.AsUnionQuery()
				.FirstOrDefault();

			result.ShouldNotBeNull();

			var firstCompany = companies.OrderBy(c => c.Id).First();
			result.Id.ShouldBe(firstCompany.Id);
			result.Name.ShouldBe(firstCompany.Name);

			var expectedDepts = departments
				.Where(d => d.CompanyId == firstCompany.Id)
				.OrderBy(d => d.Id)
				.ToList();

			AreEqual(expectedDepts, result.Departments, ComparerBuilder.GetEqualityComparer(expectedDepts));

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);
		}

		#endregion

		#region FirstOrDefault — no matching children, verify empty list

		[Test]
		public void Select_Union_FirstOrDefault_NoChildren(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			// Only one company, no departments match
			var companies   = new[] { new Company { Id = 999, Name = "Lonely" } };
			var departments = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			var result = (
					from c in tCo
					orderby c.Id
					select new
					{
						c.Id,
						Departments = tDep
							.Where(d => d.CompanyId == c.Id)
							.OrderBy(d => d.Id)
							.ToList(),
					}
				)
				.AsUnionQuery()
				.FirstOrDefault();

			result.ShouldNotBeNull();
			result.Id.ShouldBe(999);
			result.Departments.ShouldNotBeNull();
			result.Departments.Count.ShouldBe(0);

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);
		}

		#endregion

		#region FirstOrDefault — multiple associations, verify query count

		[Test]
		public void Select_Union_FirstOrDefault_MultipleAssociations(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors, _, _) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

			var result = (
					from d in tDep
					orderby d.Id
					select new
					{
						d.Id,
						d.Name,
						Employees = tEmp.Where(e => e.DepartmentId == d.Id)
							.OrderBy(e => e.Id).ToList(),
						Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
							.OrderBy(c => c.Id).ToList(),
					}
				)
				.AsUnionQuery()
				.FirstOrDefault();

			result.ShouldNotBeNull();

			var firstDept = rootDepts.OrderBy(d => d.Id).First();
			result.Id.ShouldBe(firstDept.Id);

			var expectedEmps = employees.Where(e => e.DepartmentId == firstDept.Id).OrderBy(e => e.Id).ToList();
			var expectedCtrs = contractors.Where(c => c.DepartmentId == firstDept.Id).OrderBy(c => c.Id).ToList();

			AreEqual(expectedEmps, result.Employees, ComparerBuilder.GetEqualityComparer(expectedEmps));
			AreEqual(expectedCtrs, result.Contractors, ComparerBuilder.GetEqualityComparer(expectedCtrs));

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 2 child queries)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 3 : 1);
		}

		#endregion

		#region Empty master — no rows returned, only 1 query executed

		[Test]
		public void Select_Union_EmptyMaster_OnlyOneQuery(
			[DataSources(true)] string context)
		{
			// Empty companies table — master returns nothing
			var companies   = Array.Empty<Company>();
			var departments = GenerateHierarchy().Item2;

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			var query = (
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
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			result.Count.ShouldBe(0);

			// TODO: When full UNION ALL optimization is implemented, assert counter.Count == 1
		}

		[Test]
		public void Select_Union_EmptyMaster_MultipleAssociations_OnlyOneQuery(
			[DataSources(true)] string context)
		{
			var (_, _, employees, contractors, _, _) = GenerateHierarchy();

			// Empty departments — master returns nothing
			var rootDepts = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

			var query = (
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
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			result.Count.ShouldBe(0);

			// TODO: When full UNION ALL optimization is implemented, assert counter.Count == 1
		}

		[Test]
		public void Select_Union_EmptyMaster_FirstOrDefault_OnlyOneQuery(
			[DataSources(true)] string context)
		{
			var companies   = Array.Empty<Company>();
			var departments = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			var result = (
					from c in tCo
					orderby c.Id
					select new
					{
						c.Id,
						Departments = tDep
							.Where(d => d.CompanyId == c.Id)
							.OrderBy(d => d.Id)
							.ToList(),
					}
				)
				.AsUnionQuery()
				.FirstOrDefault();

			result.ShouldBeNull();

			// TODO: When full UNION ALL optimization is implemented, assert counter.Count == 1
		}

		#endregion

		#region Global DefaultEagerLoadingStrategy — no AsUnionQuery() needed

		[Test]
		public void Select_GlobalUnion_InlineCollection(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);

			// Enable CteUnion globally — no AsUnionQuery() calls needed
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.CteUnion });

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			var query = (
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

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);
		}

		[Test]
		public void Select_GlobalUnion_MultipleAssociations(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors, _, _) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.CteUnion });

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

			var query = (
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
			);

			var result = query.ToList();

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

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 2 child queries)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 3 : 1);
		}

		[Test]
		public void Select_GlobalUnion_NestedTwoLevel(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.CteUnion });

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query = (
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
		public void LoadWith_GlobalUnion_SingleLevel(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.CteUnion });

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			// No AsUnionQuery() — global strategy applies
			var query = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id);

			var result = query.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expected = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList();
				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expected, ComparerBuilder.GetEqualityComparer(expected));
			}

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);
		}

		[Test]
		public void Select_GlobalUnion_FirstOrDefault(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.CteUnion });

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

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

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);
		}

		#endregion

		#region Non-equality operators and OR predicates

		[Test]
		public void Select_Union_GreaterThanOperator(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					HigherIdEmployees = tEmp
						.Where(e => e.DepartmentId > d.Id)
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					HigherIdEmployees = employees
						.Where(e => e.DepartmentId > d.Id)
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_LessThanOrEqualOperator(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, _, contractors, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(departments);
			using var tCtr = db.CreateLocalTable(contractors);

			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					LowerOrEqualContractors = tCtr
						.Where(c => c.DepartmentId <= d.Id)
						.OrderBy(c => c.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					LowerOrEqualContractors = contractors
						.Where(c => c.DepartmentId <= d.Id)
						.OrderBy(c => c.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_MixedOperators(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, contractors, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			// One collection uses >, another uses <=
			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId > d.Id)
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId <= d.Id)
						.OrderBy(c => c.Id).ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					Employees   = employees
						.Where(e => e.DepartmentId > d.Id)
						.OrderBy(e => e.Id).ToList(),
					Contractors = contractors
						.Where(c => c.DepartmentId <= d.Id)
						.OrderBy(c => c.Id).ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_OrPredicate(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// OR predicate: employees from this department OR with salary above threshold
			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					MatchingEmployees = tEmp
						.Where(e => e.DepartmentId == d.Id || e.Salary > 60000)
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					MatchingEmployees = employees
						.Where(e => e.DepartmentId == d.Id || e.Salary > 60000)
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_NotEqualOperator(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// != operator: employees NOT from this department
			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					OtherEmployees = tEmp
						.Where(e => e.DepartmentId != d.Id)
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					OtherEmployees = employees
						.Where(e => e.DepartmentId != d.Id)
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_OrWithMultipleParentKeys(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (_, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// OR using two parent columns: match by Id OR by CompanyId
			var query = (
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.CompanyId,
					d.Name,
					MatchingEmployees = tEmp
						.Where(e => e.DepartmentId == d.Id || e.DepartmentId == d.CompanyId)
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = departments
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.CompanyId,
					d.Name,
					MatchingEmployees = employees
						.Where(e => e.DepartmentId == d.Id || e.DepartmentId == d.CompanyId)
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Query cache validation — iteration 2 must hit cache with correct values

		[Test]
		public void Cache_Union_ParentFilterChanged(
			[DataSources(true, TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Filter on parent: iteration 1 → CompanyId <= 1, iteration 2 → CompanyId <= 2
			var maxCompanyId = iteration;

			var query =
				from c in tCo
				where c.Id <= maxCompanyId
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				};

			var cacheMiss = query.GetCacheMissCount();

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = companies
				.Where(c => c.Id <= maxCompanyId)
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

			if (iteration == 2)
			{
				// Second iteration must hit cache — no new cache miss
				query.GetCacheMissCount().ShouldBe(cacheMiss);
			}
		}

		[Test]
		public void Cache_Union_ChildFilterChanged(
			[DataSources(true)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Filter on children: iteration 1 → Salary > 50000, iteration 2 → Salary > 45000
			var minSalary = iteration == 1 ? 50000 : 45000;

			var query =
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
							HighPaidEmployees = tEmp
								.Where(e => e.DepartmentId == d.Id && e.Salary > minSalary)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				};

			var cacheMiss = query.GetCacheMissCount();

			var result = query
				.AsUnionQuery()
				.ToList();

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
							HighPaidEmployees = employees
								.Where(e => e.DepartmentId == d.Id && e.Salary > minSalary)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));

			if (iteration == 2)
			{
				query.GetCacheMissCount().ShouldBe(cacheMiss);
			}
		}

		// TODO: Handle Clickhouse correlated subquery in join expression
		[Test]
		public void Cache_Union_MultipleAssociationsFilterChanged(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, employees, contractors, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			// Filter on parent: iteration 1 → Id <= 2, iteration 2 → Id <= 3
			var maxId = iteration + 1;

			var query =
				from c in tCo
				where c.Id <= maxId
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
					Contractors = tCtr
						.Where(ct => tDep.Any(d => d.CompanyId == c.Id && d.Id == ct.DepartmentId))
						.OrderBy(ct => ct.Id)
						.ToList(),
				};

			var cacheMiss = query.GetCacheMissCount();

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = companies
				.Where(c => c.Id <= maxId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
					Contractors = contractors
						.Where(ct => departments.Any(d => d.CompanyId == c.Id && d.Id == ct.DepartmentId))
						.OrderBy(ct => ct.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));

			if (iteration == 2)
			{
				query.GetCacheMissCount().ShouldBe(cacheMiss);
			}
		}

		#endregion

		#region Root-level AsUnionQuery applies to all child collections

		[Test]
		public void RootAsUnionQuery_SingleChild(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// AsUnionQuery on root — no AsUnionQuery on child collection
			var query = 
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
				};

			var result = query
				.AsUnionQuery()
				.ToList();

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

		[Test]
		public void RootAsUnionQuery_MultipleChildren(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// AsUnionQuery on root — strategy propagates to both child collections
			var query = 
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
					Employees = tEmp
						.Where(e => tDep.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				};

			var result = query
				.AsUnionQuery()
				.ToList();

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
					Employees = employees
						.Where(e => departments.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void RootAsUnionQuery_NestedTwoLevel(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// AsUnionQuery on root — propagates through nested levels
			var query = 
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
				};

			var result = query
				.AsUnionQuery()
				.ToList();

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

		#region Association navigation properties with Union

		[Test]
		public void Association_Union_LoadWithSingleLevel(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// LoadWith using association navigation property
			var query = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id);

			var result = query
				.AsUnionQuery()
				.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expected = departments
					.Where(d => d.CompanyId == c.Id)
					.OrderBy(d => d.Id)
					.ToList();

				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expected, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void Association_Union_LoadWithThenLoad(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// LoadWith + ThenLoad using association navigation properties
			var query = tCo
				.LoadWith(c => c.Departments)
				.ThenLoad(d => d.Employees)
				.OrderBy(c => c.Id);

			var result = query
				.AsUnionQuery()
				.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expectedDepts = departments
					.Where(d => d.CompanyId == c.Id)
					.OrderBy(d => d.Id)
					.ToList();

				c.Departments.Count.ShouldBe(expectedDepts.Count);

				foreach (var d in c.Departments)
				{
					var expectedEmps = employees
						.Where(e => e.DepartmentId == d.Id)
						.OrderBy(e => e.Id)
						.ToList();

					d.Employees.OrderBy(e => e.Id).ToList()
						.ShouldBe(expectedEmps, ComparerBuilder.GetEqualityComparer(expectedEmps));
				}
			}
		}
		
		[Test]
		public void Association_Union_SelectNavigation([DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Select projection using association navigation property
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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

		[Test]
		public void Association_Union_SelectNestedNavigation(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Nested associations in Select projection: c.Departments → d.Employees
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = d.Employees
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
		public void Association_Union_RootAsUnionQueryWithNavigation([DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Root-level AsUnionQuery with association navigation properties (no per-child AsUnionQuery)
			var query =
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = d.Employees
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				};

			var result = query
				.AsUnionQuery()
				.ToList();

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

		#region Concat/Union with Predicate (eagerLoad.Predicate)

		[Test]
		public void Concat_Union_DifferentConstants(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllDB2)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db      = GetDataContext(context);
			using var tCo     = db.CreateLocalTable(companies);
			using var tDep    = db.CreateLocalTable(departments);
			var       counter = new SelectQueryCounter();

			if (!context.IsRemote()) db.AddInterceptor(counter);

			// Two Concat branches with same companies but different constants.
			// Both branches select all companies — if child results are grouped
			// incorrectly, duplicate departments would appear.
			var query1 =
				from c in tCo
				select new
				{
					Label       = "Small",
					c.Id,
					c.Name,
					Departments = c.Departments.OrderBy(d => d.Id).ToList(),
				};

			var query2 =
				from c in tCo
				select new
				{
					Label       = "Large",
					c.Id,
					c.Name,
					Departments = c.Departments.OrderBy(d => d.Id).ToList(),
				};

			var query  = query1
				.Concat(query2)
				.AsUnionQuery();

			var result = query.ToList();

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 1 child query)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 2 : 1);

			var expected = companies
				.Select(c => new
				{
					Label       = "Small",
					c.Id,
					c.Name,
					Departments = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList(),
				})
				.Concat(companies
					.Select(c => new
					{
						Label       = "Large",
						c.Id,
						c.Name,
						Departments = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList(),
					}))
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Concat_Union_DifferentChildFilters(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllDB2)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db      = GetDataContext(context);
			using var tCo     = db.CreateLocalTable(companies);
			using var tDep    = db.CreateLocalTable(departments);
			var       counter = new SelectQueryCounter();

			if (!context.IsRemote()) db.AddInterceptor(counter);

			// Two Concat branches with SAME companies but different child filters.
			// Both branches select all companies — if the predicate is not applied to
			// child queries, active-only and all departments would be mixed/duplicated.
			var query1 =
				from c in tCo
				select new
				{
					Label       = "ActiveOnly",
					c.Id,
					Departments = c.Departments.Where(d => d.IsActive).OrderBy(d => d.Id).ToList(),
				};

			var query2 =
				from c in tCo
				select new
				{
					Label       = "All",
					c.Id,
					Departments = c.Departments.OrderBy(d => d.Id).ToList(),
				};

			var query  = query1
				.Concat(query2)
				.AsUnionQuery();

			var result = query.ToList();

			// CteUnion: single query; non-CTE providers fall back to KeyedQuery (buffer + 2 child queries — one per Concat branch)
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 3 : 1);

			var expected = companies
				.Select(c => new
				{
					Label       = "ActiveOnly",
					c.Id,
					Departments = departments.Where(d => d.CompanyId == c.Id && d.IsActive).OrderBy(d => d.Id).ToList(),
				})
				.Concat(companies
					.Select(c => new
					{
						Label       = "All",
						c.Id,
						Departments = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList(),
					}))
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Union_Union_NestedEagerLoading(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllDB2)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Two UnionAll branches with same companies but different labels.
			// Both select all companies — overlapping IDs test that predicate is applied correctly.
			var query1 =
				from c in tCo
				select new
				{
					Label       = "First",
					c.Id,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = d.Employees.OrderBy(e => e.Id).ToList(),
						})
						.ToList(),
				};

			var query2 =
				from c in tCo
				select new
				{
					Label       = "Second",
					c.Id,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = d.Employees.OrderBy(e => e.Id).ToList(),
						})
						.ToList(),
				};

			var query  = query1.UnionAll(query2);
			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = companies
				.Select(c => new
				{
					Label       = "First",
					c.Id,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
						})
						.ToList(),
				})
				.Concat(companies
					.Select(c => new
					{
						Label       = "Second",
						c.Id,
						Departments = departments
							.Where(d => d.CompanyId == c.Id)
							.OrderBy(d => d.Id)
							.Select(d => new
							{
								d.Id,
								d.Name,
								Employees = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
							})
							.ToList(),
					}))
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Nested: 4-level Company → Departments → Employees → Tasks (via navigation)

		[Test]
		public void Select_Union_NestedFourLevel(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, tasks) = GenerateHierarchy();

			using var db    = GetDataContext(context);
			using var tCo   = db.CreateLocalTable(companies);
			using var tDep  = db.CreateLocalTable(departments);
			using var tEmp  = db.CreateLocalTable(employees);
			using var tTask = db.CreateLocalTable(tasks);

			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = c.Departments
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							Employees = d.Employees
								.OrderBy(e => e.Id)
								.Select(e => new
								{
									e.Id,
									e.Name,
									e.Salary,
									Tasks = e.Tasks
										.OrderBy(t => t.Id)
										.ToList(),
								})
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
								.Select(e => new
								{
									e.Id,
									e.Name,
									e.Salary,
									Tasks = tasks
										.Where(t => t.EmployeeId == e.Id)
										.OrderBy(t => t.Id)
										.ToList(),
								})
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region CollectOrderBy traverses Select projection

		[Test]
		public void Select_Union_OrderByBeforeSelect(
			[DataSources(true)] string context)
		{
			var (companies, departments, _, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// OrderBy sits BELOW Select — CollectOrderBy must walk through the Select
			// and remap (Department d => d.Id) → (projected p => p.DeptId)
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new { DeptId = d.Id, DeptName = d.Name })
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new { DeptId = d.Id, DeptName = d.Name })
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_Union_OrderByBeforeSelectWrapped(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// OrderBy below Select that wraps entity — tests nested member remapping
			// CollectOrderBy must remap (Department d => d.Id) → (projected p => p.Dept.Id)
			var query = (
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
							Dept      = d,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.ToList(),
						})
						.ToList(),
				}
			);

			var result = query
				.AsUnionQuery()
				.ToList();

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
							Dept      = d,
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

		#region ToDictionary inside Select

		[Test]
		public void Select_Union_ToDictionaryInSelect(
			[DataSources(true, TestProvName.AllAccess)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query =
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DepartmentEmployees = tDep
						.Where(d => d.CompanyId == c.Id)
						.ToDictionary(
							d => d.Id),
				};

			var result = query
				.AsUnionQuery()
				.ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					DepartmentEmployees = departments
						.Where(d => d.CompanyId == c.Id)
						.ToDictionary(d => d.Id),
				})
				.ToList();

			result.Count.ShouldBe(expected.Count);
			for (int i = 0; i < expected.Count; i++)
			{
				result[i].Id.ShouldBe(expected[i].Id);
				result[i].Name.ShouldBe(expected[i].Name);
				result[i].DepartmentEmployees.Count.ShouldBe(expected[i].DepartmentEmployees.Count);
				foreach (var kvp in expected[i].DepartmentEmployees)
				{
					result[i].DepartmentEmployees.ShouldContainKey(kvp.Key);
					var actual = result[i].DepartmentEmployees[kvp.Key];
					actual.Id.ShouldBe(kvp.Value.Id);
					actual.Name.ShouldBe(kvp.Value.Name);
					actual.CompanyId.ShouldBe(kvp.Value.CompanyId);
				}
			}
		}

		[Test]
		public void Select_Union_ToDictionaryInSelectNested(
			[DataSources(true)] string context)
		{
			var (companies, departments, employees, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query =
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
							EmployeesByKey = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.ToDictionary(e => e.Id),
						})
						.ToList(),
				};

			var result = query
				.AsUnionQuery()
				.ToList();

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
							EmployeesByKey = employees
								.Where(e => e.DepartmentId == d.Id)
								.ToDictionary(e => e.Id),
						})
						.ToList(),
				})
				.ToList();

			result.Count.ShouldBe(expected.Count);
			for (int i = 0; i < expected.Count; i++)
			{
				result[i].Id.ShouldBe(expected[i].Id);
				result[i].Name.ShouldBe(expected[i].Name);
				result[i].Departments.Count.ShouldBe(expected[i].Departments.Count);
				for (int j = 0; j < expected[i].Departments.Count; j++)
				{
					var expDept = expected[i].Departments[j];
					var actDept = result[i].Departments[j];
					actDept.Id.ShouldBe(expDept.Id);
					actDept.Name.ShouldBe(expDept.Name);
					actDept.EmployeesByKey.Count.ShouldBe(expDept.EmployeesByKey.Count);
					foreach (var kvp in expDept.EmployeesByKey)
					{
						actDept.EmployeesByKey.ShouldContainKey(kvp.Key);
						var actual = actDept.EmployeesByKey[kvp.Key];
						actual.Id.ShouldBe(kvp.Value.Id);
						actual.Name.ShouldBe(kvp.Value.Name);
						actual.DepartmentId.ShouldBe(kvp.Value.DepartmentId);
					}
				}
			}
		}

		#endregion
	}
}
