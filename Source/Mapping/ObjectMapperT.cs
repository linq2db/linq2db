using System;

namespace LinqToDB.Mapping
{
	public static class ObjectMapper<T>
	{
		public static T CreateInstance()
		{
			return (T)_instance.CreateInstance();
		}

		public static int Count
		{
			get { return _instance.Count; }
		}

		public static string GetName(int index)
		{
			return _instance.GetName(index);
		}

		public static object GetValue(T o, int index)
		{
			return _instance.GetValue(o, index);
		}

		public static object GetValue(T o, string name)
		{
			return _instance.GetValue(o, name);
		}

		public static int GetOrdinal(string name)
		{
			return _instance.GetOrdinal(name);
		}

		public static void SetValue(T o, int index, object value)
		{
			_instance.SetValue(o, index, value);
		}

		public static void SetValue(object o, string name, object value)
		{
			_instance.SetValue(o, name, value);
		}

		private static readonly ObjectMapper _instance = Map.DefaultSchema.GetObjectMapper(typeof(T));
		public  static          ObjectMapper  Instance
		{
			get { return _instance; }
		}
	}
}
