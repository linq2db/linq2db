#nullable disable
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.Common;

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

		void SetResolver()
		{
			ResolveEventHandler resolver = Resolver;

			// use this to avoid
			// System.MethodAccessException : Attempt by security transparent method 'LinqToDB.DataProvider.AssemblyResolver.SetResolver()' to access security critical method 'System.AppDomain.add_AssemblyResolve(System.ResolveEventHandler)'
			var l = Expression.Lambda<Action>(Expression.Call(
				Expression.Constant(AppDomain.CurrentDomain),
				typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod(),
				Expression.Constant(resolver)));

			l.Compile()();
		}

		public Assembly Resolver(object sender, ResolveEventArgs args)
		{
			if (args.Name == _resolveName)
				return _assembly ?? (_assembly = Assembly.LoadFile(File.Exists(_path) ? _path : Path.Combine(_path, args.Name, ".dll")));
			return null;
		}
	}
}
