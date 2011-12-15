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

		public static IObjectFactory ObjectFactory
		{
			get { return _instance.ObjectFactory;  }
			set { _instance.ObjectFactory = value; }
		}

		public static Type Type { get { return _instance.Type; } }

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

