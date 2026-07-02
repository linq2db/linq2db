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

		#region Global DefaultEagerLoadingStrategy — no WithKeyedLoadStrategy() needed

		[Test]
		public void Select_GlobalKeyedQuery_InlineCollection(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			// Enable KeyedQuery globally — no WithKeyedLoadStrategy() calls needed
			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

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

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

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

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

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

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			if (!context.IsRemote()) counter.Count = 0;

			// No WithKeyedLoadStrategy() — global strategy applies
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

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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

		[ActiveIssue(Configuration = TestProvName.AllYdb, Details = "YDB Re2.PatternFromLike (LIKE UDF) requires a non-nullable String pattern; building it from a nullable column (the parent Name referenced in the child-filter method call) yields Optional<Utf8>, giving 'Mismatch type argument #1: String != Optional<Utf8>'. Needs nullable-pattern coercion in the YDB LIKE translation.")]
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
			).WithKeyedLoadStrategy();

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
					Workers = d.Employees.OrderBy(e => e.Id).Select(e => new { e.Id, e.Name }).ToList(),
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
					Workers = cGroup.OrderBy(c => c.Id).Select(c => new { c.Id, c.Name }).ToList(),
				};

			var query  = query1.Concat(query2).WithKeyedLoadStrategy();
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
						.OrderBy(e => e.Id)
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
							.OrderBy(c => c.Id)
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
				.WithKeyedLoadStrategy();

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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
		public void RootWithKeyedLoadStrategy_SingleChild(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			var query =
				from c in tCo.WithKeyedLoadStrategy()
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
		public void RootWithKeyedLoadStrategy_MultipleChildren(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			var query =
				from d in tDep.WithKeyedLoadStrategy()
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
				.WithKeyedLoadStrategy()
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
			).WithKeyedLoadStrategy().Single();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy();

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
			).WithKeyedLoadStrategy().FirstAsync();

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
				.WithKeyedLoadStrategy();

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

			var result = query.WithKeyedLoadStrategy().ToList();

			result.Count.ShouldBe(companies.Length);
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Id.ShouldBe(companies[i].Id);
				result[i].f60.ShouldBe(companies[i].Id * 60);
				result[i].Departments.Count.ShouldBe(departments.Count(d => d.CompanyId == companies[i].Id));
			}
		}

		#endregion

		#region Concurrency — shared cached query must not share per-execution key state

		// Regression for MAJ001: KeyedQueryKeysHolder used to be build-time state baked into the
		// cached query, so concurrent executions of the same cached query clobbered each other's
		// key sets — producing empty or cross-loaded child collections. Keys are now isolated per
		// execution via the preamble-results array, so this must stay green under concurrency.
		// Scoped to providers whose CreateLocalTable tables are visible across connections.
		[Test]
		public async Task KeyedQuery_ConcurrentExecutions_DoNotShareKeyState(
			[IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);
			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);

			// Warm the query cache once so every worker hits the same cached Query<T> (the shared holder).
			RunKeyed(db, 3);

			const int workers            = 8;
			const int iterationsPerWorker = 12;

			var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();

			var tasks = Enumerable.Range(0, workers).Select(w => Task.Run(() =>
			{
				for (var it = 0; it < iterationsPerWorker; it++)
				{
					// Vary the filter so concurrent executions extract different key sets.
					var maxId = (w + it) % 3 + 1;

					try
					{
						using var ldb = GetDataContext(context);

						var rows = RunKeyed(ldb, maxId);

						var gotDeptIds = rows.SelectMany(r => r.Departments).Select(d => d.Id).OrderBy(id => id).ToList();
						var expDeptIds = departments.Where(d => d.CompanyId <= maxId).Select(d => d.Id).OrderBy(id => id).ToList();

						if (!gotDeptIds.SequenceEqual(expDeptIds))
							failures.Enqueue($"maxId={maxId}: expected depts [{string.Join(",", expDeptIds)}], got [{string.Join(",", gotDeptIds)}]");

						foreach (var r in rows)
							foreach (var d in r.Departments)
								if (d.CompanyId != r.Id)
									failures.Enqueue($"maxId={maxId}: company {r.Id} got foreign dept {d.Id} (CompanyId={d.CompanyId})");
					}
					catch (Exception ex)
					{
						failures.Enqueue($"maxId={maxId}: {ex.GetType().Name}: {ex.Message}");
					}
				}
			})).ToArray();

			await Task.WhenAll(tasks);

			failures.ShouldBeEmpty();
		}

		// Company → Departments eager load under the KeyedQuery strategy, resolved by type so it can
		// run on any context (the shared tables are created once by the caller).
		static List<CompanyWithDepartments> RunKeyed(IDataContext db, int maxId)
		{
			var query =
				from c in db.GetTable<Company>()
				where c.Id <= maxId
				orderby c.Id
				select new CompanyWithDepartments
				{
					Id          = c.Id,
					Departments = db.GetTable<Department>()
						.Where(d => d.CompanyId == c.Id)
						.OrderBy(d => d.Id)
						.ToList(),
				};

			return query.WithKeyedLoadStrategy().ToList();
		}

		sealed class CompanyWithDepartments
		{
			public int              Id          { get; set; }
			public List<Department> Departments { get; set; } = null!;
		}

		#endregion

		#region Default strategy — runtime-parameter regression

		// Regression: DetachedPreamble and Preamble<TKey,T> in ExpressionBuilder.EagerLoadDefault.cs
		// used to call query.GetResultEnumerable(…, preambles, preambles) — passing preambles in both
		// the parameters and preambles slots. Any captured local variable that feeds a SQL parameter on
		// the detail query arrived as null (or empty) because the real parameters array was never forwarded.
		// This test ensures that a captured local variable (minId) reaches the detail query correctly.
		[Test]
		public void Select_DefaultStrategy_RuntimeParameterReachesDetailQuery(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (_, departments, employees, _, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db   = GetDataContext(context);
			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);

			int minId = 2;

			var query =
				from d in tDep
				orderby d.Id
				select new
				{
					d.Id,
					d.Name,
					Employees = tEmp
						.Where(e => e.DepartmentId == d.Id && e.Id >= minId)
						.OrderBy(e => e.Id)
						.ToList(),
				};

			var result = query
				.WithSeparateLoadStrategy()
				.ToList();

			var expected = rootDepts
				.OrderBy(d => d.Id)
				.Select(d => new
				{
					d.Id,
					d.Name,
					Employees = employees
						.Where(e => e.DepartmentId == d.Id && e.Id >= minId)
						.OrderBy(e => e.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region WithSeparateLoadStrategy — explicit Default strategy override

		/// <summary>
		/// Returns <see langword="true"/> when the provider supports Common Table Expressions.
		/// Non-CTE providers fall back from CteUnion to KeyedQuery.
		/// </summary>
		static bool IsCteSupported(string context)
			=> !context.IsAnyOf(TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllMySql57);

		// MAJ001: root marker — global KeyedQuery overridden to Default.
		[Test]
		public void WithSeparateLoadStrategy_OverridesGlobalKeyedQuery(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, employees, _, _) = GenerateHierarchy();

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));

			using var tCo  = db.CreateLocalTable(companies);
			using var tDep = db.CreateLocalTable(departments);
			using var tEmp = db.CreateLocalTable(employees);

			// WithSeparateLoadStrategy() on the root query overrides the global KeyedQuery default
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
				.WithSeparateLoadStrategy()
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

		// MIN001: per-call WithUnionLoadStrategy() marker wins over global DefaultEagerLoadingStrategy = Default.
		// Verified via query count: Default produces N+1 separate queries; CteUnion collapses 2 same-level
		// child collections into a single UNION ALL query (counter == 1 on CTE providers).
		[Test]
		public void WithUnionLoadStrategy_OverridesGlobalDefault_QueryCountDistinguishesStrategy(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase, TestProvName.AllFirebirdLess3)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			// Scope to one company's departments so the parent row count is predictable
			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			// Global default is Default (N+1 separate queries); WithUnionLoadStrategy() must override it
			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.Default));

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

			// Two same-level child collections: CteUnion collapses them into 1 query (vs 3 under KeyedQuery
			// fallback, or N+1 per child under Default).  Single-child queries fall back to KeyedQuery even
			// under CteUnion, so 2 children are required to observe the UNION ALL single-query path.
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
				.WithUnionLoadStrategy()
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

			// CteUnion (marker won): 1 UNION ALL query loads both child collections.
			// Non-CTE providers fall back to KeyedQuery: 1 buffer preamble + 2 child queries = 3.
			if (!context.IsRemote()) counter.Count.ShouldBe(!IsCteSupported(context) ? 3 : 1);
		}

		// Last-marker-wins precedence: the OUTERMOST (last-applied) marker decides the strategy; inner markers
		// are ignored. Outer WithUnionLoadStrategy() beats inner WithKeyedLoadStrategy(), so 2 same-level child
		// collections collapse into one UNION ALL (count == 1 on SQLite, which supports CTE). The strategy is
		// resolved at build time and is provider-independent, so one CTE provider validates the precedence.
		[Test]
		public void WithLoadStrategy_LastMarkerWins_OuterUnionOverInnerKeyed(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			// Global default would be N+1 separate queries; the outer marker must override it to CteUnion.
			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.Default));

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

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
				.WithKeyedLoadStrategy() // inner — ignored
				.WithUnionLoadStrategy() // outer — wins
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

			// Outer CteUnion won: a single UNION ALL query loads both child collections.
			if (!context.IsRemote()) counter.Count.ShouldBe(1);
		}

		// Inverse order: outer WithKeyedLoadStrategy() beats inner WithUnionLoadStrategy(), so KeyedQuery runs
		// (1 buffer preamble + 2 child queries = 3) even though SQLite supports CTE — proving the rule is
		// "outermost wins", not "CteUnion always wins".
		[Test]
		public void WithLoadStrategy_LastMarkerWins_OuterKeyedOverInnerUnion(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var (_, departments, employees, contractors, _) = GenerateHierarchy();

			var rootDepts = departments.Where(d => d.CompanyId == 1).ToArray();

			using var db = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.Default));

			var counter = new SelectQueryCounter();
			if (!context.IsRemote()) db.AddInterceptor(counter);

			using var tDep = db.CreateLocalTable(rootDepts);
			using var tEmp = db.CreateLocalTable(employees);
			using var tCtr = db.CreateLocalTable(contractors);

			if (!context.IsRemote()) counter.Count = 0;

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
				.WithUnionLoadStrategy() // inner — ignored
				.WithKeyedLoadStrategy() // outer — wins
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

			// Outer KeyedQuery won: 1 buffer preamble + 2 child queries = 3.
			if (!context.IsRemote()) counter.Count.ShouldBe(3);
		}

		#endregion

		#region Composite primary key — KeyedQuery with a two-column PK

		[Table]
		sealed class CompositeParent
		{
			[Column, PrimaryKey(1)] public int     Region { get; set; }
			[Column, PrimaryKey(2)] public int     Code   { get; set; }
			[Column]                public string? Name   { get; set; }
		}

		[Table]
		sealed class CompositeChild
		{
			[Column, PrimaryKey] public int     Id           { get; set; }
			[Column]             public int     ParentRegion { get; set; }
			[Column]             public int     ParentCode   { get; set; }
			[Column]             public string? Value        { get; set; }
		}

		[Test]
		public void Select_KeyedQuery_CompositeKey_ChildrenAttachedCorrectly(
			[DataSources(true, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var parents = new[]
			{
				new CompositeParent { Region = 1, Code = 10, Name = "P1_10" },
				new CompositeParent { Region = 1, Code = 20, Name = "P1_20" },
				new CompositeParent { Region = 2, Code = 10, Name = "P2_10" },
			};

			var children = new[]
			{
				new CompositeChild { Id = 1, ParentRegion = 1, ParentCode = 10, Value = "A" },
				new CompositeChild { Id = 2, ParentRegion = 1, ParentCode = 10, Value = "B" },
				new CompositeChild { Id = 3, ParentRegion = 1, ParentCode = 20, Value = "C" },
				new CompositeChild { Id = 4, ParentRegion = 2, ParentCode = 10, Value = "D" },
			};

			using var db  = GetDataContext(context);
			using var tP  = db.CreateLocalTable(parents);
			using var tC  = db.CreateLocalTable(children);

			var query =
				from p in tP
				orderby p.Region, p.Code
				select new
				{
					p.Region,
					p.Code,
					p.Name,
					Children = tC
						.Where(c => c.ParentRegion == p.Region && c.ParentCode == p.Code)
						.OrderBy(c => c.Id)
						.ToList(),
				};

			var result = query
				.WithKeyedLoadStrategy()
				.ToList();

			var expected = parents
				.OrderBy(p => p.Region).ThenBy(p => p.Code)
				.Select(p => new
				{
					p.Region,
					p.Code,
					p.Name,
					Children = children
						.Where(c => c.ParentRegion == p.Region && c.ParentCode == p.Code)
						.OrderBy(c => c.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expected, result, ComparerBuilder.GetEqualityComparer(expected));
		}

		#endregion

		#region Deterministic key order — string keys inlined in ordinal order (#5664)

		[Table]
		sealed class StringKeyParent
		{
			[Column, PrimaryKey] public string  Code { get; set; } = null!;
			[Column]             public string? Name { get; set; }
		}

		[Table]
		sealed class StringKeyChild
		{
			[Column, PrimaryKey] public int     Id         { get; set; }
			[Column]             public string  ParentCode { get; set; } = null!;
			[Column]             public string? Value      { get; set; }
		}

		sealed class CommandTextCollector : CommandInterceptor
		{
			public List<string> Commands { get; } = new();

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Commands.Add(command.CommandText);
				return command;
			}
		}

		// #5664: the master keys inlined into the detail query's VALUES/IN list must be ordered
		// deterministically, so direct and remote (LinqService) execution emit identical SQL. The order
		// must be ordinal, not culture-linguistic: "zzB" and "zza" are distinct string keys whose ordinal
		// order ('B' 0x42 < 'a' 0x61) is the reverse of their linguistic order (a < B). A culture-sensitive
		// comparer would order them machine-culture-dependently (and unstably for canonically-equal strings),
		// which is exactly the divergence #5664 fixes. Assert the inlined keys appear in ordinal order.
		[Test]
		public void Select_KeyedQuery_StringKeys_InlinedInOrdinalOrder(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var parents = new[]
			{
				new StringKeyParent { Code = "zza", Name = "P_a" },
				new StringKeyParent { Code = "zzB", Name = "P_B" },
			};

			var children = new[]
			{
				new StringKeyChild { Id = 1, ParentCode = "zza", Value = "a1" },
				new StringKeyChild { Id = 2, ParentCode = "zzB", Value = "B1" },
			};

			var collector = new CommandTextCollector();

			using var db = GetDataContext(context);
			db.AddInterceptor(collector);

			using var tP = db.CreateLocalTable(parents);
			using var tC = db.CreateLocalTable(children);

			// Drop the table-creation / insert commands — only the query commands matter below.
			collector.Commands.Clear();

			var query =
				from p in tP
				select new
				{
					p.Code,
					p.Name,
					Children = tC
						.Where(c => c.ParentCode == p.Code)
						.OrderBy(c => c.Id)
						.ToList(),
				};

			var result = query.WithKeyedLoadStrategy().ToList();

			result.Count.ShouldBe(2);

			// The child detail query is the single command that inlines both parent-code keys.
			var detailSql = collector.Commands.SingleOrDefault(sql => sql.Contains("'zzB'") && sql.Contains("'zza'"));
			detailSql.ShouldNotBeNull();

			// Ordinal order: 'B' sorts before 'a'; culture-linguistic order would be the reverse.
			detailSql.IndexOf("'zzB'", StringComparison.Ordinal)
				.ShouldBeLessThan(detailSql.IndexOf("'zza'", StringComparison.Ordinal));
		}

		#endregion

		#region Nullable FK key — orphan children must not attach to any parent

		[Table]
		sealed class NullableParent
		{
			[Column, PrimaryKey] public int     Id   { get; set; }
			[Column]             public string? Name { get; set; }
		}

		[Table]
		sealed class NullableChild
		{
			[Column, PrimaryKey] public int     Id       { get; set; }
			[Column]             public int?    ParentId { get; set; }
			[Column]             public string? Name     { get; set; }
		}

		[Test]
		public void Select_KeyedQuery_NullableFK_OrphanChildrenNotAttachedToAnyParent(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var parents = new[]
			{
				new NullableParent { Id = 1, Name = "P1" },
				new NullableParent { Id = 2, Name = "P2" },
			};

			var children = new[]
			{
				new NullableChild { Id = 10, ParentId = 1,    Name = "C1_P1"   },
				new NullableChild { Id = 11, ParentId = 1,    Name = "C2_P1"   },
				new NullableChild { Id = 20, ParentId = 2,    Name = "C1_P2"   },
				new NullableChild { Id = 30, ParentId = null, Name = "Orphan1" },
				new NullableChild { Id = 31, ParentId = null, Name = "Orphan2" },
			};

			using var db  = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.KeyedQuery));
			using var tP  = db.CreateLocalTable(parents);
			using var tC  = db.CreateLocalTable(children);

			// --- KeyedQuery strategy ---

			var queryKeyed =
				from p in tP
				orderby p.Id
				select new
				{
					p.Id,
					p.Name,
					Children = tC
						.Where(c => c.ParentId == p.Id)
						.OrderBy(c => c.Id)
						.ToList(),
				};

			var resultKeyed = queryKeyed.ToList();

			var expectedKeyed = parents
				.OrderBy(p => p.Id)
				.Select(p => new
				{
					p.Id,
					p.Name,
					Children = children
						.Where(c => c.ParentId == p.Id)
						.OrderBy(c => c.Id)
						.ToList(),
				})
				.ToList();

			AreEqual(expectedKeyed, resultKeyed, ComparerBuilder.GetEqualityComparer(expectedKeyed));

			// Orphan children (ParentId = null) must not appear under any parent.
			resultKeyed.SelectMany(p => p.Children).Any(c => c.ParentId == null).ShouldBeFalse();

			// --- CteUnion strategy (falls back to KeyedQuery when CTEs or window functions are unsupported) ---

			var queryCteUnion =
				from p in tP
				orderby p.Id
				select new
				{
					p.Id,
					p.Name,
					Children = tC
						.Where(c => c.ParentId == p.Id)
						.OrderBy(c => c.Id)
						.ToList(),
				};

			var resultUnion = queryCteUnion
				.WithUnionLoadStrategy()
				.ToList();

			AreEqual(expectedKeyed, resultUnion, ComparerBuilder.GetEqualityComparer(expectedKeyed));

			resultUnion.SelectMany(p => p.Children).Any(c => c.ParentId == null).ShouldBeFalse();
		}

		#endregion
	}
}
