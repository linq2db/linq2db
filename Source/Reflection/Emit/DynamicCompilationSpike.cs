#region

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace LinqToDB.Reflection.Emit
{
    public delegate object GetHandler(object source);

    public delegate void SetHandler(object source, object value);

    public delegate object InstantiateObjectHandler();

    public delegate Object ParamsConstructorDelegate(params object[] parameters);

    public static class DynamicMethodCompiler
    {
        // DynamicMethodCompiler

        public static ParamsConstructorDelegate Build(Type type, Type[] constructorArgsTypes)
        {
            if (constructorArgsTypes == null)
                constructorArgsTypes = new Type[0];

            var mthd = new DynamicMethod(".ctor", type, new[] {typeof (object[])});
            var il = mthd.GetILGenerator();
            var ctor = type.GetConstructor(constructorArgsTypes);
            var ctorParams = ctor.GetParameters();
            for (int i = 0; i < ctorParams.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        il.Emit(OpCodes.Ldc_I4_8);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4, i);
                        break;
                }
                il.Emit(OpCodes.Ldelem_Ref);
                Type paramType = ctorParams[i].ParameterType;
                il.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
            }
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return (ParamsConstructorDelegate) mthd.CreateDelegate(typeof (ParamsConstructorDelegate));
        }

        // CreateInstantiateObjectDelegate
        internal static InstantiateObjectHandler CreateInstantiateObjectHandler(Type type, Type[] constructorArgsTypes)
        {
            if (constructorArgsTypes == null)
                constructorArgsTypes = new Type[0];

            ConstructorInfo constructorInfo =
                type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    constructorArgsTypes, null);
            if (constructorInfo == null)
            {
                throw new ApplicationException(
                    string.Format(
                        "The type {0} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).",
                        type));
            }

            var dynamicMethod = new DynamicMethod("InstantiateObject", MethodAttributes.Static | MethodAttributes.Public,
                CallingConventions.Standard, typeof (object), null, type, true);
            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Newobj, constructorInfo);
            generator.Emit(OpCodes.Ret);
            return (InstantiateObjectHandler) dynamicMethod.CreateDelegate(typeof (InstantiateObjectHandler));
        }

        // CreateGetDelegate
        internal static GetHandler CreateGetHandler(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Call, getMethodInfo);
            BoxIfNeeded(getMethodInfo.ReturnType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (GetHandler) dynamicGet.CreateDelegate(typeof (GetHandler));
        }

        // CreateGetDelegate
        internal static GetHandler CreateGetHandler(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            BoxIfNeeded(fieldInfo.FieldType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (GetHandler) dynamicGet.CreateDelegate(typeof (GetHandler));
        }

        // CreateSetDelegate
        internal static SetHandler CreateSetHandler(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
            setGenerator.Emit(OpCodes.Call, setMethodInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (SetHandler) dynamicSet.CreateDelegate(typeof (SetHandler));
        }

        // CreateSetDelegate
        internal static SetHandler CreateSetHandler(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(fieldInfo.FieldType, setGenerator);
            setGenerator.Emit(OpCodes.Stfld, fieldInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (SetHandler) dynamicSet.CreateDelegate(typeof (SetHandler));
        }

        // CreateGetDynamicMethod
        private static DynamicMethod CreateGetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicGet", typeof (object), new[] {typeof (object)}, type, true);
        }

        // CreateSetDynamicMethod
        private static DynamicMethod CreateSetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicSet", typeof (void), new[] {typeof (object), typeof (object)}, type, true);
        }

        // BoxIfNeeded
        private static void BoxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        // UnboxIfNeeded
        private static void UnboxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, type);
            }
        }

        public static DynamicMethod ConstructThenExecMethod(Type t, string methodName, Type returnType, Type[] paramsTypes)
        {
            var method1 = new DynamicMethod("ExecMethod" + methodName, returnType, paramsTypes);
            ILGenerator il = method1.GetILGenerator();

            ConstructorInfo cil = t.GetConstructor(new Type[] { });
            il.Emit(OpCodes.Newobj, cil);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, t.GetMethod(methodName));
            il.Emit(OpCodes.Ret);

            return method1;
        }

        public static DynamicMethod ExecMethod(Type t, string methodName, Type returnType, params Type[] paramsTypes)
        {
            var list = paramsTypes.ToList();
            list.Insert(0, t);

            var method1 = new DynamicMethod("ExecMethod" + methodName, returnType, list.ToArray(), t);
            ILGenerator il = method1.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, t.GetMethod(methodName, paramsTypes));
            il.Emit(OpCodes.Ret);

            return method1;
        }
    }

    public static class MethodExtensions
    {
        public static object InvokeStatic(this DynamicMethod method,
            params object[] args)
        {
            return method.Invoke(null, args);
        }

        public static T InvokeStatic<T>(this DynamicMethod method,
            params object[] args)
        {
            return (T) method.InvokeStatic(args);
        }
    }
}