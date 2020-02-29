using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Hosting;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using NUnit.Framework;

namespace Tests.Microsoft
{
	[TestFixture]
	public class MicrosoftODataTests : TestBase
	{

		[Table(Name = "odata_person")]
		public class PersonClass
		{
			[Column("Name"), PrimaryKey]
			public string Name { get; set; }
			[Column("YearsExperience"), NotNull]
			public int YearsExperience { get; set; }
			[Column("Title"), NotNull]
			public string Title { get; set; }
		}

		private static MethodInfo _toArray = MemberHelper.MethodOf<IQueryable<int>>(q => q.ToArray()).GetGenericMethodDefinition();

		private static ICollection Materialize(IQueryable query)
		{
			var elementType = query.ElementType;
			var method = _toArray.MakeGenericMethod(elementType);
			return (ICollection)method.Invoke(null, new object[] { query });
		}

		[Test]
		public void SelectViaOData([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
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

				var request = new HttpRequestMessage()
				{
					Method = HttpMethod.Get,
					RequestUri =
						new Uri(
							"http://localhost:15580/odata/PersonClass?$apply=groupby((Title),aggregate(YearsExperience%20with%20sum%20as%20TotalExperience))")
				};

				var config = new HttpConfiguration();
				config.EnableDependencyInjection();

				request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, config);

				var options = new ODataQueryOptions(queryContext, request);

				var resultQuery = options.ApplyTo(table);
				var materialized = Materialize(resultQuery);
				Assert.That(materialized.Count, Is.EqualTo(1));
			}
		}

		private static PersonClass[] GenerateTestData()
		{
			return new []{
				new PersonClass
				{
					Title = "Common Title",
					Name = "N1",
					YearsExperience = 3,
				},
				new PersonClass
				{
					Title = "Common Title",
					Name = "N2",
					YearsExperience = 4,
				}
			};
		}

		class NamedProperty
		{
			public string Name { get; set; }
			public object Value { get; set; }

		}

		class GroupByWrapper
		{
			public virtual AggregationPropertyContainer GroupByContainer { get; set; }
			public virtual AggregationPropertyContainer Container { get; set; }
		}

		class AggregationWrapper : GroupByWrapper
		{
		}

		class AggregationPropertyContainer : NamedProperty
		{
			public class LastInChain : AggregationPropertyContainer
			{
			}
		}

		class FlatteningWrapper<T>: GroupByWrapper
		{
			public T Source { get; set; }
		}

		[Test]
		public void SelectPure([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
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
								Value =  ((IEnumerable<FlatteningWrapper<PersonClass>>)it).Sum(it2 => (int)it2.GroupByContainer.Value)
							}
						});

				var materialized = query.ToArray();

				Assert.That(materialized.Length, Is.EqualTo(1));
			}
		}

	}
}
