using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using Newtonsoft.Json;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3340Tests : TestBase
	{
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3340")]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<SampleTable>();

			tb.AsUpdatable()
				.Set(t => t.Object!, t => SampleObjectExtensions.Set(t.Object!, o => o.Sample, "test"))
				.Update();

			Assert.That(db.LastQuery, Does.Contain("@"));
		}

		public class JsonModifyExtensionCallBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				new JsonModifySample<SampleObject>().BuildSet(builder);
			}
		}

		public class SampleTable
		{
			[Column(DataType = DataType.NVarChar)]
			public SampleObject? Object { get; set; }
		}

		public class SampleObject
		{
			public string? Sample { get; set; }
		}

		public static class SampleObjectExtensions
		{
			[Sql.Extension("Set", ServerSideOnly = true, BuilderType = typeof(JsonModifyExtensionCallBuilder))]
			public static SampleObject Set<TValue>(SampleObject sampleObject, Expression<Func<SampleObject, TValue>> propertyExpression, TValue propertyValue)
			{
				return sampleObject;
			}
		}

		internal static class ExpressionUtils
		{
			public static bool TryExtractConstant(Expression expression, out object? value)
			{
				var memberStack = new Stack<MemberInfo>();
				Expression currentExpression = expression;

				while (currentExpression.NodeType == ExpressionType.MemberAccess ||
					   currentExpression.NodeType == ExpressionType.Convert ||
					   currentExpression.NodeType == ExpressionType.ConvertChecked)
				{
					if (currentExpression.NodeType == ExpressionType.Convert ||
						currentExpression.NodeType == ExpressionType.ConvertChecked)
					{
						currentExpression = ((UnaryExpression)currentExpression).Operand;
					}
					else
					{
						var memberExpression = (MemberExpression) currentExpression;
						if (!(memberExpression.Member is PropertyInfo || memberExpression.Member is FieldInfo))
						{
							break;
						}

						memberStack.Push(memberExpression.Member);

						currentExpression = memberExpression.Expression!;
					}
				}

				if (currentExpression is ConstantExpression constant)
				{
					var currentValue = constant.Value;
					while (memberStack.Count > 0)
					{
						var member = memberStack.Pop();

						currentValue = member is PropertyInfo property
							? property.GetValue(currentValue)
							: ((FieldInfo)member).GetValue(currentValue);
					}

					value = currentValue;
					return true;
				}

				value = null;
				return false;
			}

			public static bool TryEvaluateExpression(Expression expression, out object? value)
			{
				if (TryExtractConstant(expression, out value))
				{
					return true;
				}

				if (expression.NodeType == ExpressionType.Convert ||
					expression.NodeType == ExpressionType.ConvertChecked)
				{
					expression = ((UnaryExpression)expression).Operand;
				}

				if (expression.NodeType == ExpressionType.MemberInit ||
					expression.NodeType == ExpressionType.New ||
					expression.NodeType == ExpressionType.NewArrayInit)
				{
					value = Expression.Lambda(expression).Compile().DynamicInvoke();
					return true;
				}

				value = null;
				return false;
			}
		}

		public class JsonModifySample<T>
		{
			private static readonly JsonSerializerSettings _defaultSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore
			};

			private const string DbObjectExpression = "JSON_MODIFY({source}, {path}, {value})";

			public void BuildSet(Sql.ISqExtensionBuilder builder)
			{
				builder.Expression = DbObjectExpression;

				builder.AddParameter("source", builder.GetExpression(0)!);

				var (lastMember, path) = GetPropertyAccessPath(builder);
				builder.AddParameter("path", path);

				builder.AddParameter("value", GetPropertyValueExpression(builder, lastMember));
			}

			private static (MemberInfo lastMember, string path) GetPropertyAccessPath(Sql.ISqExtensionBuilder builder)
			{
				var currentExression = ((LambdaExpression) ((UnaryExpression) builder.Arguments[1]).Operand).Body;
				var memberStack = new Stack<MemberInfo>();

				while (currentExression is MemberExpression memberExpression)
				{
					memberStack.Push(memberExpression.Member);

					currentExression = memberExpression.Expression;
				}

				if (!(currentExression is ParameterExpression) || memberStack.Count == 0)
				{
					throw new InvalidOperationException("Invalid property expression.");
				}

				var pathBuilder = new StringBuilder();
				if (memberStack.Count > 1)
				{
					throw new NotSupportedException("Modifying of inner objects is not supported.");
				}

				pathBuilder.Append('$');

				MemberInfo? lastMember = null;
				foreach (var member in memberStack)
				{
					pathBuilder.Append('.');
					pathBuilder.Append(member.Name);

					lastMember = member;
				}

				return (lastMember: lastMember!, path: pathBuilder.ToString()!);
			}

			private static ISqlExpression GetPropertyValueExpression(Sql.ISqExtensionBuilder builder, MemberInfo member)
			{
				ISqlExpression ConstantToSqlExpression(object? constantValue, Type valueType)
				{
					// force using of sql parameters
					var expression = Expression.Convert(
						Expression.Property(
							Expression.Constant(new ValueContainer(constantValue)),
							nameof(ValueContainer.Value)),
						valueType);

					return builder.ConvertExpressionToSql(expression)!;
				}

				ISqlExpression ToSqlExpression<TValue>(TValue constantValue)
				{
					return ConstantToSqlExpression(constantValue, typeof(TValue));
				}

				var valueExpression = builder.Arguments[2];

				if (!ExpressionUtils.TryEvaluateExpression(valueExpression, out var value))
				{
					return builder.ConvertExpressionToSql(valueExpression)!;
				}

				var valueType = Nullable.GetUnderlyingType(valueExpression.Type) ?? valueExpression.Type;
				if (value == null)
				{
					return new SqlValue(valueType, null);
				}

				if (valueType.IsPrimitive ||
					valueType == typeof(string))
				{
					return new SqlParameter(new DbDataType(valueExpression.Type), null, value);
					//return ConstantToSqlExpression(value, valueExpression.Type);
				}

				var serializer = JsonSerializer.CreateDefault(_defaultSettings);
				var converterAttribute = member?.GetAttribute<JsonConverterAttribute>(true);
				if (converterAttribute != null)
				{
					serializer.Converters.Add((JsonConverter)Activator.CreateInstance(converterAttribute.ConverterType, converterAttribute.ConverterParameters)!);
				}

				string serializedValue;
				using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
				using (var jsonTextWriter = new JsonTextWriter(stringWriter))
				{
					serializer.Serialize(jsonTextWriter, value);

					serializedValue = stringWriter.ToString();
				}

				if (valueType.IsEnum)
				{
					if (serializedValue.StartsWith("\""))
					{
						return ToSqlExpression(serializedValue.Trim('"'));
					}

					return ToSqlExpression(Convert.ToInt32(value));
				}

				if (valueType == typeof(Guid) ||
					valueType == typeof(DateTime))
				{
					return ToSqlExpression(serializedValue.Trim('"'));
				}

				return new SqlExpression(
					"JSON_QUERY({0})",
					ToSqlExpression(serializedValue)
				);
			}

			#region CLASSES

			private class ValueContainer
			{
				public ValueContainer(object? value)
				{
					Value = value;
				}

				public object? Value { get; }
			}

			#endregion CLASSES
		}
	}
}

