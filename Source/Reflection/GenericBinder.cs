using System;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Reflection
{
	/// <Summary>
	/// Selects a member from a list of candidates, and performs type conversion
	/// from actual argument type to formal argument type.
	/// </Summary>
	[Serializable]
	public class GenericBinder : Binder
	{
		private readonly bool _genericMethodDefinition;
		public GenericBinder(bool genericMethodDefinition)
		{
			_genericMethodDefinition = genericMethodDefinition;
		}

		#region System.Reflection.Binder methods

		public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args,
			ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
		{
			throw new NotImplementedException("GenericBinder.BindToMethod");
		}

		public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
		{
			throw new NotImplementedException("GenericBinder.BindToField");
		}

		public override MethodBase SelectMethod(
			BindingFlags        bindingAttr,
			MethodBase[]        matchMethods,
			Type[]              parameterTypes,
			ParameterModifier[] modifiers)
		{
			for (var i = 0; i < matchMethods.Length; ++i)
			{
				if (matchMethods[i].IsGenericMethodDefinition != _genericMethodDefinition)
					continue;

				var pis = matchMethods[i].GetParameters();
				var match = (pis.Length == parameterTypes.Length);

				for (var j = 0; match && j < pis.Length; ++j)
				{
					match = TypeHelper.CompareParameterTypes(pis[j].ParameterType, parameterTypes[j]);
				}

				if (match)
					return matchMethods[i];
			}

			return null;
		}

		public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType,
			Type[] indexes, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException("GenericBinder.SelectProperty");
		}

		public override object ChangeType(object value, Type type, CultureInfo culture)
		{
			throw new NotImplementedException("GenericBinder.ChangeType");
		}

		public override void ReorderArgumentArray(ref object[] args, object state)
		{
			throw new NotImplementedException("GenericBinder.ReorderArgumentArray");
		}

		#endregion

		private static GenericBinder _generic;
		public  static GenericBinder  Generic
		{
			get { return _generic ?? (_generic = new GenericBinder(true)); }
		}

		private static GenericBinder _nonGeneric;
		public  static GenericBinder  NonGeneric
		{
			get { return _nonGeneric ?? (_nonGeneric = new GenericBinder(false)); }
		}
	}
}
