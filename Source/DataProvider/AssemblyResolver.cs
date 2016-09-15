using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider
{
	class AssemblyResolver
	{
		public AssemblyResolver([NotNull] string path, [NotNull] string resolveName)
		{
			if (path        == null) throw new ArgumentNullException("path");
			if (resolveName == null) throw new ArgumentNullException("resolveName");

			_path        = path;
			_resolveName = resolveName;

			if (_path.StartsWith("file:///"))
				_path = _path.Substring("file:///".Length);

			SetResolver();
		}

		public AssemblyResolver([NotNull] Assembly assembly, [NotNull] string resolveName)
		{
			if (assembly    == null) throw new ArgumentNullException("assembly");
			if (resolveName == null) throw new ArgumentNullException("resolveName");

			_assembly    = assembly;
			_resolveName = resolveName;

			SetResolver();
		}

#if !NETSTANDARD
		void SetResolver()
		{
			ResolveEventHandler resolver = Resolver;

#if FW4
			var l = Expression.Lambda<Action>(Expression.Call(
				Expression.Constant(AppDomain.CurrentDomain),
				typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod(),
				Expression.Constant(resolver)));

			l.Compile()();
#else
			AppDomain.CurrentDomain.AssemblyResolve += resolver;
#endif
		}

		readonly string   _path;
		readonly string   _resolveName;
		         Assembly _assembly;

		public Assembly Resolver(object sender, ResolveEventArgs args)
		{
			if (args.Name == _resolveName)
				return _assembly ?? (_assembly = Assembly.LoadFile(File.Exists(_path) ? _path : Path.Combine(_path, args.Name, ".dll")));
			return null;
		}
#else
		public class MyAssemblyLoadContext : AssemblyLoadContext
		{
			protected override Assembly Load(AssemblyName assemblyName)
			{
				return base.LoadFromAssemblyPath("/home/steveharter/netcore/temp/" + assemblyName + ".dll");
			}
		}
		void SetResolver()
		{
		}
#endif

	}

}
