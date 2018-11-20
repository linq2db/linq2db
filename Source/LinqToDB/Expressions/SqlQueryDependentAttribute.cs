using System;
using System.Collections;

namespace LinqToDB.Expressions
{
	/// <summary>
	/// Used for controlling query caching of custom SQL Functions.
	/// Parameter with this attribute will be evaluated on client side before generating SQL.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class SqlQueryDependentAttribute : Attribute
	{
		/// <summary>
		/// Compares two objects during expression tree comparison. Handles sequences also.
		/// Has to be overriden if specific comparison required
		/// </summary>
		/// <param name="obj1"></param>
		/// <param name="obj2"></param>
		/// <returns>Result of comparison</returns>
		public virtual bool ObjectsEqual(object obj1, object obj2)
		{
			if (ReferenceEquals(obj1, obj2))
				return true;

			if (obj1 is IEnumerable list1 && obj2 is IEnumerable list2)
			{
				var enum1 = list1.GetEnumerator();
				var enum2 = list2.GetEnumerator();
				using (enum1 as IDisposable)
				using (enum2 as IDisposable)
				{
					while (enum1.MoveNext())
					{
						if (!enum2.MoveNext() || !object.Equals(enum1.Current, enum2.Current))
							return false;
					}

					if (enum2.MoveNext())
						return false;
				}

				return true;
			}

			return obj1.Equals(obj2);
		}
	}
}
