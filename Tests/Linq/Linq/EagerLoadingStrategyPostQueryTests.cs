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

			var interns = departments
				.Where(d => d.Id % 2 == 0) // even-id depts get interns
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

		#region Basic PostQuery — single level

		[Test]
		public void LoadWith_PostQuery_SingleLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = tCo
				.LoadWith(c => c.Departments.AsKeyedQuery())
				.OrderBy(c => c.Id);

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
		public void Select_PostQuery_InlineCollection(
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
						.AsKeyedQuery()
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

		#region Select with filter on children

		[Test]
		public void Select_PostQuery_FilteredChildren(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
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
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id)
						.AsKeyedQuery()
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
		}

		#endregion

		#region 3-level flat: root loads 2 levels of children independently

		[Test]
		public void Select_PostQuery_ThreeLevelFlat(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Root query loads departments AND employees independently (3 entity types, PostQuery)
			var query = (
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
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, contractors, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
					InactiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && !d.IsActive)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select_PostQuery_ScalarAndCollection(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Mix scalar projections with PostQuery collection
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
						.AsKeyedQuery()
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		public void Select_PostQuery_ParentWithTake(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Take first 2 companies, load departments via PostQuery
			var query = (
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
			).Take(2);

			var result = query.ToList();

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
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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

		#endregion

		#region Nested: 3-level Company → Departments → Employees + Contractors

		[Test]
		public void Select_PostQuery_NestedThreeLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, contractors, _) = GenerateHierarchy();

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
			);

			var result = query.ToList();

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
		public void Select_PostQuery_NestedThreeCollectionsAtThirdLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, contractors, interns) = GenerateHierarchy();

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
							Interns = tInt
								.Where(i => i.DepartmentId == d.Id)
								.AsKeyedQuery()
								.OrderBy(i => i.Id)
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
		public void Select_PostQuery_NestedWithFilters(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
			);

			var result = query.ToList();

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
		public void Select_PostQuery_NestedScalarAndCollection(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
			);

			var result = query.ToList();

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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();
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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
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

			var query = (
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
			);

			var result = query.ToList();

			result.Count.ShouldBe(0);

			// Buffer preamble returns empty, child query is skipped = 1 SELECT only
			counter.Count.ShouldBe(1);
		}

		[Test]
		public void Select_PostQuery_EmptyMaster_MultipleAssociations_OnlyOneQuery(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, _, employees, contractors, _) = GenerateHierarchy();

			// Empty departments — master returns nothing
			var rootDepts = Array.Empty<Department>();

			using var db = GetDataContext(context);

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			counter.Count = 0;

			var query = (
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
			);

			var result = query.ToList();

			result.Count.ShouldBe(0);

			// Buffer preamble returns empty, both child queries skipped = 1 SELECT only
			counter.Count.ShouldBe(1);
		}

		[Test]
		public void Select_PostQuery_EmptyMaster_FirstOrDefault_OnlyOneQuery(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);

			// Enable PostQuery globally — no AsKeyedQuery() calls needed
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

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

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalPostQuery_MultipleAssociations(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			counter.Count = 0;

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

			// 1 buffer preamble + 2 child queries = 3 SELECT queries
			counter.Count.ShouldBe(3);
		}

		[Test]
		public void Select_GlobalPostQuery_NestedTwoLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

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
		public void LoadWith_GlobalPostQuery_SingleLevel(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.PostQuery });

			var counter = new SelectQueryCounter();
			db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			counter.Count = 0;

			// No AsKeyedQuery() — global strategy applies
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

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalPostQuery_FirstOrDefault(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

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

		#region Non-equality operators and OR predicates

		[Test]
		public void Select_PostQuery_GreaterThanOperator(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		public void Select_PostQuery_LessThanOrEqualOperator(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, _, contractors, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(c => c.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		public void Select_PostQuery_MixedOperators(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId <= d.Id)
						.AsKeyedQuery()
						.OrderBy(c => c.Id).ToList(),
				}
			);

			var result = query.ToList();

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
		public void Select_PostQuery_OrPredicate(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		public void Select_PostQuery_NotEqualOperator(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		public void Select_PostQuery_OrWithMultipleParentKeys(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(e => e.Id)
						.ToList(),
				}
			);

			var result = query.ToList();

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
		public void Cache_PostQuery_ParentFilterChanged(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
				};

			var cacheMiss = query.GetCacheMissCount();

			var result = query.ToList();

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
		public void Cache_PostQuery_ChildFilterChanged(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
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

		[Test]
		public void Cache_PostQuery_MultipleAssociationsFilterChanged(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllSybase)] string context,
			[Values(1, 2)] int iteration)
		{
			var (companies, departments, employees, contractors, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.ToList(),
					Contractors = tCtr
						.Where(ct => tDep.Any(d => d.CompanyId == c.Id && d.Id == ct.DepartmentId))
						.AsKeyedQuery()
						.OrderBy(ct => ct.Id)
						.ToList(),
				};

			var cacheMiss = query.GetCacheMissCount();

			var result = query.ToList();

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

		#region Root-level AsKeyedQuery applies to all child collections

		[Test]
		public void RootAsKeyedQuery_SingleChild(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// AsKeyedQuery on root — no AsKeyedQuery on child collection
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
			).AsKeyedQuery();

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

		[Test]
		public void RootAsKeyedQuery_MultipleChildren(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// AsKeyedQuery on root — strategy propagates to both child collections
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
					Employees = tEmp
						.Where(e => tDep.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				}
			).AsKeyedQuery();

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
					Employees = employees
						.Where(e => departments.Any(d => d.CompanyId == c.Id && d.Id == e.DepartmentId))
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void RootAsKeyedQuery_NestedTwoLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// AsKeyedQuery on root — propagates through nested levels
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
			).AsKeyedQuery();

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

		#endregion

		#region Association navigation properties with PostQuery

		[Test]
		public void Association_PostQuery_LoadWithSingleLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// LoadWith using association navigation property
			var query = tCo
				.LoadWith(c => c.Departments.AsKeyedQuery())
				.OrderBy(c => c.Id);

			var result = query.ToList();

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
		public void Association_PostQuery_LoadWithThenLoad(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// LoadWith + ThenLoad using association navigation properties
			var query = tCo
				.LoadWith(c => c.Departments.AsKeyedQuery())
				.ThenLoad(d => d.Employees)
				.OrderBy(c => c.Id);

			var result = query.ToList();

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
		public void Association_PostQuery_SelectNavigation(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
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

		[Test]
		public void Association_PostQuery_SelectNestedNavigation(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
						.AsKeyedQuery()
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
		public void Association_PostQuery_RootAsKeyedQueryWithNavigation(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Root-level AsKeyedQuery with association navigation properties (no per-child AsKeyedQuery)
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
			).AsKeyedQuery();

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

		#endregion

		#region Child projection referencing non-key parent fields

		[Test]
		public void Select_PostQuery_ChildProjectsParentFieldFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Child anonymous object includes c.Name (parent non-key field)
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							CompanyName = c.Name,
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
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							CompanyName = c.Name,
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_PostQuery_NestedChildProjectsGrandparentFieldFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// Nested: employee projection includes c.Name (grandparent non-key field)
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							Employees = tEmp
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.Select(e => new
								{
									e.Id,
									e.Name,
									CompanyName = c.Name,
								})
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
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							Employees = employees
								.Where(e => e.DepartmentId == d.Id)
								.OrderBy(e => e.Id)
								.Select(e => new
								{
									e.Id,
									e.Name,
									CompanyName = c.Name,
								})
								.ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_PostQuery_ChildProjectsParentFieldVerifyValuesFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Child uses c.Name (non-key parent field) → falls back to Default strategy
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.Select(d => new
						{
							d.Id,
							d.Name,
							CompanyName = c.Name,
						})
						.ToList(),
				}
			);

			var result = query.ToList();

			result.Count.ShouldBe(companies.Length);

			// Verify that parent field values are correctly propagated to each child row
			foreach (var c in result)
			{
				var company = companies.First(co => co.Id == c.Id);
				foreach (var d in c.Departments)
				{
					d.CompanyName.ShouldBe(company.Name);
				}
			}
		}

		[Test]
		public void Select_PostQuery_MixedChildrenSomeWithParentFieldsFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// One child references c.Name (falls back to Default), another doesn't (stays PostQuery)
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					// This child references parent non-key field → Default fallback
					DeptWithCompanyName = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							CompanyName = c.Name,
						})
						.ToList(),
					// This child uses only FK key → PostQuery (Contains/VALUES)
					PlainDepts = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
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
					DeptWithCompanyName = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							CompanyName = c.Name,
						})
						.ToList(),
					PlainDepts = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_PostQuery_ChildProjectsParentExpressionFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Child projection uses expression involving parent field: c.Name + " dept"
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.AsKeyedQuery()
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							Label = c.Name + " / " + d.Name,
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
					Departments = departments
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							Label = c.Name + " / " + d.Name,
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[Test]
		public void Select_PostQuery_ChildFilterUsesParentInMethodCallFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Child filter uses parent field inside a method call (string.StartsWith),
			// not a simple binary comparison → falls back to Default
			var query = (
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id && d.Name!.StartsWith(c.Name!))
						.AsKeyedQuery()
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
					Departments = departments
						.Where(d => d.CompanyId == c.Id && d.Name!.StartsWith(c.Name!))
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion
	}
}
