using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

using LinqToDB.Reflection;
using LinqToDB.Reflection.Emit;

namespace LinqToDB.TypeBuilder.Builders
{
	[DebuggerStepThrough]
	public class BuildContext
	{
		public BuildContext(Type type)
		{
			Type  = type;
			Items = new Dictionary<object,object>(10);
		}

		public TypeHelper                Type            { get; private set; }
		public AssemblyBuilderHelper     AssemblyBuilder { get; set; }
		public TypeBuilderHelper         TypeBuilder     { get; set; }
		public Dictionary<object,object> Items           { get; private set; }

		public T GetItem<T>(string key)
		{
			object value;
			return Items.TryGetValue(key, out value) ? (T)value : default(T);
		}

		private Dictionary<PropertyInfo,FieldBuilder> _fields;
		public  Dictionary<PropertyInfo,FieldBuilder>  Fields
		{
			get { return _fields ?? (_fields = new Dictionary<PropertyInfo, FieldBuilder>(10)); }
		}

		private IDictionary<TypeHelper,IAbstractTypeBuilder> _interfaceMap;
		public  IDictionary<TypeHelper,IAbstractTypeBuilder>  InterfaceMap
		{
			get { return _interfaceMap ?? (_interfaceMap = new Dictionary<TypeHelper,IAbstractTypeBuilder>()); }
		}

		public TypeHelper          CurrentInterface { get; set; }
		public MethodBuilderHelper MethodBuilder    { get; set; }
		public LocalBuilder        ReturnValue      { get; set; }
		public LocalBuilder        Exception        { get; set; }
		public Label               ReturnLabel      { get; set; }

		#region BuildElement

		public BuildElement BuildElement { get; set; }

		public bool IsAbstractGetter   { get { return BuildElement == BuildElement.AbstractGetter; } }
		public bool IsAbstractSetter   { get { return BuildElement == BuildElement.AbstractSetter; } }
		public bool IsAbstractProperty { get { return IsAbstractGetter || IsAbstractSetter;        } }
		public bool IsAbstractMethod   { get { return BuildElement == BuildElement.AbstractMethod; } }
		public bool IsVirtualGetter    { get { return BuildElement == BuildElement.VirtualGetter;  } }
		public bool IsVirtualSetter    { get { return BuildElement == BuildElement.VirtualSetter;  } }
		public bool IsVirtualProperty  { get { return IsVirtualGetter  || IsVirtualSetter;         } }
		public bool IsVirtualMethod    { get { return BuildElement == BuildElement.VirtualMethod;  } }
		public bool IsGetter           { get { return IsAbstractGetter || IsVirtualGetter;         } }
		public bool IsSetter           { get { return IsAbstractSetter || IsVirtualSetter;         } }
		public bool IsProperty         { get { return IsGetter         || IsSetter;                } }
		public bool IsMethod           { get { return IsAbstractMethod || IsVirtualMethod;         } }
		public bool IsMethodOrProperty { get { return IsMethod         || IsProperty;              } }

		#endregion

		#region BuildStep

		public BuildStep Step { get; set; }

		public bool IsBeginStep   { get { return Step == BuildStep.Begin;   } }
		public bool IsBeforeStep  { get { return Step == BuildStep.Before;  } }
		public bool IsBuildStep   { get { return Step == BuildStep.Build;   } }
		public bool IsAfterStep   { get { return Step == BuildStep.After;   } }
		public bool IsCatchStep   { get { return Step == BuildStep.Catch;   } }
		public bool IsFinallyStep { get { return Step == BuildStep.Finally; } }
		public bool IsEndStep     { get { return Step == BuildStep.End;     } }

		public bool IsBeforeOrBuildStep
		{
			get { return Step == BuildStep.Before || Step == BuildStep.Build; }
		}

		#endregion

		public AbstractTypeBuilderList TypeBuilders    { get; set; }
		public PropertyInfo            CurrentProperty { get; set; }
		public MethodInfo              CurrentMethod   { get; set; }

		#region Internal Methods

		public FieldBuilder GetField(string fieldName)
		{
			return GetItem<FieldBuilder>("$LinqToDB.Field." + fieldName);
		}

		public FieldBuilder CreateField(string fieldName, Type type, FieldAttributes attributes)
		{
			var field = TypeBuilder.DefineField(fieldName, type, attributes);

			Items.Add("$LinqToDB.Field." + fieldName, field);

			return field;
		}

		public FieldBuilder CreatePrivateField(string fieldName, Type type)
		{
			return CreateField(fieldName, type, FieldAttributes.Private);
		}

		public FieldBuilder CreatePrivateField(PropertyInfo propertyInfo, string fieldName, Type type)
		{
			var field = CreateField(fieldName, type, FieldAttributes.Private);

			if (propertyInfo != null)
				Fields.Add(propertyInfo, field);

			return field;
		}

		public FieldBuilder CreatePrivateStaticField(string fieldName, Type type)
		{
			return CreateField(fieldName, type, FieldAttributes.Private | FieldAttributes.Static);
		}

		public MethodBuilderHelper GetFieldInstanceEnsurer(string fieldName)
		{
			return GetItem<MethodBuilderHelper>("$LinqToDB.FieldInstanceEnsurer." + fieldName);
		}

		#endregion
	}
}
