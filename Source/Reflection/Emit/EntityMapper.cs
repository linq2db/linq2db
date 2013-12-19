#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace LinqToDB.Reflection.Emit
{
    public class Converter
    {
        public static Int16 ToInt16(object value)
        {
            return IsDbNull<Int16>(value);
        }

        public static Int32 ToInt32(object value)
        {
            return IsDbNull<Int32>(value);
        }

        public static Int64 ToInt64(object value)
        {
            return IsDbNull<Int64>(value);
        }

        public static Single ToSingle(object value)
        {
            return IsDbNull<Single>(value);
        }

        public static Boolean ToBoolean(object value)
        {
            return IsDbNull<Boolean>(value);
        }

        public static String ToString(object value)
        {
            return IsDbNull<String>(value);
        }

        public static DateTime ToDateTime(object value)
        {
            return IsDbNull<DateTime>(value);
        }

        public static Decimal ToDecimal(object value)
        {
            return IsDbNull<Decimal>(value);
        }

        public static Double ToDouble(object value)
        {
            return IsDbNull<Double>(value);
        }

        public static Guid ToGuid(object value)
        {
            return IsDbNull<Guid>(value);
        }

        public static Byte ToByte(object value)
        {
            return IsDbNull<Byte>(value);
        }

        public static Byte[] ToBytes(object value)
        {
            return IsDbNull<Byte[]>(value);
        }

        public static DateTime? ToDateTimeNull(object value)
        {
            return IsDbNull<DateTime?>(value);
        }

        public static Int32? ToInt32Null(object value)
        {
            return IsDbNull<Int32?>(value);
        }

        private static T IsDbNull<T>(object value)
        {
            return IsDbNull(value, default(T));
        }

        private static T IsDbNull<T>(object value, T returnedValue)
        {
            if (value == DBNull.Value)
                return returnedValue;
            return (T) value;
        }
    }

    public class Mapper
    {
        //Mapping Delegate

        private static Dictionary<Type, Delegate> m_mapperCache;

        private Dictionary<Type, Delegate> MapperCache
        {
            get
            {
                if (m_mapperCache == null)
                    m_mapperCache = new Dictionary<Type, Delegate>();
                return m_mapperCache;
            }
        }

        public void ClearMappingCache()
        {
            MapperCache.Clear();
        }

        public T ToT<T>(DbDataReader reader)
        {
            if (reader.Read())
                return ToT<T>(reader, GetMappingMethod<T>());
            return default(T);
        }

        public L ToList<L, T>(DbDataReader reader)
            where L : IList<T>, new()
            where T : class, new()
        {
            ToTDelegate<T> mappingMethod = GetMappingMethod<T>();
            var list = new L();
            while (reader.Read())
                list.Add(ToT<T>(reader, mappingMethod));
            return list;
        }

        public IEnumerable<T> ToEnumerable<T>(DbDataReader reader)
        {
            ToTDelegate<T> mappingMethod = GetMappingMethod<T>();
            while (reader.Read())
                yield return ToT<T>(reader, mappingMethod);
        }

        private T ToT<T>(DbDataReader reader, ToTDelegate<T> mapper)
        {
            return mapper(reader);
        }

        private ToTDelegate<T> GetMappingMethod<T>()
        {
            Type typeT = typeof (T);
            if (!MapperCache.ContainsKey(typeT))
            {
                Type[] methodArgs = {typeof (DbDataReader)};
                var dm = new DynamicMethod("MapDatareader", typeof (T), methodArgs, typeof (T));

                ILGenerator il = dm.GetILGenerator();
                il.DeclareLocal(typeof (T));
                il.Emit(OpCodes.Newobj, typeof (T).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_0);

                PropertyInfo[] properties = typeT.GetProperties();

                foreach (PropertyInfo info in properties)
                {
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldarg_0);

                    il.Emit(OpCodes.Ldstr, info.Name);

                    MethodInfo methodInfo = typeof (DbDataReader).GetMethod("get_Item", new Type[] {typeof (string)});
                    il.Emit(OpCodes.Callvirt, methodInfo);

                    il.Emit(OpCodes.Call, GetConverterMethod(info.PropertyType));

                    il.Emit(OpCodes.Callvirt, typeof (T).GetMethod("set_" + info.Name, BindingFlags.Public | BindingFlags.Instance));
                }

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
                MapperCache.Add(typeof (T), dm.CreateDelegate(typeof (ToTDelegate<T>)));
            }
            return MapperCache[typeT] as ToTDelegate<T>;
        }

        private MethodInfo GetConverterMethod(Type type)
        {
            switch (type.Name.ToUpper())
            {
                case "INT16":
                    return CreateConverterMethodInfo("ToInt16");
                case "INT32":
                    return CreateConverterMethodInfo("ToInt32");
                case "INT64":
                    return CreateConverterMethodInfo("ToInt64");
                case "SINGLE":
                    return CreateConverterMethodInfo("ToSingle");
                case "BOOLEAN":
                    return CreateConverterMethodInfo("ToBoolean");
                case "STRING":
                    return CreateConverterMethodInfo("ToString");
                case "DATETIME":
                    return CreateConverterMethodInfo("ToDateTime");
                case "DECIMAL":
                    return CreateConverterMethodInfo("ToDecimal");
                case "DOUBLE":
                    return CreateConverterMethodInfo("ToDouble");
                case "GUID":
                    return CreateConverterMethodInfo("ToGuid");
                case "BYTE[]":
                    return CreateConverterMethodInfo("ToBytes");
                case "BYTE":
                    return CreateConverterMethodInfo("ToByte");
                case "NULLABLE`1":
                    return CreateConverterMethodInfo("ToDateTimeNull");
            }
            return null;
        }

        private MethodInfo CreateConverterMethodInfo(string method)
        {
            return typeof (Converter).GetMethod(method, new Type[] {typeof (object)});
        }

        private delegate T ToTDelegate<T>(DbDataReader reader);
    }

    // TODO Choose between Mapper and EntityMapper class
    public class EntityMapper
    {
        private static readonly Dictionary<Type, Delegate> cachedMappers = new Dictionary<Type, Delegate>();

        public static IEnumerable<T> MapToEntities<T>(IDataReader dr)
        {
            // If a mapping function from dr -> T does not exist, create and cache one
            if (!cachedMappers.ContainsKey(typeof (T)))
            {
                // Our method will take in a single parameter, a DbDataReader
                Type[] methodArgs = {typeof (DbDataReader)};

                // The MapDR method will map a DbDataReader row to an instance of type T
                var dm = new DynamicMethod("MapDR", typeof (T), methodArgs, Assembly.GetExecutingAssembly().GetType().Module);
                ILGenerator il = dm.GetILGenerator();

                // We'll have a single local variable, the instance of T we're mapping
                il.DeclareLocal(typeof (T));

                // Create a new instance of T and save it as variable 0
                il.Emit(OpCodes.Newobj, typeof (T).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_0);

                foreach (PropertyInfo pi in typeof (T).GetProperties())
                {
                    // Load the T instance, SqlDataReader parameter and the field name onto the stack
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, pi.Name);

                    // Push the column value onto the stack
                    il.Emit(OpCodes.Callvirt, typeof (DbDataReader).GetMethod("get_Item", new Type[] {typeof (string)}));

                    // Depending on the type of the property, convert the datareader column value to the type
                    switch (pi.PropertyType.Name)
                    {
                        case "Int16":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToInt16", new Type[] {typeof (object)}));
                            break;
                        case "Int32":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToInt32", new Type[] {typeof (object)}));
                            break;
                        case "Int64":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToInt64", new Type[] {typeof (object)}));
                            break;
                        case "Boolean":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToBoolean", new Type[] {typeof (object)}));
                            break;
                        case "String":
                            il.Emit(OpCodes.Callvirt, typeof (string).GetMethod("ToString", new Type[] {}));
                            break;
                        case "DateTime":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToDateTime", new Type[] {typeof (object)}));
                            break;
                        case "Decimal":
                            il.Emit(OpCodes.Call, typeof (Convert).GetMethod("ToDecimal", new Type[] {typeof (object)}));
                            break;
                        default:
                            // Don't set the field value as it's an unsupported type
                            continue;
                    }

                    // Set the T instances property value
                    il.Emit(OpCodes.Callvirt, typeof (T).GetMethod("set_" + pi.Name, new Type[] {pi.PropertyType}));
                }

                // Load the T instance onto the stack
                il.Emit(OpCodes.Ldloc_0);

                // Return
                il.Emit(OpCodes.Ret);

                // Cache the method so we won't have to create it again for the type T
                cachedMappers.Add(typeof (T), dm.CreateDelegate(typeof (mapEntity<T>)));
            }

            // Get a delegate reference to the dynamic method
            var invokeMapEntity = (mapEntity<T>) cachedMappers[typeof (T)];

            // For each row, map the row to an instance of T and yield return it
            while (dr.Read())
                yield return invokeMapEntity(dr);
        }

        public static void ClearCachedMapperMethods()
        {
            cachedMappers.Clear();
        }

        private delegate T mapEntity<T>(IDataReader dr);
    }
}