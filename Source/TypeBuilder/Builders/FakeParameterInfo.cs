using System;
using System.Collections;
using System.Reflection;

namespace LinqToDB.TypeBuilder.Builders
{
	class FakeParameterInfo : ParameterInfo
	{
		public FakeParameterInfo(string name, Type type, MemberInfo memberInfo, object[] attributes)
		{
			_name       = name;
			_type       = type;
			_memberInfo = memberInfo;
			_attributes = attributes ?? new object[0];
		}

		public FakeParameterInfo(MethodInfo method) : this(
			"ret",
			method.ReturnType,
			method,
			method.ReturnTypeCustomAttributes.GetCustomAttributes(true))
		{
		}

		public override ParameterAttributes Attributes
		{
			get { return ParameterAttributes.Retval; }
		}

		public override object DefaultValue
		{
			get { return DBNull.Value; }
		}

		private readonly object[] _attributes;

		public override object[] GetCustomAttributes(bool inherit)
		{
			return _attributes;
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			if (_attributes.Length == 0)
				return (object[]) Array.CreateInstance(attributeType, 0);

			ArrayList list = new ArrayList();

			foreach (object o in _attributes)
				if (o.GetType() == attributeType || attributeType.IsInstanceOfType(o))
					list.Add(o);

			return (object[]) list.ToArray(attributeType);
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			foreach (object o in _attributes)
				if (o.GetType() == attributeType || attributeType.IsInstanceOfType(o))
					return true;

			return false;
		}

		private readonly MemberInfo _memberInfo;
		public  override MemberInfo  Member
		{
			get { return _memberInfo; }
		}

		private readonly string _name;
		public  override string  Name
		{
			get { return _name; }
		}

		private readonly Type _type;
		public  override Type  ParameterType
		{
			get { return _type; }
		}

		public override int Position
		{
			get { return 0; }
		}
	}
}
