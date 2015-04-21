using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Common.FSharp
{
    abstract class Option
    {

        public static bool IsOption(Type type)
        {
            return type.Name == "FSharpOption`1";
        }

        public static bool IsSome(object value)
        {
             return (bool)value
                .GetType()
                .GetMethods()
                .Single(m => m.Name == "get_IsSome")
                .Invoke(null, new object[] { value });
        }

        public static bool IsNone(object value)
        {
            return !IsSome(value);
        }

        public static object GetValue(object value)
        {
            return value
                .GetType()
                .GetMethods()
                .Single(m => m.Name == "get_Value")
                .Invoke(value, new object[] { });
        }

        public static Type GetUnderlyingType(Type type)
        {
            return type.GetGenericArguments()[0];
        }

    }
}
