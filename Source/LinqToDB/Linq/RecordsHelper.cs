using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Linq
{
	using Extensions;

	[Flags]
	internal enum RecordType
	{
		/// <summary>
		/// Type is not recognized as record type.
		/// </summary>
		NotRecord     = 0x00,
		/// <summary>
		/// Type is C# record class or any other class with constructor parameter mathing properties by name.
		/// </summary>
		RecordClass   = 0x01,
		/// <summary>
		/// Type is C# or VB.NET anonymous type.
		/// </summary>
		AnonymousType = 0x02,
	}

	internal static class RecordsHelper
	{
		private static readonly ConcurrentDictionary<Type, RecordType> _recordCache = new ();

		internal static RecordType GetRecordType(Type objectType)
		{
			return _recordCache.GetOrAdd(objectType, static objectType =>
			{
				if (objectType.IsAnonymous())
					return RecordType.AnonymousType;

				if (!HasDefaultConstructor(objectType))
					return RecordType.RecordClass;

				return RecordType.NotRecord;
			});
		}

		private static bool HasDefaultConstructor(Type objectType)
		{
			var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var constructor in constructors)
			{
				if (constructor.GetParameters().Length == 0)
					return true;
			}

			return constructors.Length == 0;
		}
	}
}
