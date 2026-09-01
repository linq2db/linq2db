using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// Value, read by <see cref="Sql.IExtensionCallBuilder"/> from an argument, must be a part of query cache key.
	/// https://github.com/linq2db/linq2db/issues/5769
	/// </summary>
	[TestFixture]
	public class Issue5769Tests : TestBase
	{
		[Table]
		sealed class JsonData
		{
			[Column, PrimaryKey              ] public int     Id    { get; set; }
			[Column(DataType = DataType.Text)] public string? Value { get; set; }
		}

		sealed class JsonPathBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var value = builder.GetExpression(0)!;
				var path  = builder.GetValue<List<string>>(1)!;

				var sb         = new StringBuilder("({0}::json");
				var parameters = new List<ISqlExpression>(path.Count);

				for (var i = 0; i < path.Count; i++)
				{
					parameters.Add(new SqlValue(path[i]));
					sb
						.Append(i == path.Count - 1 ? "->>" : "->")
						.Append('{')
						.Append((i + 1).ToString(CultureInfo.InvariantCulture))
						.Append('}');
				}

				sb.Append(')');

				builder.ResultExpression = new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), sb.ToString(), Precedence.Primary, [value, .. parameters]);
			}
		}

		[Sql.Extension("", BuilderType = typeof(JsonPathBuilder), ServerSideOnly = true, CanBeNull = true)]
		static string? JsonValue(string? value, List<string> path)
		{
			if (value == null)
				return null;

			using var document = JsonDocument.Parse(value);

			var element = document.RootElement;

			foreach (var name in path)
			{
				if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var property))
					return null;

				element = property;
			}

			return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
		}

		sealed class PathLiteralBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<string>(0)!);
			}
		}

		[Sql.Extension("", BuilderType = typeof(PathLiteralBuilder), ServerSideOnly = true)]
		static string PathLiteral(string path) => path;

		sealed class PathListLiteralBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(string.Join(".", builder.GetValue<List<string>>(0)!));
			}
		}

		[Sql.Extension("", BuilderType = typeof(PathListLiteralBuilder), ServerSideOnly = true)]
		static string PathListLiteral(List<string> path) => string.Join(".", path);

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769")]
		public void BuilderValueIsPartOfCacheKey([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
			[
				new JsonData { Id = 1, Value = "sub.name" }
			]);

			var path = "sub.name";

			AssertQuery(table.Where(r => PathLiteral(path) == r.Value)).ShouldHaveSingleItem();

			path = "sub.name2";

			AssertQuery(table.Where(r => PathLiteral(path) == r.Value)).ShouldBeEmpty();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void EqualBuilderValueIsStillCached([DataSources] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
			[
				new JsonData { Id = 1, Value = "sub.name" }
			]);

			// same content, but a new list instance on each iteration
			var path = new List<string> { "sub", "name" };

			var query     = table.Where(r => PathListLiteral(path) == r.Value);
			var cacheMiss = query.GetCacheMissCount();

			query.ToArray().ShouldHaveSingleItem();

			if (iteration == 2)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769")]
		public void JsonPathIsPartOfCacheKey([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
			[
				new JsonData { Id = 1, Value = /*lang=json,strict*/ """{"sub":{"name":"findme","name2":"dontfindme"}}""" }
			]);

			// PostgreSQL registers List<string> as a scalar (array) type, so builder argument value could be
			// erroneously treated as a parameter and excluded from query cache key.
			var path = new List<string> { "sub", "name" };

			AssertQuery(table.Where(r => JsonValue(r.Value, path) == "findme")).ShouldHaveSingleItem();

			path = ["sub", "name2"];

			AssertQuery(table.Where(r => JsonValue(r.Value, path) == "findme")).ShouldBeEmpty();
		}
	}
}
