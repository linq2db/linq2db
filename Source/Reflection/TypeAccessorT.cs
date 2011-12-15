using System;

namespace LinqToDB.Reflection
{
	public static class TypeAccessor<T>
	{
		#region CreateInstance

		[System.Diagnostics.DebuggerStepThrough]
		public static T CreateInstance()
		{
			return (T)_instance.CreateInstance();
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static T CreateInstanceEx()
		{
			return (T)_instance.CreateInstanceEx();
		}

		#endregion

		#region Copy & AreEqual

		public static T Copy(T source, T dest)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (dest   == null) throw new ArgumentNullException("dest");

			return (T)TypeAccessor.CopyInternal(source, dest, _instance);
		}

		public static T Copy(T source)
		{
			if (source == null) return source;

			return (T)TypeAccessor.CopyInternal(source, CreateInstanceEx(), _instance);
		}

		public static bool AreEqual(T obj1, T obj2)
		{
			if (ReferenceEquals(obj1, obj2))
				return true;

			if (obj1 == null || obj2 == null)
				return false;

			foreach (MemberAccessor ma in _instance)
				if ((!Equals(ma.GetValue(obj1), ma.GetValue(obj2))))
					return false;

			return true;
		}

		#endregion

		public static IObjectFactory ObjectFactory
		{
			get { return _instance.ObjectFactory;  }
			set { _instance.ObjectFactory = value; }
		}

		public static Type Type         { get { return _instance.Type; } }
		public static Type OriginalType { get { return _instance.OriginalType; } }

		private static readonly TypeAccessor _instance;
		public  static          TypeAccessor  Instance
		{
			[System.Diagnostics.DebuggerStepThrough]
			get { return _instance; }
		}

		static TypeAccessor()
		{
			// Explicitly typed type constructor to prevent 'BeforeFieldInit' jit optimization.
			// See http://blogs.msdn.com/davidnotario/archive/2005/02/08/369593.aspx for details.
			//
			// For us, this means that
			//
			// TypeFactory.SetGlobalAssembly();
			// SomeObject o = TypeAccessor.CreateInstance<SomeObject>();
			//
			// May be executed in reverse order. Usually, there is no problem,
			// but sometimes the order is important.
			// See UnitTests\CS\TypeBuilder\InternalTypesTest.cs for an example.
			//
			_instance = TypeAccessor.GetAccessor(typeof(T));
		}
	}
}

