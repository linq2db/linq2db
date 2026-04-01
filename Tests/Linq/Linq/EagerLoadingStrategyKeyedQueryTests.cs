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
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var (companies, departments, _, _, _) = GenerateHierarchy();

			using var db   = GetDataContext(context);

			// Enable KeyedQuery globally — no AsKeyedQuery() calls needed
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
		public void Select_GlobalKeyedQuery_MultipleAssociations(
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
		public void Select_GlobalKeyedQuery_NestedTwoLevel(
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
		public void LoadWith_GlobalKeyedQuery_SingleLevel(
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
		public void Select_GlobalKeyedQuery_FirstOrDefault(
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
	}
}
