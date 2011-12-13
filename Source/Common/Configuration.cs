using System;

namespace LinqToDB.Common
{
	using Mapping;

	public static class Configuration
	{
		public enum NullEquivalent { DBNull, Null, Value }

		private static NullEquivalent _checkNullReturnIfNull = NullEquivalent.DBNull;
		/// <summary>
		/// Specifies what value should be returned by <c>TypeAccessor.CheckNull</c>
		/// if <see cref="LinqToDB.Reflection.IsNullHandler"/> was specified and interpreted current property 
		/// value as null. Default is: <see cref="DBNull"/>.
		/// </summary>
		public  static NullEquivalent  CheckNullReturnIfNull
		{
			get { return _checkNullReturnIfNull;  }
			set { _checkNullReturnIfNull = value; }
		}

		/// <summary>
		/// Controls global trimming behaviour of mapper. Specifies whether trailing spaces
		/// should be trimmed when mapping from one entity to another. Default is: false. 
		/// To specify trimming behaviour other than global, please user <see cref="TrimmableAttribute"/>.
		/// </summary>
		public static bool TrimOnMapping { get; set; }

		private static bool _trimDictionaryKey = true;
		/// <summary>
		/// Controls global trimming behaviour of mapper for dictionary keys. Specifies whether trailing spaces
		/// should be trimmed when adding keys to dictionaries. Default is: true. 
		/// </summary>
		public  static bool  TrimDictionaryKey
		{
			get { return _trimDictionaryKey;  }
			set { _trimDictionaryKey = value; }
		}

		private static bool _notifyOnEqualSet = true;
		/// <summary>
		/// Specifies default behavior for PropertyChange generation. If set to true, <see cref="LinqToDB.EditableObjects.EditableObject.OnPropertyChanged"/>
		/// is invoked even when current value is same as new one. If set to false,  <see cref="LinqToDB.EditableObjects.EditableObject.OnPropertyChanged"/> 
		/// is invoked only when new value is being assigned. To specify notification behaviour other than default, please see 
		/// <see cref="LinqToDB.TypeBuilder.PropertyChangedAttribute"/>
		/// </summary>
		public  static bool  NotifyOnEqualSet
		{
			get { return _notifyOnEqualSet;  }
			set { _notifyOnEqualSet = value; }
		}

		/// <summary>
		/// Controls whether attributes specified on base types should be always added to list of attributes
		/// when scanning hierarchy tree or they should be compared to attributes found on derived classes
		/// and added only when not present already. Default value: false;
		/// WARNING: setting this flag to "true" can significantly affect initial object generation/access performance
		/// use only when side effects are noticed with attribute being present on derived and base classes. 
		/// For builder attributes use provided attribute compatibility mechanism.
		/// </summary>
		public static bool FilterOutBaseEqualAttributes { get; set; }

		private static bool _openNewConnectionToDiscoverParameters = true;
		/// <summary>
		/// Controls whether attributes specified on base types should be always added to list of attributes
		/// when scanning hierarchy tree or they should be compared to attributes found on derived classes
		/// and added only when not present already. Default value: false;
		/// WARNING: setting this flag to "true" can significantly affect initial object generation/access performance
		/// use only when side effects are noticed with attribute being present on derived and base classes. 
		/// For builder attributes use provided attribute compatibility mechanism.
		/// </summary>
		public  static bool OpenNewConnectionToDiscoverParameters
		{
			get { return _openNewConnectionToDiscoverParameters; }
			set { _openNewConnectionToDiscoverParameters = value; }
		}

		public static class Linq
		{
			public static bool PreloadGroups      { get; set; }
			public static bool IgnoreEmptyUpdate  { get; set; }
			public static bool AllowMultipleQuery { get; set; }
		}

		public static class NullableValues
		{
			public static Int32          Int32          = 0;
			public static Double         Double         = 0;
			public static Int16          Int16          = 0;
			public static Boolean        Boolean        = false;
			[CLSCompliant(false)]
			public static SByte          SByte          = 0;
			public static Int64          Int64          = 0;
			public static Byte           Byte           = 0;
			[CLSCompliant(false)]
			public static UInt16         UInt16         = 0;
			[CLSCompliant(false)]
			public static UInt32         UInt32         = 0;
			[CLSCompliant(false)]
			public static UInt64         UInt64         = 0;
			public static Single         Single         = 0;
			public static Char           Char           = '\x0';
			public static DateTime       DateTime       = DateTime.MinValue;
			public static TimeSpan       TimeSpan       = TimeSpan.MinValue;
			public static DateTimeOffset DateTimeOffset = DateTimeOffset.MinValue;
			public static Decimal        Decimal        = 0m;
			public static Guid           Guid           = Guid.Empty;
			public static String         String         = string.Empty;
		}
	}
}
