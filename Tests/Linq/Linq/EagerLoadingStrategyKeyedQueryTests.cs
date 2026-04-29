using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class EagerLoadingStrategyKeyedQueryTests : TestBase
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

		#region Global DefaultEagerLoadingStrategy — no AsKeyedQuery() needed

		[Test]
		public void Select_GlobalKeyedQuery_InlineCollection(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);

			// Enable KeyedQuery globally — no AsKeyedQuery() calls needed
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.KeyedQuery });

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

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			if (!context.IsRemote()) counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalKeyedQuery_MultipleAssociations(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.KeyedQuery });

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

			// 1 buffer preamble + 2 child queries = 3 SELECT queries
			if (!context.IsRemote()) counter.Count.ShouldBe(3);
		}

		[Test]
		public void Select_GlobalKeyedQuery_NestedTwoLevel(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.KeyedQuery });

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
		public void LoadWith_GlobalKeyedQuery_SingleLevel(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.KeyedQuery });

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

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
			if (!context.IsRemote()) counter.Count.ShouldBe(2);
		}

		[Test]
		public void Select_GlobalKeyedQuery_FirstOrDefault(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { DefaultEagerLoadingStrategy = EagerLoadingStrategy.KeyedQuery });

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

			// 1 buffer preamble + 1 child query = 2 SELECT queries
			if (!context.IsRemote()) counter.Count.ShouldBe(2);
		}

		#endregion

		#region Child projection referencing non-key parent fields

		[Test]
		public void Select_KeyedQuery_ChildProjectsParentFieldFallback(
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
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							d.Name,
							CompanyName = c.Name,
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
		public void Select_KeyedQuery_NestedChildProjectsGrandparentFieldFallback(
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
			).AsKeyedQuery();

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
		public void Select_KeyedQuery_ChildProjectsParentFieldVerifyValuesFallback(
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
						.Select(d => new
						{
							d.Id,
							d.Name,
							CompanyName = c.Name,
						})
						.ToList(),
				}
			).AsKeyedQuery();

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
		public void Select_KeyedQuery_MixedChildrenSomeWithParentFieldsFallback(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// One child references c.Name (falls back to Default), another doesn't (stays KeyedQuery)
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
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							CompanyName = c.Name,
						})
						.ToList(),
					// This child uses only FK key → KeyedQuery (Contains/VALUES)
					PlainDepts = tDep
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
		public void Select_KeyedQuery_ChildProjectsParentExpressionFallback(
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
						.OrderBy(d => d.Id)
						.Select(d => new
						{
							d.Id,
							Label = c.Name + " / " + d.Name,
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
		public void Select_KeyedQuery_ChildFilterUsesParentInMethodCallFallback(
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
					Departments = departments
						.Where(d => d.CompanyId == c.Id && d.Name!.StartsWith(c.Name!))
						.OrderBy(d => d.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Concat with different eager-loaded details

		[Test]
		public void Concat_KeyedQuery_EagerLoadDifferentDetails(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllClickHouse)] string context)
		{
			var (companies, departments, employees, contractors, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			// Branch 1: departments with filtered employees
			var query1 =
				from d in tDep
					.LoadWith(d => d.Employees, eq => eq.Where(e => e.Salary > 45000))
				where d.IsActive
				select new
				{
					d.Id,
					d.Name,
					Kind    = "Active",
					Workers = d.Employees.Select(e => new { e.Id, e.Name }).ToList(),
				};

			// Branch 2: departments with contractors (different child type)
			var query2 =
				from d in tDep
				where !d.IsActive
				join c in tCtr on d.Id equals c.DepartmentId into cGroup
				select new
				{
					d.Id,
					d.Name,
					Kind    = "Inactive",
					Workers = cGroup.Select(c => new { c.Id, c.Name }).ToList(),
				};

			var query  = query1.Concat(query2).AsKeyedQuery();
			var result = query.ToList();

			var expected = departments
				.Where(d => d.IsActive)
				.Select(d => new
				{
					d.Id,
					d.Name,
					Kind    = "Active",
					Workers = employees
						.Where(e => e.DepartmentId == d.Id && e.Salary > 45000)
						.Select(e => new { e.Id, e.Name })
						.ToList(),
				})
				.Concat(departments
					.Where(d => !d.IsActive)
					.Select(d => new
					{
						d.Id,
						d.Name,
						Kind    = "Inactive",
						Workers = contractors
							.Where(c => c.DepartmentId == d.Id)
							.Select(c => new { c.Id, c.Name })
							.ToList(),
					}))
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Tests ported from UnionTests — basic patterns

		[Test]
		public void LoadWith_KeyedQuery_SingleLevel(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id)
				.AsKeyedQuery();

			var result = query.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expected = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList();
				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expected, ComparerBuilder.GetEqualityComparer(expected));
			}
		}

		[Test]
		public void Select_KeyedQuery_InlineCollection(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

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
				.AsKeyedQuery()
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
		public void Select_KeyedQuery_FilteredChildren(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query =
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					ActiveDepts = tDep
						.Where(d => d.CompanyId == c.Id && d.IsActive)
						.OrderBy(d => d.Id)
						.ToList(),
				};

			var result = query
				.AsKeyedQuery()
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

		[Test]
		public void Select_KeyedQuery_MultipleAssociations(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var query =
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id).OrderBy(c => c.Id).ToList(),
				};

			var result = query
				.AsKeyedQuery()
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

		[Test]
		public void Select_KeyedQuery_NestedTwoLevel(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

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
							Employees = tEmp.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
						})
						.ToList(),
				};

			var result = query
				.AsKeyedQuery()
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
							Employees = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
						})
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void Select_KeyedQuery_ScalarAndCollection(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query =
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
				};

			var result = query
				.AsKeyedQuery()
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

		#region Tests ported from UnionTests — FirstOrDefault

		[Test]
		public void Select_KeyedQuery_FirstOrDefault_SingleAssociation(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

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
				.AsKeyedQuery()
				.FirstOrDefault();

			result.ShouldNotBeNull();
			result.Id.ShouldBe(1);
			result.Departments.Count.ShouldBe(departments.Count(d => d.CompanyId == 1));
		}

		[Test]
		public void Select_KeyedQuery_EmptyMaster_OnlyOneQuery(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			using var db      = GetDataContext(context);
			using var tCo     = db.CreateLocalTable<Company>();
			using var tDep    = db.CreateLocalTable<Department>();
			var       counter = new SelectQueryCounter();

			if (!context.IsRemote()) db.AddInterceptor(counter);

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
				.AsKeyedQuery()
				.ToList();

			result.Count.ShouldBe(0);
			if (!context.IsRemote()) counter.Count.ShouldBe(1);
		}

		#endregion

		#region Tests ported from UnionTests — Association navigation

		[Test]
		public void Association_KeyedQuery_LoadWithSingleLevel(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = tCo
				.LoadWith(c => c.Departments)
				.OrderBy(c => c.Id);

			var result = query
				.AsKeyedQuery()
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
		public void Association_KeyedQuery_LoadWithThenLoad(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			var query = tCo
				.LoadWith(c => c.Departments)
				.ThenLoad(d => d.Employees)
				.OrderBy(c => c.Id);

			var result = query
				.AsKeyedQuery()
				.ToList();

			result.Count.ShouldBe(companies.Length);

			foreach (var c in result)
			{
				var expectedDepts = departments.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList();
				c.Departments.Count.ShouldBe(expectedDepts.Count);

				foreach (var d in c.Departments)
				{
					var expectedEmps = employees.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList();
					d.Employees.OrderBy(e => e.Id).ToList()
						.ShouldBe(expectedEmps, ComparerBuilder.GetEqualityComparer(expectedEmps));
				}
			}
		}

		#endregion

		#region Tests ported from UnionTests — Root marker

		[Test]
		public void RootAsKeyedQuery_SingleChild(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query =
				from c in tCo.AsKeyedQuery()
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
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var query =
				from d in tDep.AsKeyedQuery()
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees   = tEmp.Where(e => e.DepartmentId == d.Id).OrderBy(e => e.Id).ToList(),
					Contractors = tCtr.Where(c => c.DepartmentId == d.Id).OrderBy(c => c.Id).ToList(),
				};

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

		#region Tests ported from UnionTests — ToDictionary

		[Test]
		public void Select_KeyedQuery_ToDictionaryInSelect(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query =
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					DepartmentsByKey = tDep
						.Where(d => d.CompanyId == c.Id)
						.ToDictionary(d => d.Id),
				};

			var result = query
				.AsKeyedQuery()
				.ToList();

			var expected = companies
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					DepartmentsByKey = departments
						.Where(d => d.CompanyId == c.Id)
						.ToDictionary(d => d.Id),
				})
				.ToList();

			result.Count.ShouldBe(expected.Count);
			for (int i = 0; i < expected.Count; i++)
			{
				result[i].Id.ShouldBe(expected[i].Id);
				result[i].Name.ShouldBe(expected[i].Name);
				result[i].DepartmentsByKey.Count.ShouldBe(expected[i].DepartmentsByKey.Count);
				foreach (var kvp in expected[i].DepartmentsByKey)
				{
					result[i].DepartmentsByKey.ShouldContainKey(kvp.Key);
					var actual = result[i].DepartmentsByKey[kvp.Key];
					actual.Id.ShouldBe(kvp.Value.Id);
					actual.Name.ShouldBe(kvp.Value.Name);
					actual.CompanyId.ShouldBe(kvp.Value.CompanyId);
				}
			}
		}

		#endregion

		#region Cardinality semantics — Single / SingleAsync

		[Test]
		public void Select_KeyedQuery_Single_OneParent(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var result = (
				from c in tCo
				where c.Id == 1
				select new
				{
					c.Id,
					c.Name,
					Departments = tDep
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				}
			).AsKeyedQuery().Single();

			result.Id.ShouldBe(1);
			result.Departments.Count.ShouldBe(departments.Count(d => d.CompanyId == 1));
		}

		[Test]
		public void Select_KeyedQuery_Single_MultipleParents_Throws(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// No filter — multiple companies match. Single() must throw the standard
			// LINQ "more than one element" InvalidOperationException.
			var query = (
				from c in tCo
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

			var act = () => query.Single();
			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void Select_KeyedQuery_SingleOrDefault_MultipleParents_Throws(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = (
				from c in tCo
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

			var act = () => query.SingleOrDefault();
			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void Select_KeyedQuery_SingleAsync_MultipleParents_Throws(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query = (
				from c in tCo
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

			Assert.ThrowsAsync<InvalidOperationException>(async () => await query.SingleAsync());
		}

		[Test]
		public async Task Select_KeyedQuery_FirstAsync_OneParent(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var result = await (
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
			).AsKeyedQuery().FirstAsync();

			var expectedFirst = companies.OrderBy(c => c.Id).First();
			result.Id.ShouldBe(expectedFirst.Id);
			result.Departments.Count.ShouldBe(departments.Count(d => d.CompanyId == expectedFirst.Id));
		}

		#endregion

		#region Mixed-operator dependency — Contains rewrite gating

		[Test]
		public void LoadWith_KeyedQuery_MixedOperatorDependency(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// The predicate references parent's Id in BOTH equality (CompanyId == c.Id) AND
			// non-equality (Id > c.Id). The Contains rewrite path can only handle the equality
			// half; the gating helper forces a fall-back to SelectMany + VALUES JOIN, which
			// resolves both references correctly.
			var query = tCo
				.LoadWith(c => c.Departments.Where(d => d.CompanyId == c.Id && d.Id > c.Id))
				.OrderBy(c => c.Id)
				.AsKeyedQuery();

			var result = query.ToList();

			result.Count.ShouldBe(companies.Length);
			foreach (var c in result)
			{
				var expectedDepts = departments
					.Where(d => d.CompanyId == c.Id && d.Id > c.Id)
					.OrderBy(d => d.Id)
					.ToList();
				c.Departments.OrderBy(d => d.Id).ToList()
					.ShouldBe(expectedDepts, ComparerBuilder.GetEqualityComparer(expectedDepts));
			}
		}

		#endregion

		#region Wide projection — exercises BuildValueTupleType beyond 56 fields

		[Test]
		public void Select_KeyedQuery_WideProjection(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// 60 distinct scalar slots in the projection — used to throw with the old
			// 56-field cap on BuildValueTupleType. Each `c.Id * N` is a distinct binary
			// SQL expression that lands as its own placeholder in the buffer carrier.
			var query =
				from c in tCo
				orderby c.Id
				select new
				{
					c.Id,
					c.Name,
					f01 = c.Id *  1, f02 = c.Id *  2, f03 = c.Id *  3, f04 = c.Id *  4, f05 = c.Id *  5,
					f06 = c.Id *  6, f07 = c.Id *  7, f08 = c.Id *  8, f09 = c.Id *  9, f10 = c.Id * 10,
					f11 = c.Id * 11, f12 = c.Id * 12, f13 = c.Id * 13, f14 = c.Id * 14, f15 = c.Id * 15,
					f16 = c.Id * 16, f17 = c.Id * 17, f18 = c.Id * 18, f19 = c.Id * 19, f20 = c.Id * 20,
					f21 = c.Id * 21, f22 = c.Id * 22, f23 = c.Id * 23, f24 = c.Id * 24, f25 = c.Id * 25,
					f26 = c.Id * 26, f27 = c.Id * 27, f28 = c.Id * 28, f29 = c.Id * 29, f30 = c.Id * 30,
					f31 = c.Id * 31, f32 = c.Id * 32, f33 = c.Id * 33, f34 = c.Id * 34, f35 = c.Id * 35,
					f36 = c.Id * 36, f37 = c.Id * 37, f38 = c.Id * 38, f39 = c.Id * 39, f40 = c.Id * 40,
					f41 = c.Id * 41, f42 = c.Id * 42, f43 = c.Id * 43, f44 = c.Id * 44, f45 = c.Id * 45,
					f46 = c.Id * 46, f47 = c.Id * 47, f48 = c.Id * 48, f49 = c.Id * 49, f50 = c.Id * 50,
					f51 = c.Id * 51, f52 = c.Id * 52, f53 = c.Id * 53, f54 = c.Id * 54, f55 = c.Id * 55,
					f56 = c.Id * 56, f57 = c.Id * 57, f58 = c.Id * 58, f59 = c.Id * 59, f60 = c.Id * 60,
					Departments = tDep.Where(d => d.CompanyId == c.Id).OrderBy(d => d.Id).ToList(),
				};

			var result = query.AsKeyedQuery().ToList();

			result.Count.ShouldBe(companies.Length);
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Id.ShouldBe(companies[i].Id);
				result[i].f60.ShouldBe(companies[i].Id * 60);
				result[i].Departments.Count.ShouldBe(departments.Count(d => d.CompanyId == companies[i].Id));
			}
		}

		#endregion
	}
}
