using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

using System.Runtime.InteropServices;
using LinqToDB.Reflection;
using LinqToDB.Reflection.Emit;
using LinqToDB.TypeBuilder.Builders;
using LinqToDB.Properties;

namespace LinqToDB.TypeBuilder
{
	public static class TypeFactory
	{
		private static AssemblyBuilderHelper GetAssemblyBuilder(Type type, string suffix)
		{
#if SILVERLIGHT
			var assemblyDir = ".";
#else
			var assemblyDir = AppDomain.CurrentDomain.BaseDirectory;

			// Dynamic modules are locationless, so ignore them.
			// _ModuleBuilder is the base type for both
			// ModuleBuilder and InternalModuleBuilder classes.
			//
			if (!(type.Module is _ModuleBuilder) && type.Module.FullyQualifiedName != null && type.Module.FullyQualifiedName.IndexOf('<') < 0)
				assemblyDir = Path.GetDirectoryName(type.Module.FullyQualifiedName);
#endif

			var fullName = type.FullName;

			if (type.IsGenericType)
				fullName = TypeHelper.GetTypeFullName(type);

			fullName = fullName.Replace('<', '_').Replace('>', '_');

			return new AssemblyBuilderHelper(Path.Combine(assemblyDir, fullName + "." + suffix + ".dll"));
		}


		#region GetType

		static readonly Dictionary<Type,IDictionary<object,Type>> _builtTypes = new Dictionary<Type,IDictionary<object,Type>>(10);
		static readonly Dictionary<Assembly,Assembly>             _assemblies = new Dictionary<Assembly, Assembly>(10);

		public static Type GetType(object hashKey, Type sourceType, ITypeBuilder typeBuilder)
		{
			if (hashKey     == null) throw new ArgumentNullException("hashKey");
			if (sourceType  == null) throw new ArgumentNullException("sourceType");
			if (typeBuilder == null) throw new ArgumentNullException("typeBuilder");

			try
			{
				lock (_builtTypes)
				{
					Type type;
					IDictionary<object,Type> builderTable;

					if (_builtTypes.TryGetValue(typeBuilder.GetType(), out builderTable))
					{
						if (builderTable.TryGetValue(hashKey, out type))
							return type;
					}
					else
						_builtTypes.Add(typeBuilder.GetType(), builderTable = new Dictionary<object,Type>());

					var assemblyBuilder = GetAssemblyBuilder(sourceType, typeBuilder.AssemblyNameSuffix);

					type = typeBuilder.Build(assemblyBuilder);

					if (type != null)
						builderTable.Add(hashKey, type);

					return type;
				}
			}
			catch (TypeBuilderException)
			{
				throw;
			}
			catch (Exception ex)
			{
				// Convert an Exception to TypeBuilderException.
				//
				throw new TypeBuilderException(string.Format(Resources.TypeFactory_BuildFailed, sourceType.FullName), ex);
			}
		}

		static class InstanceCreator<T>
		{
			public static readonly Func<T> CreateInstance = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
		}

		public static T CreateInstance<T>() where T: class
		{
			return InstanceCreator<T>.CreateInstance();
		}

		#endregion

		#region Private Helpers

		static Assembly LoadExtensionAssembly(Assembly originalAssembly)
		{
#if !SILVERLIGHT

			if (originalAssembly is _AssemblyBuilder)
			{
				// This is a generated assembly. Even if it has a valid Location,
				// there is definitelly no extension assembly at this path.
				//
				return null;
			}

			try
			{
				var originalAssemblyLocation  = new Uri(originalAssembly.EscapedCodeBase).LocalPath;
				var extensionAssemblyLocation = Path.ChangeExtension(
					originalAssemblyLocation, "LinqToDBExtension.dll");

				if (File.GetLastWriteTime(originalAssemblyLocation) <= File.GetLastWriteTime(extensionAssemblyLocation))
					return Assembly.LoadFrom(extensionAssemblyLocation);

				Debug.WriteLineIf(File.Exists(extensionAssemblyLocation),
					string.Format("Extension assembly '{0}' is out of date. Please rebuild.",
						extensionAssemblyLocation), typeof(TypeAccessor).FullName);

				// Some good man may load this assembly already. Like IIS does it.
				//
				var extensionAssemblyName = originalAssembly.GetName(true);
				extensionAssemblyName.Name += ".LinqToDBExtension";

				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					// Note that assembly version and strong name are compared too.
					//
					if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(false), extensionAssemblyName))
						return assembly;
				}
			}
			catch (Exception ex)
			{
				// Extension exist, but can't be loaded for some reason.
				// Switch back to code generation
				//
				Debug.WriteLine(ex, typeof(TypeAccessor).FullName);
			}

#endif

			return null;
		}

		[Conditional("DEBUG")]
		private static void WriteDebug(string format, params object[] parameters)
		{
			Debug.WriteLine(string.Format(format, parameters));
		}

		#endregion
	}
}
