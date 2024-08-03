using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using NUnit.Framework;

#if NETFRAMEWORK
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
#else
using Microsoft.OData.UriParser;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Tests.OData.Microsoft
{
	[TestFixture]
	public class MicrosoftODataTests : TestBase
	{

		[Table(Name = "odata_person")]
		public class PersonClass
		{
			[Column("Name", Length = 50, CanBeNull = false), PrimaryKey]
			public string Name { get; set; } = null!;
			[Column("YearsExperience"), NotNull]
			public int YearsExperience { get; set; }
			[Column("Title"), NotNull]
			public string Title { get; set; } = null!;
		}

		private static MethodInfo _toArray = MemberHelper.MethodOf<IQueryable<int>>(q => q.ToArray()).GetGenericMethodDefinition();

		private static ICollection Materialize(IQueryable query)
		{
			var elementType = query.ElementType;
			var method = _toArray.MakeGenericMethod(elementType);
			return (ICollection)method.Invoke(null, new object[] { query })!;
		}

		public class ODataQueries
		{
			private readonly string _name;

			public ODataQueries(string testCaseName, string query)
			{
				_name = testCaseName;
				Query = query;
			}

			public string Query { get; }

			public override string ToString() => _name;
		}

		public static IEnumerable<ODataQueries> ODataQueriesTestCases
		{
			get
			{
				yield return new ODataQueries("Query 01", "/odata/PersonClass?$apply=groupby((Title),aggregate(YearsExperience%20with%20sum%20as%20TotalExperience))");
				yield return new ODataQueries("Query 02", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with countdistinct as Test))");
				yield return new ODataQueries("Query 03", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with sum as TotalExperience))&$orderby=TotalExperience");
				yield return new ODataQueries("Query 04", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with min as Test))&$orderby=Test");
				yield return new ODataQueries("Query 05", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with max as Test))&$orderby=Test");
				yield return new ODataQueries("Query 06", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with average as Test))&$orderby=Test");
				yield return new ODataQueries("Query 07", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with countdistinct as Test))&$orderby=Test");
				yield return new ODataQueries("Query 08", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with sum as Test))");
				yield return new ODataQueries("Query 09", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with average as Test))");
				yield return new ODataQueries("Query 10", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with min as Test))");
				yield return new ODataQueries("Query 11", "/odata/People?$apply=groupby((Title),aggregate(YearsExperience with max as Test))");
				yield return new ODataQueries("Query 12", "/odata/People?$apply=groupby((Title),aggregate($count as NumPeople))&$orderby=NumPeople");
				yield return new ODataQueries("Query 13", "/odata/People?$apply=groupby((Title),aggregate($count as NumPeople))&$count=true");
				yield return new ODataQueries("Query 14", "/odata/People?$apply=filter(Title eq 'Engineer' or Title eq 'QA')/groupby((Title),aggregate($count as NumPeople))&$count=true");
				//"/odata/People?$apply=groupby((Office/Name),aggregate($count as NumPeople))&$count=true",
				//"/odata/People?$apply=filter(Title eq 'QA')/groupby((Office/Id,Office/Name),aggregate($count as NumPeople))&$count=true&$orderby=NumPeople desc"
			}
		}

		[Test]
		public void SelectViaOData(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[ValueSource(nameof(ODataQueriesTestCases))] ODataQueries testCase)
		{
			var modelBuilder = new ODataModelBuilder();
			var person = modelBuilder.EntityType<PersonClass>();
			person.HasKey(p => p.Name);
			person.Property(p => p.Title);
			person.Property(p => p.YearsExperience);

			var model = modelBuilder.GetEdmModel();
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var path = new ODataPath();
				ODataQueryContext queryContext = new ODataQueryContext(model, typeof(PersonClass), path);

				var uri = new Uri("http://localhost:15580" + testCase.Query);
#if NETFRAMEWORK
				using var request = new HttpRequestMessage()
				{
					Method = HttpMethod.Get,
					RequestUri = uri
				};

				var config = new HttpConfiguration();
				config.EnableDependencyInjection();

				request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
#else
				// https://github.com/OData/AspNetCoreOData/blob/master/test/Microsoft.AspNetCore.OData.Tests/Extensions/RequestFactory.cs#L78
				var  httpContext    = new DefaultHttpContext();
				HttpRequest request = httpContext.Request;

				IServiceCollection services = new ServiceCollection();
				httpContext.RequestServices = services.BuildServiceProvider();

				request.Method      = "GET";
				request.Scheme      = uri.Scheme;
				request.Host        = uri.IsDefaultPort ? new HostString(uri.Host) : new HostString(uri.Host, uri.Port);
				request.QueryString = new QueryString(uri.Query);
				request.Path        = new PathString(uri.AbsolutePath);
#endif
				var options = new ODataQueryOptions(queryContext, request);

				var resultQuery  = options.ApplyTo(table);
				var materialized = Materialize(resultQuery);
				Assert.That(materialized, Has.Count.EqualTo(1));
			}
		}

		private static PersonClass[] GenerateTestData()
		{
			return new []{
				new PersonClass
				{
					Title = "Engineer",
					Name = "N1",
					YearsExperience = 3,
				},
				new PersonClass
				{
					Title = "Engineer",
					Name = "N2",
					YearsExperience = 4,
				}
			};
		}

		class NamedProperty
		{
			public string  Name  { get; set; } = null!;
			public object? Value { get; set; }

		}

		class GroupByWrapper
		{
			public virtual AggregationPropertyContainer GroupByContainer { get; set; } = null!;
			public virtual AggregationPropertyContainer Container { get; set; } = null!;
		}

		sealed class AggregationWrapper : GroupByWrapper
		{
		}

		class AggregationPropertyContainer : NamedProperty
		{
			public sealed class LastInChain : AggregationPropertyContainer
			{
			}
		}

		sealed class FlatteningWrapper<T>: GroupByWrapper
		{
			public T Source { get; set; } = default!;
		}

		[Test]
		public void SelectPure([IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{

				var query = table
					.Select(it => new FlatteningWrapper<PersonClass>
					{
						Source = it,
						GroupByContainer = new AggregationPropertyContainer
						{
							Name = "Property0",
							Value = it.YearsExperience
						}
					})
					.GroupBy(
						it => new GroupByWrapper
						{
							GroupByContainer = new AggregationPropertyContainer.LastInChain
							{
								Name = "Title",
								Value = (it.Source == null) ? null : it.Source.Title
							}
						})
					.Select(
						it => new AggregationWrapper
						{
							GroupByContainer = it.Key.GroupByContainer,
							Container = new AggregationPropertyContainer
							{
								Name = "TotalExperience",
								Value =  ((IEnumerable<FlatteningWrapper<PersonClass>>)it).Sum(it2 => (int)it2.GroupByContainer.Value!)
							}
						});

				var materialized = query.ToArray();

				Assert.That(materialized, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public void SelectPure2([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{

				var query = table
					.Select(
						it => new FlatteningWrapper<PersonClass>
						{
							Source = it,
							GroupByContainer = new AggregationPropertyContainer.LastInChain
							{
								Name = "Property0",
								Value = it.YearsExperience
							}
						})
					.GroupBy(
						it => new GroupByWrapper
						{
							GroupByContainer = new AggregationPropertyContainer.LastInChain
							{
								Name = "Title",
								Value = (it.Source == null) ? null : it.Source.Title
							}
						})
					.Select(
						it => new AggregationWrapper
						{
							GroupByContainer = it.Key.GroupByContainer,
							Container = new AggregationPropertyContainer.LastInChain
							{
								Name = "Test",
								// Value = ((IEnumerable<FlatteningWrapper<MicrosoftODataTests.PersonClass>>)it)
								Value = it
									.Select(it2 => (int)it2.GroupByContainer.Value!)
									.Distinct()
									.LongCount()
							}
						});

				var materialized = query.ToArray();

				Assert.That(materialized, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public void SelectPure2Simplified([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from it in table
					group it by new { Name = "Title", it.Title }
					into g
					select new
					{
						Title = g.Key.Title,
						Test = g.Select(_ => _.Title).Distinct().LongCount()
					};

				var materialized = query.ToArray();
			}
		}

		#region Issue 3757 Data
		[Test]
		public void Issue3757Test([DataSources(TestProvName.AllClickHouse)] string context, [Values("", "?$filter=Children/any(c: contains(c/LS, 'de'))")] string queryString)
		{
			var modelBuilder = new ODataModelBuilder();

			var level1 = modelBuilder.EntityType<Issue3757Level1>();
			level1.HasKey(p => p.ID);
			level1.Property(p => p.ValS);
			level1.Property(p => p.ValB);
			level1.Property(p => p.ValInt);

			var level2 = modelBuilder.EntityType<Issue3757Level2>();
			level2.HasKey(p => p.ID);
			level2.HasKey(p => p.ParentId);
			level2.Property(p => p.ValS);
			level2.Property(p => p.ValB);
			level2.Property(p => p.ValInt);

			var projection = modelBuilder.ComplexType<Issue3757ODataModel>();
			projection.Property(p => p.Id);
			projection.Property(p => p.L1B);
			projection.Property(p => p.L1S);
			projection.Property(p => p.L1I);
			projection.CollectionProperty(p => p.Children);

			var cjildProjection = modelBuilder.ComplexType<Issue3757ODataChild>();
			cjildProjection.Property(p => p.Id);
			cjildProjection.Property(p => p.LB);
			cjildProjection.Property(p => p.LS);
			cjildProjection.Property(p => p.LI);

			var model = modelBuilder.GetEdmModel();

			using var db = GetDataContext(context);
			var path = new ODataPath();
			ODataQueryContext queryContext = new ODataQueryContext(model, typeof(Issue3757ODataModel), path);

			var uri = new Uri($"http://localhost:15580/odata/Issue3757{queryString}");
#if NETFRAMEWORK
			using var request = new HttpRequestMessage()
			{
				Method = HttpMethod.Get,
				RequestUri = uri
			};

			var config = new HttpConfiguration();
			config.EnableDependencyInjection();

			request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);
#else
			// https://github.com/OData/AspNetCoreOData/blob/master/test/Microsoft.AspNetCore.OData.Tests/Extensions/RequestFactory.cs#L78
			var  httpContext    = new DefaultHttpContext();
			HttpRequest request = httpContext.Request;

			IServiceCollection services = new ServiceCollection();
			httpContext.RequestServices = services.BuildServiceProvider();

			request.Method = "GET";
			request.Scheme = uri.Scheme;
			request.Host = uri.IsDefaultPort ? new HostString(uri.Host) : new HostString(uri.Host, uri.Port);
			request.QueryString = new QueryString(uri.Query);
			request.Path = new PathString(uri.AbsolutePath);
#endif
			var options = new ODataQueryOptions(queryContext, request);

			IQueryable resultQuery;
			using var t1 = db.CreateLocalTable<Issue3757Level1>();
			using var t2 = db.CreateLocalTable<Issue3757Level2>();

			var query = from l1 in t1
						select new Issue3757ODataModel
						{
							Id = l1.ID,
							L1B = l1.ValB,
							L1S = l1.ValS,
							L1I = l1.ValInt,
							Children = from l2 in t2
									   where l1.ID == l2.ParentId
									   select new Issue3757ODataChild
									   {
										   Id = l2.ID,
										   LB = l2.ValB,
										   LS = l1.ValS, // #### Important: l1 instead of l2
										   LI = l2.ValInt
									   }
						};
			resultQuery = options.ApplyTo(query);
			var materialized = Materialize(resultQuery);
		}

		[Table]
		public class Issue3757Level1
		{
			[PrimaryKey] public int ID { get; set; }
			[Column] public string? ValS { get; set; }
			[Column] public bool? ValB { get; set; }
			[Column] public int? ValInt { get; set; }
		}
		[Table]
		public class Issue3757Level2
		{
			[PrimaryKey] public int ID { get; set; }
			[Column] public int ParentId { get; set; }
			[Column] public string? ValS { get; set; }
			[Column] public bool? ValB { get; set; }
			[Column] public int? ValInt { get; set; }
		}

		public class Issue3757ODataModel
		{
			public int Id { get; set; }
			public string? L1S { get; set; }
			public bool? L1B { get; set; }
			public int? L1I { get; set; }
			public IEnumerable<Issue3757ODataChild> Children { get; set; } = null!;
		}
		public class Issue3757ODataChild
		{
			public int Id { get; set; }
			public string? LS { get; set; }
			public bool? LB { get; set; }
			public int? LI { get; set; }
		}
		#endregion
	}
}
