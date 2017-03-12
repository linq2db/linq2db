using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Tests.OrmBattle.Helper
{
    public class GenericEqualityComparer<T> : IComparable<T>, IEqualityComparer<T>
    {
        private readonly List<PropertyInfo> _propertyInfos = new List<PropertyInfo>();

        public GenericEqualityComparer(params Expression<Func<T, object>>[] property)
        {
            var props = property.Select(pe => ExpressionUtils.ExtractMember(pe).Member).OfType<PropertyInfo>();

            _propertyInfos.AddRange(props);
        }

        #region IEqualityComparer Members

        public bool Equals(T x, T y)
        {
            foreach (var propertyInfo in _propertyInfos)
            {
                //get the current value of the comparison property of x and of y
                var xValue = propertyInfo.GetValue(x, null);
                var yValue = propertyInfo.GetValue(y, null);

                //if the xValue is null then we consider them equal if and only if yValue is null
                if (xValue == null)
                {
                    if (yValue != null)
                        return false;
                }

                if (!xValue.Equals(yValue))
                    return false;
            }
            return true;
        }

        public int GetHashCode(T obj)
        {
            var values = new List<object>();
            foreach (var propertyInfo in _propertyInfos)
            {
                //get the current value of the comparison property of x and of y
                var xValue = propertyInfo.GetValue(obj, null);
                values.Add(xValue);
            }
            return HashCodeBuilder.Hash(values.ToList());
        }

        #endregion

        public int CompareTo(T other)
        {
            var x = this;
            var y = other;
            foreach (var propertyInfo in _propertyInfos)
            {
                //get the current value of the comparison property of x and of y
                var xValue = propertyInfo.GetValue(x, null);
                var yValue = propertyInfo.GetValue(y, null);

                //if the xValue is null then we consider them equal if and only if yValue is null
                if (xValue == null)
                {
                    if (yValue != null)
                        return -1;
                }

                if (!xValue.Equals(yValue))
                    return 1;
            }

            return 0;
        }
    }

    public static class HashCodeBuilder
    {
        public static int Hash(params object[] args)
        {
            if (args == null)
            {
                return 0;
            }

            var num = 42;

            unchecked
            {
                foreach (var item in args)
                {
                    if (ReferenceEquals(item, null))
                    {
                    }
                    else if (item.GetType().IsArray)
                    {
                        foreach (var subItem in (IEnumerable) item)
                        {
                            num = num * 37 + Hash(subItem);
                        }
                    }
                    else
                    {
                        num = num * 37 + item.GetHashCode();
                    }
                }
            }

            return num;
        }
    }
}