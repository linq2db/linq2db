using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ParameterContainer
	{
		List<ParameterAccessor>? _accessors;

		public int RegisterAccessor(ParameterAccessor accessor)
		{
			_accessors ??= new List<ParameterAccessor>();
			_accessors.Add(accessor);
			return _accessors.Count - 1;
		}

		public Expression?   ParameterExpression { get; set; }
		public object?[]?    CompiledParameters  { get; set; }
		public IDataContext? DataContext         { get; set; }

		public static readonly MethodInfo GetValueMethodInfo =
			MemberHelper.MethodOf<ParameterContainer>(c => c.GetValue(0));

		public object? GetValue(int index)
		{
			if (_accessors == null || index >= _accessors.Count)
				throw new InvalidOperationException();
			
			var accessor = _accessors[index];
			
			return accessor.Accessor(ParameterExpression!, DataContext, CompiledParameters);
		}
	}
}
