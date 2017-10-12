using System;
using System.Configuration;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Collection of configuration section elements.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	public abstract class ElementCollectionBase<T>: ConfigurationElementCollection
		where T : ConfigurationElement, new()
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new T();
		}

		protected abstract object GetElementKey(T element);

		protected sealed override object GetElementKey(ConfigurationElement element)
		{
			return GetElementKey((T)element);
		}

		/// <summary>
		/// Gets element from collection by its name.
		/// </summary>
		/// <param name="name">Element name.</param>
		/// <returns>Element or null, if element with such name is not found.</returns>
		public new T this[string name]
		{
			get { return (T)BaseGet(name); }
		}

		/// <summary>
		/// Gets element from collection by its index.
		/// </summary>
		/// <param name="index">Element index.</param>
		/// <returns>Element at specified index.</returns>
		public  T this[int index]
		{
			get { return (T)BaseGet(index); }
		}
	}
}
