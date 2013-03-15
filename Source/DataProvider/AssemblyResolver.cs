using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

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

		readonly string _path;
		readonly string _resolveName;

		public Assembly Resolver(object sender, ResolveEventArgs args)
		{
			if (args.Name == _resolveName)
				return Assembly.LoadFile(File.Exists(_path) ? _path : Path.Combine(_path, args.Name, ".dll"));
			return null;
		}
	}
}
