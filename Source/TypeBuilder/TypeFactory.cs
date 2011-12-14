using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Permissions;

using LinqToDB.Reflection;
using LinqToDB.Reflection.Emit;
using LinqToDB.TypeBuilder.Builders;
using LinqToDB.Properties;
#if !SILVERLIGHT
using LinqToDB.Configuration;
#endif

namespace LinqToDB.TypeBuilder
{
	public static class TypeFactory
	{
		static TypeFactory()
		{
			SealTypes = true;

#if !SILVERLIGHT

			var section = LinqToDBSection.Instance;

			if (section != null)
			{
				var elm = section.TypeFactory;

				if (elm != null)
				{
					SaveTypes = elm.SaveTypes;
					SealTypes = elm.SealTypes;
					LoadTypes = elm.LoadTypes;

					SetGlobalAssembly(elm.AssemblyPath, elm.Version, elm.KeyFile);
				}
			}

#endif

#if !SILVERLIGHT

			var perm = new SecurityPermission(SecurityPermissionFlag.ControlAppDomain);

#if FW4
			try
			{
				//var permissionSet = new PermissionSet(PermissionState.None);
				//permissionSet.AddPermission(perm);

				//if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
					SubscribeAssemblyResolver();
			}
			catch
			{
			}
#else
			if (SecurityManager.IsGranted(perm))
				SubscribeAssemblyResolver();
#endif

#endif
		}

		static void SubscribeAssemblyResolver()
		{
#if FW4
			// This hack allows skipping FW 4.0 security check for partial trusted assemblies.
			//

			var dm   = new DynamicMethod("SubscribeAssemblyResolverEx", typeof(void), null);
			var emit = new EmitHelper(dm.GetILGenerator());

			emit
				.call     (typeof(AppDomain).GetProperty("CurrentDomain").GetGetMethod())
				.ldnull
				.ldftn    (typeof(TypeFactory).GetMethod("AssemblyResolver"))
				.newobj   (typeof(ResolveEventHandler).GetConstructor(new[] { typeof(object), typeof(IntPtr) }))
				.callvirt (typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod())
				.ret()
				;

			var setter = (Action)dm.CreateDelegate(typeof(Action));

			setter();
#else
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver;
#endif
		}

		#region Create Assembly

		private static string                _globalAssemblyPath;
		private static string                _globalAssemblyKeyFile;
		private static Version               _globalAssemblyVersion;
		private static AssemblyBuilderHelper _globalAssembly;

		private static AssemblyBuilderHelper GlobalAssemblyBuilder
		{
			get
			{
				if (_globalAssembly == null && _globalAssemblyPath != null)
					_globalAssembly = new AssemblyBuilderHelper(_globalAssemblyPath, _globalAssemblyVersion, _globalAssemblyKeyFile);

				return _globalAssembly;
			}
		}

		public static bool SaveTypes { get; set; }
		public static bool SealTypes { get; set; }

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void SetGlobalAssembly(string path)
		{
			SetGlobalAssembly(path, null, null);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void SetGlobalAssembly(string path, Version version, string keyFile)
		{
			if (_globalAssembly != null)
				SaveGlobalAssembly();

			if (!string.IsNullOrEmpty(path))
				_globalAssemblyPath = path;

			_globalAssemblyVersion = version;
			_globalAssemblyKeyFile = keyFile;
		}

		public static void SaveGlobalAssembly()
		{
			if (_globalAssembly != null)
			{
				_globalAssembly.Save();

				WriteDebug("The global assembly saved in '{0}'.", _globalAssembly.Path);

				_globalAssembly        = null;
				_globalAssemblyPath    = null;
				_globalAssemblyVersion = null;
				_globalAssemblyKeyFile = null;
			}
		}

		private static AssemblyBuilderHelper GetAssemblyBuilder(Type type, string suffix)
		{
			var ab = GlobalAssemblyBuilder;

			if (ab == null)
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
					fullName = AbstractClassBuilder.GetTypeFullName(type);

				fullName = fullName.Replace('<', '_').Replace('>', '_');

				ab = new AssemblyBuilderHelper(Path.Combine(assemblyDir, fullName + "." + suffix + ".dll"));
			}

			return ab;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void SaveAssembly(AssemblyBuilderHelper assemblyBuilder, Type type)
		{
			if (!SaveTypes || _globalAssembly != null)
				return;
			try
			{
				assemblyBuilder.Save();

				WriteDebug("The '{0}' type saved in '{1}'.",
							type.FullName,
							assemblyBuilder.Path);
			}
			catch (Exception ex)
			{
				WriteDebug("Can't save the '{0}' assembly for the '{1}' type: {2}.",
							assemblyBuilder.Path,
							type.FullName,
							ex.Message);
			}
		}

		#endregion

		#region GetType

		static readonly Dictionary<Type,IDictionary<object,Type>> _builtTypes = new Dictionary<Type,IDictionary<object,Type>>(10);
		static readonly Dictionary<Assembly,Assembly>             _assemblies = new Dictionary<Assembly, Assembly>(10);

		public static bool LoadTypes { get; set; }

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

					if (LoadTypes)
					{
						var originalAssembly = sourceType.Assembly;

						Assembly extensionAssembly;

						if (!_assemblies.TryGetValue(originalAssembly, out extensionAssembly))
						{
							extensionAssembly = LoadExtensionAssembly(originalAssembly);
							_assemblies.Add(originalAssembly, extensionAssembly);
						}

						if (extensionAssembly != null)
						{
							type = extensionAssembly.GetType(typeBuilder.GetTypeName());

							if (type != null)
							{
								builderTable.Add(hashKey, type);
								return type;
							}
						}
					}

					var assemblyBuilder = GetAssemblyBuilder(sourceType, typeBuilder.AssemblyNameSuffix);

					type = typeBuilder.Build(assemblyBuilder);

					if (type != null)
					{
						builderTable.Add(hashKey, type);
						SaveAssembly(assemblyBuilder, type);
					}

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

		public static Type GetType(Type sourceType)
		{
			return TypeHelper.IsScalar(sourceType) || sourceType.IsSealed ?
				sourceType:
				GetType(sourceType, sourceType, new AbstractClassBuilder(sourceType));
		}

		static class InstanceCreator<T>
		{
			public static readonly Func<T> CreateInstance = Expression.Lambda<Func<T>>(Expression.New(TypeFactory.GetType(typeof(T)))).Compile();
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

		#region Resolve Types

		/// <summary>
		/// Initializes AssemblyResolve hooks for the current <see cref="AppDomain"/>.
		/// </summary>
		public static void Init()
		{
			//
			// The code actually does nothing except an implicit call to the type constructor.
			//
		}

		public static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
		{
			var name      = args.Name;
			var nameParts = name.Split(',');

			if (nameParts.Length > 0 && nameParts[0].ToLower().EndsWith(".dll"))
			{
				nameParts[0] = nameParts[0].Substring(0, nameParts[0].Length - 4);
				name         = string.Join(",", nameParts);
			}

			lock (_builtTypes)
				foreach (var type in _builtTypes.Keys)
					if (type.FullName == name)
						return type.Assembly;

#if !SILVERLIGHT

			var idx = name.IndexOf("." + TypeBuilderConsts.AssemblyNameSuffix);

			if (idx > 0)
			{
				var typeName = name.Substring(0, idx);
				var type     = Type.GetType(typeName);

				if (type == null)
				{
					var ass = ((AppDomain)sender).GetAssemblies();

					// CLR can't find an assembly built on previous AssemblyResolve event.
					//
					for (var i = ass.Length - 1; i >= 0; i--)
					{
						if (string.Compare(ass[i].FullName, name) == 0)
							return ass[i];
					}

					for (var i = ass.Length - 1; i >= 0; i--)
					{
						var a = ass[i];

						if (!(
#if FW4
							a.IsDynamic ||
#endif
							a is _AssemblyBuilder) &&
							(a.CodeBase.IndexOf("Microsoft.NET/Framework") > 0 || a.FullName.StartsWith("System."))) continue;

						type = a.GetType(typeName);

						if (type != null) break;

						foreach (var t in a.GetTypes())
						{
							if (!t.IsAbstract)
								continue;

							if (t.FullName == typeName)
							{
								type = t;
							}
							else
							{
								if (t.FullName.IndexOf('+') > 0)
								{
									var s = typeName;

									while (type == null && (idx = s.LastIndexOf(".")) > 0)
									{
										s = s.Remove(idx, 1).Insert(idx, "+");

										if (t.FullName == s)
											type = t;
									}
								}
							}

							if (type != null) break;
						}

						if (type != null) break;
					}
				}

				if (type != null)
				{
					var newType = GetType(type);

					if (newType.Assembly.FullName == name)
						return newType.Assembly;
				}
			}

#endif

			return null;
		}

		#endregion
	}
}
