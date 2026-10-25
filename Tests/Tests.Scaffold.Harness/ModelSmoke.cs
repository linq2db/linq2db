using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

using NUnit.Framework;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Executes compiled model against database: creates tables, inserts and reads back sample rows, invokes generated
	/// Find methods and loads associations. The same path for C# and F# models.
	/// </summary>
	public static class ModelSmoke
	{
		public sealed record Report(int Tables, int Rows, IReadOnlyList<string> FindMethods, int Associations);

		/// <summary>
		/// Number of sample rows per table. Row 0 has values in all columns, row 1 has <see langword="null"/> in nullable columns.
		/// </summary>
		public const int RowCount = 2;

		public static Report Run(ScaffoldedModel model, DataOptions options)
		{
			using var db = model.CreateContext(options);

			var tables = model.GetTables(db);

			if (tables.Count == 0)
				Assert.Fail($"{model}: context has no table properties");

			var rows = new Dictionary<Type, object[]>();

			foreach (var table in tables)
				Call(nameof(CreateTable), table.EntityType, db);

			foreach (var table in tables)
			{
				var entities = Enumerable.Range(0, RowCount).Select(i => CreateRow(db, table.EntityType, i)).ToArray();

				foreach (var entity in entities)
					Call(nameof(Insert), table.EntityType, db, entity);

				rows[table.EntityType] = entities;
			}

			var rowCount = 0;

			foreach (var table in tables)
			{
				var actual   = ((IEnumerable)Call(nameof(ToList), table.EntityType, table.GetTable())!).Cast<object>().ToList();
				var expected = rows[table.EntityType];

				var columns = Comparable(db, table.EntityType);

				Assert.That(
					actual.Select(r => RowKey(columns, r)).OrderBy(static k => k, StringComparer.Ordinal),
					Is.EqualTo(expected.Select(r => RowKey(columns, r)).OrderBy(static k => k, StringComparer.Ordinal)),
					$"{model}: {table.Property.Name} rows");

				rowCount += actual.Count;
			}

			var finds = new List<string>();
			foreach (var method in FindMethods(model))
			{
				var entityType = method.GetParameters()[0].ParameterType.IsGenericType
					? method.GetParameters()[0].ParameterType.GetGenericArguments()[0]
					: UnwrapResultType(method.ReturnType);

				var table  = tables.Single(t => t.EntityType == entityType);
				var source = rows[entityType][0];
				var found  = InvokeFind(method, db, table.GetTable(), source);

				Assert.That(found, Is.Not.Null, $"{model}: {method.DeclaringType!.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(static p => p.ParameterType.Name))}) didn't find row");
				Assert.That(RowKey(Comparable(db, entityType), found!), Is.EqualTo(RowKey(Comparable(db, entityType), source)));

				finds.Add($"{entityType.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(static p => p.ParameterType.Name))})");
			}

			var associations = 0;
			foreach (var table in tables)
			{
				foreach (var association in db.MappingSchema.GetEntityDescriptor(table.EntityType).Associations)
				{
					var loaded = ((IEnumerable)Call(nameof(LoadWith), table.EntityType, table.GetTable(), association.MemberInfo)!).Cast<object>().ToList();

					var populated = loaded.Count(row => GetValue(association.MemberInfo, row) switch
					{
						null                 => false,
						string               => true,
						IEnumerable children => children.Cast<object>().Any(),
						_                    => true,
					});

					Assert.That(populated, Is.GreaterThan(0), $"{model}: association {table.EntityType.Name}.{association.MemberInfo.Name} not loaded for any row");

					associations++;
				}
			}

			return new Report(tables.Count, rowCount, finds, associations);
		}

		// by-primary-key methods on context are named Find<Entity>[Async|Query]
		static readonly System.Text.RegularExpressions.Regex _findName = new("^Find[A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

		/// <summary>
		/// Generated Find/FindAsync/FindQuery extension methods.
		/// </summary>
		public static IReadOnlyList<MethodInfo> FindMethods(ScaffoldedModel model)
			=> model.Types
				.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
				.Where(static m => _findName.IsMatch(m.Name) && m.GetParameters().Length > 1)
				.ToList();

		/// <summary>
		/// Invokes Find method for entity <paramref name="source"/>: primary key parameters are taken from entity by name,
		/// entity parameter gets entity. Returns found entity or <see langword="null"/>.
		/// </summary>
		public static object? InvokeFind(MethodInfo method, IDataContext db, object table, object source)
		{
			var parameters = method.GetParameters();
			var args       = new object?[parameters.Length];

			args[0] = parameters[0].ParameterType.IsInstanceOfType(db) ? db : table;

			for (var i = 1; i < parameters.Length; i++)
			{
				var parameter = parameters[i];

				if (parameter.ParameterType == typeof(CancellationToken))
					args[i] = CancellationToken.None;
				else if (parameter.ParameterType.IsInstanceOfType(source))
					args[i] = source;
				else
				{
					var member = source.GetType().GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
						?? throw new InvalidOperationException($"{method.Name}: no entity member for parameter '{parameter.Name}'");

					args[i] = member.GetValue(source);
				}
			}

			var result = method.Invoke(null, args);

			if (result is Task task)
			{
				task.GetAwaiter().GetResult();
				result = task.GetType().GetProperty(nameof(Task<>.Result))!.GetValue(task);
			}

			if (result is IQueryable query)
				result = Call(nameof(FirstOrDefault), query.ElementType, query);

			return result;
		}

		static Type UnwrapResultType(Type type)
		{
			if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(IQueryable<>)))
				return type.GetGenericArguments()[0];

			return type;
		}

		/// <summary>
		/// Creates sample entity: generated values for mapped columns, <see langword="null"/> for nullable columns in rows after first.
		/// </summary>
		public static object CreateRow(IDataContext db, Type entityType, int row)
		{
			var columns = db.MappingSchema.GetEntityDescriptor(entityType).Columns;

			if (FSharpType.IsRecord(entityType, null))
			{
				var template = entityType
					.GetProperties(BindingFlags.Public | BindingFlags.Static)
					.FirstOrDefault(p => p.PropertyType == entityType && p.Name.StartsWith("Empty", StringComparison.Ordinal))
					?.GetValue(null);

				var fields = FSharpType.GetRecordFields(entityType, null);
				var values = fields
					.Select(f =>
					{
						var column = columns.FirstOrDefault(c => c.MemberName == f.Name);

						return column != null
							? SampleValue(f.PropertyType, row, column.CanBeNull && row > 0)
							: template != null ? f.GetValue(template) : null;
					})
					.ToArray();

				return FSharpValue.MakeRecord(entityType, values, null);
			}

			var entity = Activator.CreateInstance(entityType)!;

			foreach (var column in columns)
				column.MemberAccessor.SetValue(entity, SampleValue(column.MemberType, row, column.CanBeNull && row > 0));

			return entity;
		}

		/// <summary>
		/// Deterministic value of <paramref name="type"/> for row index. Equal row indexes give equal values for equal types,
		/// so foreign keys of row 0 reference primary keys of row 0.
		/// </summary>
		public static object? SampleValue(Type type, int row, bool asNull)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpOption<>))
			{
				// None is represented by null
				if (asNull)
					return null;

				var inner = type.GetGenericArguments()[0];
				return type.GetMethod(nameof(FSharpOption<>.Some))!.Invoke(null, [SampleValue(inner, row, false)]);
			}

			var underlying = Nullable.GetUnderlyingType(type);

			if (asNull && (underlying != null || !type.IsValueType))
				return null;

			type = underlying ?? type;

			return Type.GetTypeCode(type) switch
			{
				TypeCode.Int32    => row + 1,
				TypeCode.Int64    => (long)row + 1,
				TypeCode.Int16    => (short)(row + 1),
				TypeCode.Byte     => (byte)(row + 1),
				TypeCode.Decimal  => row + 1.25m,
				TypeCode.Double   => row + 0.5,
				TypeCode.Single   => row + 0.5f,
				TypeCode.Boolean  => row % 2 == 0,
				TypeCode.String   => "v" + row.ToString(CultureInfo.InvariantCulture),
				TypeCode.DateTime => new DateTime(2020, 1, 1).AddDays(row),
				TypeCode.Char     => (char)('a' + row),
				_ when type == typeof(Guid)   => new Guid(row + 1, 0, 0, new byte[8]),
				_ when type == typeof(byte[]) => new byte[] { 1, 2, (byte)row },
				_ => throw new NotSupportedException($"No sample value for type {type}"),
			};
		}

		static object? GetValue(MemberInfo member, object obj)
			=> member switch
			{
				PropertyInfo property => property.GetValue(obj),
				FieldInfo    field    => field.GetValue(obj),
				_                     => throw new NotSupportedException($"Member {member} not supported"),
			};

		static IReadOnlyList<ColumnDescriptor> Comparable(IDataContext db, Type entityType)
			=> db.MappingSchema.GetEntityDescriptor(entityType).Columns
				.Where(static c => !c.IsIdentity && !c.SkipOnInsert)
				.OrderBy(static c => c.MemberName, StringComparer.Ordinal)
				.ToList();

		static string RowKey(IReadOnlyList<ColumnDescriptor> columns, object row)
			=> string.Join("|", columns.Select(c => Normalize(c.MemberAccessor.GetValue(row))));

		static string Normalize(object? value)
		{
			if (value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(FSharpOption<>))
				value = value.GetType().GetProperty(nameof(FSharpOption<>.Value))!.GetValue(value);

			return value switch
			{
				null           => "<null>",
				byte[] bytes   => Convert.ToBase64String(bytes),
				DateTime dt    => dt.Ticks.ToString(CultureInfo.InvariantCulture),
				IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
				_              => value.ToString() ?? "",
			};
		}

		static object? Call(string method, Type entityType, params object[] args)
		{
			try
			{
				return typeof(ModelSmoke)
					.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!
					.MakeGenericMethod(entityType)
					.Invoke(null, args);
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		static void CreateTable<T>(IDataContext db)
			where T : notnull
			=> db.CreateTable<T>();

		static void Insert<T>(IDataContext db, T entity)
			where T : notnull
			=> db.Insert(entity);

		static List<T> ToList<T>(ITable<T> table)
			where T : notnull
			=> table.ToList();

		static T? FirstOrDefault<T>(IQueryable<T> query)
			=> query.FirstOrDefault();

		static List<T> LoadWith<T>(ITable<T> table, MemberInfo association)
			where T : class
		{
			var parameter = Expression.Parameter(typeof(T), "e");
			var selector  = Expression.Lambda<Func<T, object?>>(Expression.Convert(Expression.MakeMemberAccess(parameter, association), typeof(object)), parameter);

			return table.LoadWith(selector).ToList();
		}
	}
}
