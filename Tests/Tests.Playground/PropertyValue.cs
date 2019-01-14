using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Playground
{
  public sealed class PropertyValue : IEquatable<PropertyValue>, IEnumerable<BaseEntity>
    {
        public PropertyValue(Type type, object value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));

            Value = value; // Value can be null
        }

        public Type Type { get; }

        public object Value { get; set; }

        public static PropertyValue Create<T>(T value)
        {
            return new PropertyValue(typeof(T), value);
        }

	    public PropertyValue this[string name]
	    {
		    get => throw new NotImplementedException();
		    set => throw new NotImplementedException();
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

            return new PropertyValue(propertyValue.Type, newValue);
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
