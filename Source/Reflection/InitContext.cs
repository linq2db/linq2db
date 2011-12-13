using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Mapping;

namespace LinqToDB.Reflection
{
	public class InitContext
	{
		public object[]       MemberParameters { get; set; }
		public object[]       Parameters       { get; set; }
		public bool           IsInternal       { get; set; }
		public bool           IsLazyInstance   { get; set; }
		public object         Parent           { get; set; }
		public object         SourceObject     { get; set; }
		public ObjectMapper   ObjectMapper     { get; set; }
		public MappingSchema  MappingSchema    { get; set; }
		public bool           IsSource         { get; set; }
		public bool           StopMapping      { get; set; }
		[CLSCompliant(false)]
		public IMapDataSource DataSource       { get; set; }

		private Dictionary<object,object> _items;
		public  Dictionary<object,object>  Items
		{
			[DebuggerStepThrough] 
			get { return _items ?? (_items = new Dictionary<object, object>()); }
		}

		public  bool  IsDestination
		{
			[DebuggerStepThrough] get { return !IsSource;  }
			[DebuggerStepThrough] set { IsSource = !value; }
		}
	}
}
