using System;
using System.Configuration.Assemblies;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Threading;

namespace LinqToDB.Reflection.Emit
{
	/// <summary>
	/// A wrapper around the <see cref="AssemblyBuilder"/> and <see cref="ModuleBuilder"/> classes.
	/// </summary>
	/// <seealso cref="System.Reflection.Emit.AssemblyBuilder">AssemblyBuilder Class</seealso>
	/// <seealso cref="System.Reflection.Emit.ModuleBuilder">ModuleBuilder Class</seealso>
	public class AssemblyBuilderHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblyBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="path">The path where the assembly will be saved.</param>
		public AssemblyBuilderHelper(string path) : this(path, null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblyBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="path">The path where the assembly will be saved.</param>
		/// <param name="version">The assembly version.</param>
		/// <param name="keyFile">The key pair file to sign the assembly.</param>
		public AssemblyBuilderHelper(string path, Version version, string keyFile)
		{
			if (path == null) throw new ArgumentNullException("path");

			var idx = path.IndexOf(',');

			if (idx > 0)
			{
				path = path.Substring(0, idx);

				if (path.Length >= 200)
				{
					idx = path.IndexOf('`');

					if (idx > 0)
					{
						var idx2 = path.LastIndexOf('.');

						if (idx2 > 0 && idx2 > idx)
							path = path.Substring(0, idx + 1) + path.Substring(idx2 + 1);
					}
				}
			}

			path = path.Replace("+", ".").Replace("<", "_").Replace(">", "_");

			if (path.Length >= 260)
			{
				path = path.Substring(0, 248);

				for (var i = 0; i < int.MaxValue; i++)
				{
					var newPath = string.Format("{0}_{1:0000}.dll", path, i);

					if (!System.IO.File.Exists(newPath))
					{
						path = newPath;
						break;
					}
				}
			}

			var assemblyName = System.IO.Path.GetFileNameWithoutExtension(path);
			var assemblyDir  = System.IO.Path.GetDirectoryName(path);

			Path               = path;
			_assemblyName.Name = assemblyName;

			if (version != null)
				_assemblyName.Version = version;

#if !SILVERLIGHT

			if (!string.IsNullOrEmpty(keyFile))
			{
				_assemblyName.Flags        |= AssemblyNameFlags.PublicKey;
				_assemblyName.KeyPair       = new StrongNameKeyPair(System.IO.File.OpenRead(keyFile));
				_assemblyName.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
			}

#endif

#if DEBUG
			_assemblyName.Flags |= AssemblyNameFlags.EnableJITcompileTracking;
#else
			_assemblyName.Flags |= AssemblyNameFlags.EnableJITcompileOptimizer;
#endif

			_createAssemblyBuilder = _ =>
			{
#if SILVERLIGHT
				_assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
#else
				_assemblyBuilder =
					string.IsNullOrEmpty(assemblyDir)?
					Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave):
					Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave, assemblyDir);
#endif

#if !SILVERLIGHT

				_assemblyBuilder.SetCustomAttribute(
					new CustomAttributeBuilder(
						typeof(AllowPartiallyTrustedCallersAttribute)
							.GetConstructor(Type.EmptyTypes),
						new object[0]));

#endif
			};
		}

		/// <summary>
		/// Gets the path where the assembly will be saved.
		/// </summary>
		public string Path { get; private set; }

		private readonly AssemblyName _assemblyName = new AssemblyName();
		/// <summary>
		/// Gets AssemblyName.
		/// </summary>
		public           AssemblyName  AssemblyName
		{
			get { return _assemblyName; }
		}

		readonly Action<int> _createAssemblyBuilder; 

		AssemblyBuilder _assemblyBuilder;
		/// <summary>
		/// Gets AssemblyBuilder.
		/// </summary>
		public   AssemblyBuilder  AssemblyBuilder
		{
			get
			{
				if (_assemblyBuilder == null)
					_createAssemblyBuilder(0);
				return _assemblyBuilder;
			}
		}

		/// <summary>
		/// Gets the path where the assembly will be saved.
		/// </summary>
		public  string  ModulePath
		{
			get { return System.IO.Path.GetFileName(Path); }
		}

		private ModuleBuilder _moduleBuilder;
		/// <summary>
		/// Gets ModuleBuilder.
		/// </summary>
		public  ModuleBuilder  ModuleBuilder
		{
			get { return _moduleBuilder ?? (_moduleBuilder = AssemblyBuilder.DefineDynamicModule(ModulePath)); }
		}

		/// <summary>
		/// Converts the supplied <see cref="AssemblyBuilderHelper"/> to a <see cref="AssemblyBuilder"/>.
		/// </summary>
		/// <param name="assemblyBuilder">The <see cref="AssemblyBuilderHelper"/>.</param>
		/// <returns>An <see cref="AssemblyBuilder"/>.</returns>
		public static implicit operator AssemblyBuilder(AssemblyBuilderHelper assemblyBuilder)
		{
			if (assemblyBuilder == null) throw new ArgumentNullException("assemblyBuilder");

			return assemblyBuilder.AssemblyBuilder;
		}

		/// <summary>
		/// Converts the supplied <see cref="AssemblyBuilderHelper"/> to a <see cref="ModuleBuilder"/>.
		/// </summary>
		/// <param name="assemblyBuilder">The <see cref="AssemblyBuilderHelper"/>.</param>
		/// <returns>A <see cref="ModuleBuilder"/>.</returns>
		public static implicit operator ModuleBuilder(AssemblyBuilderHelper assemblyBuilder)
		{
			if (assemblyBuilder == null) throw new ArgumentNullException("assemblyBuilder");

			return assemblyBuilder.ModuleBuilder;
		}

		/// <summary>
		/// Saves this dynamic assembly to disk.
		/// </summary>
		public void Save()
		{
#if !SILVERLIGHT

			if (_assemblyBuilder != null)
				_assemblyBuilder.Save(ModulePath);

#endif
		}

		#region DefineType Overrides

		/// <summary>
		/// Constructs a <see cref="TypeBuilderHelper"/> for a type with the specified name.
		/// </summary>
		/// <param name="name">The full path of the type.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string)">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineType(string name)
		{
			return new TypeBuilderHelper(this, ModuleBuilder.DefineType(name));
		}

		/// <summary>
		/// Constructs a <see cref="TypeBuilderHelper"/> for a type with the specified name and base type.
		/// </summary>
		/// <param name="name">The full path of the type.</param>
		/// <param name="parent">The Type that the defined type extends.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string,TypeAttributes,Type)">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineType(string name, Type parent)
		{
			return new TypeBuilderHelper(this, ModuleBuilder.DefineType(name, TypeAttributes.Public, parent));
		}

		/// <summary>
		/// Constructs a <see cref="TypeBuilderHelper"/> for a type with the specified name, its attributes, and base type.
		/// </summary>
		/// <param name="name">The full path of the type.</param>
		/// <param name="attrs">The attribute to be associated with the type.</param>
		/// <param name="parent">The Type that the defined type extends.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string,TypeAttributes,Type)">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineType(string name, TypeAttributes attrs, Type parent)
		{
			return new TypeBuilderHelper(this, ModuleBuilder.DefineType(name, attrs, parent));
		}

		/// <summary>
		/// Constructs a <see cref="TypeBuilderHelper"/> for a type with the specified name, base type,
		/// and the interfaces that the defined type implements.
		/// </summary>
		/// <param name="name">The full path of the type.</param>
		/// <param name="parent">The Type that the defined type extends.</param>
		/// <param name="interfaces">The list of interfaces that the type implements.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string,TypeAttributes,Type,Type[])">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineType(string name, Type parent, params Type[] interfaces)
		{
			return new TypeBuilderHelper(
				this,
				ModuleBuilder.DefineType(name, TypeAttributes.Public, parent, interfaces));
		}

		/// <summary>
		/// Constructs a <see cref="TypeBuilderHelper"/> for a type with the specified name, its attributes, base type,
		/// and the interfaces that the defined type implements.
		/// </summary>
		/// <param name="name">The full path of the type.</param>
		/// <param name="attrs">The attribute to be associated with the type.</param>
		/// <param name="parent">The Type that the defined type extends.</param>
		/// <param name="interfaces">The list of interfaces that the type implements.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string,TypeAttributes,Type,Type[])">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineType(string name, TypeAttributes attrs, Type parent, params Type[] interfaces)
		{
			return new TypeBuilderHelper(
				this,
				ModuleBuilder.DefineType(name, attrs, parent, interfaces));
		}

		#endregion
	}
}
