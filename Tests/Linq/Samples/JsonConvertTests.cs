using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Playground
{

	[AttributeUsage(AttributeTargets.Property)]
	public class JSonContentAttribute : Attribute
	{
	}

	public static class MappingHelper
	{
		private static MethodInfo _deserializeMethod;
		private static MethodInfo _serializeMethod;
		private static ConstructorInfo _dataParamContructor;

		static MappingHelper()
		{
			_deserializeMethod = MemberHelper.MethodOf(() => JsonConvert.DeserializeObject(null, typeof(int)));
			_serializeMethod = MemberHelper.MethodOf(() => JsonConvert.SerializeObject(null));
			_dataParamContructor = typeof(DataParameter).GetConstructor(new[] { typeof(string), typeof(object) });
		}

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
			foreach (var propertyInfo in entityType.GetProperties().Where(p => p.GetCustomAttribute(typeof(JSonContentAttribute)) != null))
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
		private static MappingSchema _convertorSchema;

		static MyDataConnection()
		{
			_convertorSchema = new MappingSchema();
			MappingHelper.GenerateConvertorsForTables(typeof(MyDataConnection), _convertorSchema);
		}

		public MyDataConnection(string providerName, string connectionString, MappingSchema mappingSchema) : base(providerName, connectionString, mappingSchema)
		{
			AddMappingSchema(_convertorSchema);
		}

		public MyDataConnection(string configurationString) : base(configurationString)
		{
			AddMappingSchema(_convertorSchema);
		}

		public ITable<SampleClass> SampleClass => GetTable<SampleClass>();
	}

	[Table]
	public class SampleClass
	{
		[Column] public int Id    { get; set; }

		[Column(DataType = DataType.VarChar, Length = 4000), JSonContent]
		public DataClass Data { get; set; }
	}

	public class DataClass
	{
		public string Property1 { get; set; }
	}

	public static class Json
	{
		class JsonValueBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
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
						current = ((MemberExpression) current).Expression;
					}
					else
						break;
				}

				pathList.Reverse();

				var entity = pathList[0];
				var field  = pathList[1];

				var fieldSql = builder.ConvertExpressionToSql(field);
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
		public static string Value(object path)
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture]
	public class JsonConvertTests : TestBase
	{

		[Test]
		public void SampleSelectTest([IncludeDataSources(false, TestProvName.SqlAzure)] string context)
		{
			using (var db = new MyDataConnection(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				db.Insert(new SampleClass { Id = 1, Data = new DataClass { Property1 = "Pr1" } });

				var objects = table.Where(t => Json.Value(t.Data.Property1) == "Pr1")
					.ToArray();

				Assert.That(!db.LastQuery.Contains("IS NULL"));

				Assert.AreEqual(1, objects.Length);
				Assert.AreEqual("Pr1", objects[0].Data.Property1);
			}
		}
	}
}
