using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.Common;

#if NETSTANDARD1_6
using System.Runtime.Loader;
using System.Linq;
#endif

namespace LinqToDB.DataProvider
{
	class AssemblyResolver
	{
		readonly string   _path;
		readonly string   _resolveName;
		         Assembly _assembly;

		public AssemblyResolver([NotNull] string path, [NotNull] string resolveName)
		{
			_path        = path ?? throw new ArgumentNullException("path");
			_resolveName = resolveName ?? throw new ArgumentNullException("resolveName");

			if (_path.StartsWith("file://"))
				_path = _path.GetPathFromUri();

			SetResolver();
		}

		public AssemblyResolver([NotNull] Assembly assembly, [NotNull] string resolveName)
		{
			_assembly    = assembly    ?? throw new ArgumentNullException(nameof(assembly));
			_resolveName = resolveName ?? throw new ArgumentNullException(nameof(resolveName));

			SetResolver();
		}

#if !NETSTANDARD1_6
		void SetResolver()
		{
			ResolveEventHandler resolver = Resolver;

			//#if NET40
			// use this to avoid
			// System.MethodAccessException : Attempt by security transparent method 'LinqToDB.DataProvider.AssemblyResolver.SetResolver()' to access security critical method 'System.AppDomain.add_AssemblyResolve(System.ResolveEventHandler)'
			var l = Expression.Lambda<Action>(Expression.Call(
				Expression.Constant(AppDomain.CurrentDomain),
				typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod(),
				Expression.Constant(resolver)));

			l.Compile()();
//#else
//			AppDomain.CurrentDomain.AssemblyResolve += resolver;
//#endif
		}


		public Assembly Resolver(object sender, ResolveEventArgs args)
		{
			if (args.Name == _resolveName)
				return _assembly ?? (_assembly = Assembly.LoadFile(File.Exists(_path) ? _path : Path.Combine(_path, args.Name, ".dll")));
			return null;
		}
#else
		public class FileAssemblyLoadContext : AssemblyLoadContext
		{
			readonly string _path;

			public FileAssemblyLoadContext(string path)
			{
				_path = path;
			}

			protected override Assembly Load(AssemblyName assemblyName)
			{
				var deps = Microsoft.Extensions.DependencyModel.DependencyContext.Default;
				var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
				if (res.Count > 0)
				{
					return Assembly.Load(new AssemblyName(res.First().Name));
				}
				else
				{
					var fullName = Path.Combine(_path, assemblyName.Name, ".dll");
					if (File.Exists(fullName))
					{
						var asl = new FileAssemblyLoadContext(_path);
						return asl.LoadFromAssemblyPath(fullName);
					}
				}
				return Assembly.Load(assemblyName);
			}
		}

		void SetResolver()
		{
			var fullName = Path.Combine(_path, _resolveName, ".dll");
			if(File.Exists(fullName))
			{
				var f = new FileAssemblyLoadContext(_path);
				f.LoadFromAssemblyPath(fullName);
			}
		}
#endif

	}
}
