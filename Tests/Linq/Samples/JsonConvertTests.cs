using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using Newtonsoft.Json;

using NUnit.Framework;

namespace Tests.Samples
{
	[AttributeUsage(AttributeTargets.Property)]
	public class JsonContentAttribute : Attribute
	{
	}

	public static class MappingHelper
	{
		private static MethodInfo _deserializeMethod = MemberHelper.MethodOf(() => JsonConvert.DeserializeObject(null!, typeof(int)));
		private static MethodInfo _serializeMethod = MemberHelper.MethodOf(() => JsonConvert.SerializeObject(null));
		private static ConstructorInfo _dataParamContructor = typeof(DataParameter).GetConstructor(new[] { typeof(string), typeof(object) })!;

		public static void GenerateConvertorsForTables(Type dataConnectionType, MappingSchema ms)
		{
			var propsWithTables = dataConnectionType.GetProperties()
				.Where(p => typeof(IQueryable<>).IsSameOrParentOf(p.PropertyType));

			var types = propsWithTables.Select(p => p.PropertyType.GenericTypeArguments[0]).Distinct().ToArray();
			foreach (var t in types)
			{
				GenerateConvertors(t, ms);
			}
		}

		public static void GenerateConvertors(Type entityType, MappingSchema ms)
		{
			foreach (var propertyInfo in entityType.GetProperties().Where(p => p.HasAttribute<JsonContentAttribute>()))
			{
				// emulating inParam => JsonConvert.DeserializeObject(inParam, propertyInfo.PropertyType)

				var inParam = Expression.Parameter(typeof(string));
				var lambda = Expression.Lambda(
					Expression.Convert(
						Expression.Call(null, _deserializeMethod, inParam,
							Expression.Constant(propertyInfo.PropertyType)),
						propertyInfo.PropertyType),
					inParam);

				ms.SetConvertExpression(typeof(string), propertyInfo.PropertyType, lambda);

				var inObjParam = Expression.Parameter(propertyInfo.PropertyType);
				var lambdaSql = Expression.Lambda(
					Expression.New(_dataParamContructor, Expression.Constant("p"),
						Expression.Call(null, _serializeMethod, inObjParam))
					, inObjParam);

				ms.SetConvertExpression(propertyInfo.PropertyType, typeof(DataParameter), lambdaSql);
			}
		}
	}

	public class MyDataConnection : DataConnection
	{
		private static MappingSchema _convertorSchema = new();

		static MyDataConnection()
		{
			MappingHelper.GenerateConvertorsForTables(typeof(MyDataConnection), _convertorSchema);
		}

		public MyDataConnection(string providerName, string connectionString, MappingSchema mappingSchema) : base(new DataOptions().UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema))
		{
			AddMappingSchema(_convertorSchema);
		}

		public MyDataConnection(string configurationString) : base(configurationString)
		{
			AddMappingSchema(_convertorSchema);
		}

		public ITable<SampleClass> SampleClass => this.GetTable<SampleClass>();
	}

	[Table]
	public class SampleClass
	{
		[Column] public int Id    { get; set; }

		[Column(DataType = DataType.VarChar, Length = 4000), JsonContent]
		public DataClass? Data { get; set; }
	}

	public class DataClass
	{
		public string? Property1 { get; set; }
	}

	public static class Json
	{
		sealed class JsonValueBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var pathExpr = builder.Arguments[0];
				if (pathExpr.NodeType != ExpressionType.MemberAccess)
					throw new NotSupportedException();

				var pathList = new List<Expression>();
				var current = pathExpr;
				while (true)
				{
					pathList.Add(current);
					if (current.NodeType == ExpressionType.MemberAccess)
					{
						current = ((MemberExpression) current).Expression!;
					}
					else
						break;
				}

				pathList.Reverse();

				var entity = pathList[0];
				var field  = pathList[1];

				var fieldSql = builder.ConvertExpressionToSql(field)!;
				builder.AddParameter("field", fieldSql);

				var propPathStr = "$";
				for (int i = 2; i < pathList.Count; i++)
				{
					propPathStr += "." + ((MemberExpression) pathList[i]).Member.Name;
				}

				builder.AddParameter("propPath", new SqlValue(propPathStr));
			}
		}

		[Sql.Extension("JSON_VALUE({field}, {propPath})", Precedence = Precedence.Primary, BuilderType = typeof(JsonValueBuilder), ServerSideOnly = true, CanBeNull = false)]
		public static string Value(object? path)
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture]
	public class JsonConvertTests : TestBase
	{
		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = new MyDataConnection(context);
			using var table = db.CreateLocalTable<SampleClass>();
			db.Insert(new SampleClass { Id = 1, Data = new DataClass { Property1 = "Pr1" } });

			var objects = table.Where(t => Json.Value(t.Data!.Property1) == "Pr1")
					.ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.LastQuery!, Does.Not.Contain("IS NULL"));

				Assert.That(objects, Has.Length.EqualTo(1));
			}

			Assert.That(objects[0].Data!.Property1, Is.EqualTo("Pr1"));
		}
	}
}
