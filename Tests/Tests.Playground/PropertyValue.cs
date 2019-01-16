using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.Extensions;

namespace Tests.Playground
{
  public sealed class PropertyValue : IEquatable<PropertyValue>, IEnumerable<BaseEntity>
    {
	    public PropertyInfo Member { get; }
	    private readonly object _ownerObject;
	    private Type _type;
	    private object _value;

	    public PropertyValue([NotNull] object ownerObject, [NotNull] PropertyInfo member)
        {
	        Member = member ?? throw new ArgumentNullException(nameof(member));
	        _ownerObject = ownerObject ?? throw new ArgumentNullException(nameof(ownerObject));
        }

	    public PropertyValue(object value, [NotNull] Type type)
	    {
		    Value = value;
		    _type = type ?? throw new ArgumentNullException(nameof(type));
	    }

	    public static PropertyValue Create<T>(T value)
	    {
			return new PropertyValue(value, typeof(string));
	    }

	    public Type Type => _type ?? Member.PropertyType;

	    public object Value
	    {
		    get
		    {
			    if (_ownerObject == null)
				    return _value;
			    return Member.GetValue(_ownerObject);
		    }
		    set
		    {
			    if (_ownerObject == null)
				    _value = value;
			    else
			    {
				    if (_ownerObject == null)
					    _value = value;
					else if (_value == null)
						Member.SetValue(_ownerObject, null);
					else
					    Member.SetValue(_ownerObject, Convert.ChangeType(value, Member.PropertyType));
			    }
		    }
	    }

	    public PropertyValue this[string name]
	    {
		    get => throw new NotImplementedException();
		    set => throw new NotImplementedException();
	    }

	    public T As<T>()
	    {
		    throw new NotImplementedException();
	    }

        #region int

        public static implicit operator int(PropertyValue propertyValue)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>();

            return (int)propertyValue.Value;
        }

        public static bool operator >(PropertyValue propertyValue, int value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>();

            return (int)propertyValue.Value > value;
        }

        public static bool operator <(PropertyValue propertyValue, int value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>();

            return (int)propertyValue.Value < value;
        }

        public static bool operator >=(PropertyValue propertyValue, int value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>();

            return (int)propertyValue.Value >= value;
        }

        public static bool operator <=(PropertyValue propertyValue, int value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>();

            return (int)propertyValue.Value <= value;
        }

        #endregion

        #region string

        public static implicit operator string(PropertyValue propertyValue)
        {
            propertyValue.Type.ThrowIfTypeMismatch<string>();

            return propertyValue.Value as string;
        }

//	    public static implicit operator PropertyValue(string value)
//	    {
//		    return Create(value);
//	    }
//
	    public void SetValue<T>(T value)
	    {
		    object objectValue = value;
		    var memberType = Member.GetMemberType();
		    if (memberType != typeof(T))
		    {
			    objectValue = Convert.ChangeType(objectValue, memberType);
		    }
			Member.SetValue(_ownerObject, objectValue);
	    }

        public static bool operator ==(PropertyValue propertyValue, string value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<string>();

            return propertyValue.Value as string == value;
        }

        public static bool operator !=(PropertyValue propertyValue, string value)
        {
            propertyValue.Type.ThrowIfTypeMismatch<string>();

            return propertyValue.Value as string != value;
        }

        #endregion

        public static PropertyValue operator ++(PropertyValue propertyValue)
        {
            propertyValue.Type.ThrowIfTypeMismatch<int>(); // supports just int. has to support any other type that supports ++ operator.

            var newValue = Convert.ToInt32(propertyValue.Value) + 1;
	        propertyValue.SetValue(newValue);

            return propertyValue;
        }

        public static bool operator ==(PropertyValue value1, PropertyValue value2)
        {
            return EqualityComparer<PropertyValue>.Default.Equals(value1, value2);
        }

        public static bool operator !=(PropertyValue value1, PropertyValue value2)
        {
            return !(value1 == value2);
        }

	    public bool Equals(PropertyValue other)
	    {
		    if (ReferenceEquals(null, other)) return false;
		    if (ReferenceEquals(this, other)) return true;
		    return Equals(Type, other.Type) && Equals(Value, other.Value);
	    }

	    public IEnumerator<BaseEntity> GetEnumerator()
	    {
		    return Enumerable.Empty<BaseEntity>().GetEnumerator();
	    }

	    public override bool Equals(object obj)
	    {
		    if (ReferenceEquals(null, obj)) return false;
		    if (ReferenceEquals(this, obj)) return true;
		    return obj is PropertyValue other && Equals(other);
	    }

	    public override int GetHashCode()
	    {
		    unchecked
		    {
			    return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
		    }
	    }

	    IEnumerator IEnumerable.GetEnumerator()
	    {
		    return GetEnumerator();
	    }
    }

    internal static class TypeExtensions
    {
        internal static void ThrowIfTypeMismatch<T>(this Type type)
        {
            if (type != typeof(T))
            {
                throw new InvalidOperationException("Type mismatch.");
            }
        }
    }

}
