using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.FSharp;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Compiled data model. Reflection access to generated types and members.
	/// </summary>
	public sealed class ScaffoldedModel(GeneratedSource source, Assembly assembly)
	{
		public GeneratedSource Source   { get; } = source;
		public Assembly        Assembly { get; } = assembly;

		public ScaffoldLanguage Language => Source.Language;

		public IReadOnlyList<Type> Types { get; } = assembly.GetTypes();

		public Type ContextType => GetType(Source.ContextClassName);

		/// <summary>
		/// Finds generated type by name (nested types included).
		/// </summary>
		public Type GetType(string name)
			=> FindType(name) ?? throw new InvalidOperationException($"Type '{name}' not found. Types: {string.Join(", ", Types.Select(static t => t.FullName))}");

		public Type? FindType(string name)
		{
			var types = Types.Where(t => string.Equals(t.Name, name, StringComparison.Ordinal)).ToList();

			return types.Count switch
			{
				0 => null,
				1 => types[0],
				_ => throw new InvalidOperationException($"Multiple types named '{name}': {string.Join(", ", types.Select(static t => t.FullName))}"),
			};
		}

		/// <summary>
		/// Adds language-specific configuration (F# support for F# models).
		/// </summary>
		public DataOptions PrepareOptions(DataOptions options)
			=> Language == ScaffoldLanguage.FSharp ? options.UseFSharp() : options;

		/// <summary>
		/// Creates generated context using typed (<c>DataOptions&lt;TContext&gt;</c>) or untyped options constructor.
		/// </summary>
		public IDataContext CreateContext(DataOptions options)
		{
			options = PrepareOptions(options);

			var contextType = ContextType;
			var typedOptions = typeof(DataOptions<>).MakeGenericType(contextType);

			var typedCtor = contextType.GetConstructor([typedOptions]);
			if (typedCtor != null)
				return (IDataContext)typedCtor.Invoke([Activator.CreateInstance(typedOptions, options)]);

			var ctor = contextType.GetConstructor([typeof(DataOptions)])
				?? throw new InvalidOperationException($"Context '{contextType}' has no options constructor");

			return (IDataContext)ctor.Invoke([options]);
		}

		/// <summary>
		/// Static methods with specified name from all generated types (extension methods, function mappings).
		/// </summary>
		public IReadOnlyList<MethodInfo> GetStaticMethods(string name)
			=> Types
				.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
				.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
				.ToList();

		/// <summary>
		/// Methods with specified name, declared on context or extension classes, accepting context as first parameter.
		/// </summary>
		public MethodInfo GetMethod(string name)
		{
			var methods = Types
				.SelectMany(static t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
				.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
				.ToList();

			return methods.Count == 1
				? methods[0]
				: throw new InvalidOperationException($"Expected single method '{name}', found {methods.Count}");
		}

		/// <summary>
		/// Table access properties of context, including properties of nested schema contexts.
		/// </summary>
		public IReadOnlyList<TableAccessor> GetTables(object context)
		{
			var result = new List<TableAccessor>();
			Collect(context, result, 0);
			return result;

			static void Collect(object owner, List<TableAccessor> result, int depth)
			{
				foreach (var property in owner.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if (property.GetIndexParameters().Length > 0)
						continue;

					var type = property.PropertyType;

					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ITable<>))
						result.Add(new TableAccessor(property, type.GetGenericArguments()[0], () => property.GetValue(owner)!));
					else if (depth == 0 && type.Assembly == owner.GetType().Assembly && !type.IsValueType && property.GetValue(owner) is { } schemaContext)
						Collect(schemaContext, result, depth + 1);
				}
			}
		}

		public override string ToString() => $"{Source.Name} ({Language})";
	}

	public sealed record TableAccessor(PropertyInfo Property, Type EntityType, Func<object> GetTable);
}
