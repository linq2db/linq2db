using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.TypeBuilder.Builders
{
	using Properties;
	using Reflection;
	using Reflection.Emit;

	internal class AbstractClassBuilder : ITypeBuilder
	{
		public AbstractClassBuilder(Type sourceType)
		{
			_sourceType = sourceType;
		}

		readonly Type _sourceType;

		public string AssemblyNameSuffix
		{
			get { return TypeBuilderConsts.AssemblyNameSuffix; }
		}

		public Type Build(AssemblyBuilderHelper assemblyBuilder)
		{
			_context  = new BuildContext(_sourceType);
			_builders = new AbstractTypeBuilderList();

			_context.TypeBuilders    = GetBuilderList(_context.Type);
			_context.AssemblyBuilder = assemblyBuilder;

			_builders.AddRange(_context.TypeBuilders);
			_builders.Add(_defaultTypeBuilder);

			return Build();
		}

		internal static string GetTypeFullName(Type type)
		{
			var name = type.FullName;

			if (type.IsGenericType)
			{
				name = name.Split('`')[0];

				foreach (var t in type.GetGenericArguments())
					name += "_" + GetTypeFullName(t).Replace('+', '_').Replace('.', '_');
			}

			return name;
		}

		internal static string GetTypeShortName(Type type)
		{
			var name = type.Name;

			if (type.IsGenericType)
			{
				name = name.Split('`')[0];

				foreach (var t in type.GetGenericArguments())
					name += "_" + GetTypeFullName(t).Replace('+', '_').Replace('.', '_');
			}

			return name;
		}

		public string GetTypeName()
		{
			var typeFullName  = _sourceType.FullName;
			var typeShortName = _sourceType.Name;

			if (_sourceType.IsGenericType)
			{
				typeFullName  = GetTypeFullName (_sourceType);
				typeShortName = GetTypeShortName(_sourceType);
			}

			typeFullName  = typeFullName. Replace('+', '.');
			typeShortName = typeShortName.Replace('+', '.');

			typeFullName = typeFullName.Substring(0, typeFullName.Length - typeShortName.Length);
			typeFullName = typeFullName + "LinqToDBExtension." + typeShortName;

			return typeFullName;
		}

		public Type GetBuildingType()
		{
			return _sourceType;
		}

		private static AbstractTypeBuilderList GetBuilderList(TypeHelper type)
		{
			var attrs    = type.GetAttributes(typeof(AbstractTypeBuilderAttribute));
			var builders = new AbstractTypeBuilderList(attrs.Length);

			foreach (AbstractTypeBuilderAttribute attr in attrs)
			{
				var builder = attr.TypeBuilder;

				if (builder != null)
				{
					builder.TargetElement = type;
					builders.Add(builder);
				}
			}

			return builders;
		}

		private static readonly DefaultTypeBuilder _defaultTypeBuilder = new DefaultTypeBuilder();

		private BuildContext            _context;
		private AbstractTypeBuilderList _builders;

		private Type Build()
		{
			DefineNonAbstractType();

			SetID(_builders);

			_context.BuildElement = BuildElement.Type;

			Build(BuildStep.Before, _builders);
			Build(BuildStep.Build,  _builders);

			var ids = _builders.ToDictionary(builder => builder, builder => builder.ID);

			DefineAbstractProperties();
			DefineAbstractMethods();
			OverrideVirtualProperties();
			OverrideVirtualMethods();
			DefineInterfaces();

			foreach (var builder in ids.Keys)
				builder.ID = ids[builder];

			_context.BuildElement = BuildElement.Type;

			Build(BuildStep.After, _builders);

			var initMethod = _context.Type.GetMethod("InitInstance", typeof(InitContext));

			// Finalize constructors.
			//
			if (_context.TypeBuilder.IsDefaultConstructorDefined)
			{
				if (initMethod != null)
					_context.TypeBuilder.DefaultConstructor.Emitter
						.ldarg_0
						.ldnull
						.callvirt (initMethod)
						;

				_context.TypeBuilder.DefaultConstructor.Emitter.ret();
			}

			if (_context.TypeBuilder.IsInitConstructorDefined)
			{
				if (initMethod != null)
					_context.TypeBuilder.InitConstructor.Emitter
						.ldarg_0
						.ldarg_1
						.callvirt (initMethod)
						;

				_context.TypeBuilder.InitConstructor.Emitter.ret();
			}

			if (_context.TypeBuilder.IsTypeInitializerDefined)
				_context.TypeBuilder.TypeInitializer.Emitter.ret();

			// Create the type.
			//
			return _context.TypeBuilder.Create();
		}

		private static int _idCounter;

		private static void SetID(AbstractTypeBuilderList builders)
		{
			foreach (var builder in builders)
				builder.ID = ++_idCounter;
		}

		private static void CheckCompatibility(BuildContext context, AbstractTypeBuilderList builders)
		{
			for (var i = 0; i < builders.Count; i++)
			{
				var cur = builders[i];

				if (cur == null)
					continue;

				for (var j = 0; j < builders.Count; j++)
				{
					var test = builders[j];

					if (i == j || test == null)
						continue;

					if (cur.IsCompatible(context, test) == false)
						builders[j] = null;
				}
			}

			for (var i = 0; i < builders.Count; i++)
				if (builders[i] == null)
					builders.RemoveAt(i--);
		}

		private void DefineNonAbstractType()
		{
			var interfaces = new List<Type>();

			if (_context.Type.IsInterface)
			{
				interfaces.Add(_context.Type);
				_context.InterfaceMap.Add(_context.Type, null);
			}

			foreach (var tb in _builders)
			{
				var types = tb.GetInterfaces();

				if (types != null)
				{
					foreach (var t in types)
					{
						if (t == null)
							continue;

						if (!t.IsInterface)
						{
							throw new InvalidOperationException(
								string.Format(Resources.AbstractClassBuilder_TypeIsNotAnInterface, t.FullName));
						}

						if (interfaces.Contains(t) == false)
						{
							interfaces.Add(t);
							_context.InterfaceMap.Add(t, tb);
						}
					}
				}
			}

			var typeName = GetTypeName();

			_context.TypeBuilder = _context.AssemblyBuilder.DefineType(
				typeName,
				TypeAttributes.Public
				| TypeAttributes.BeforeFieldInit
				| (TypeFactory.SealTypes? TypeAttributes.Sealed: 0),
				_context.Type.IsInterface? typeof(object): (Type)_context.Type,
				interfaces.ToArray());

			if (_context.Type.IsSerializable)
				_context.TypeBuilder.SetCustomAttribute(typeof(SerializableAttribute));

/*
			var dataContracts = _context.Type.GetCustomAttributes(typeof(DataContractAttribute));

			if (dataContracts.Length > 0)
			{
				foreach (DataContractAttribute a in dataContracts)
				{
					var builder = 
						new GeneratedAttributeBuilder(
							typeof(DataContractAttribute),
							null,
							new[] { "Name", "Namespace", "IsReference" },
							new object[] { a.Name, a.Namespace, a.IsReference });

					builder.Build(_context);
				}

				var emit = _context.TypeBuilder.DefineMethod("WcfKnownTypes", MethodAttributes.Static | MethodAttributes.Public, typeof(Type[])).Emitter;
				var loc = emit.DeclareLocal(typeof (Type[]));

				emit
					.ldc_i4_2
					.newarr(typeof(Type))
					.stloc(loc)
					.ldloc(loc)
					.ldc_i4_0
					.LoadType(_context.Type)
					.stelem_ref 
					.ldloc(loc)
					.ldc_i4_1
					.LoadType(_context.TypeBuilder.TypeBuilder)
					.stelem_ref 
					.ldloc(loc)
					.ret();

				new GeneratedAttributeBuilder(typeof(KnownTypeAttribute), new object[] { "WcfKnownTypes" }, null, null).Build(_context);
			}
*/
		}

		class BuilderComparer : IComparer<IAbstractTypeBuilder>
		{
			public BuilderComparer(BuildContext context)
			{
				_context = context;
			}

			readonly BuildContext _context;

			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public int Compare(IAbstractTypeBuilder x, IAbstractTypeBuilder y)
			{
				return y.GetPriority(_context) - x.GetPriority(_context);
			}
		}

		private void Build(BuildStep step, AbstractTypeBuilderList builders)
		{
			_context.Step = step;
			_context.TypeBuilders.Clear();

			foreach (var builder in builders)
				if (builder.IsApplied(_context, builders))
					_context.TypeBuilders.Add(builder);

			if (_context.IsVirtualMethod || _context.IsVirtualProperty)
				_context.TypeBuilders.Add(_defaultTypeBuilder);

			if (_context.TypeBuilders.Count == 0)
				return;

			CheckCompatibility(_context, _context.TypeBuilders);

			_context.TypeBuilders.Sort(new BuilderComparer(_context));

			for (var i = 0; i < _context.TypeBuilders.Count; i++)
			{
				var builder = _context.TypeBuilders[i];

				builder.Build(_context);
			}
		}

		private void BeginEmitMethod(MethodInfo method)
		{
			_context.CurrentMethod = method;
			_context.MethodBuilder = _context.TypeBuilder.DefineMethod(method);

			var emit = _context.MethodBuilder.Emitter;

			// Label right before return and catch block.
			//
			_context.ReturnLabel = emit.DefineLabel();

			// Create return value.
			//
			if (method.ReturnType != typeof(void))
			{
				_context.ReturnValue = _context.MethodBuilder.Emitter.DeclareLocal(method.ReturnType);
				emit.Init(_context.ReturnValue);
			}

			// Initialize out parameters.
			//
			var parameters = method.GetParameters();

			if (parameters != null)
				emit.InitOutParameters(parameters);
		}

		private void EmitMethod(
			AbstractTypeBuilderList builders, MethodInfo methdoInfo, BuildElement buildElement)
		{
			SetID(builders);

			_context.BuildElement = buildElement;

			var isCatchBlockRequired   = false;
			var isFinallyBlockRequired = false;

			foreach (var builder in builders)
			{
				isCatchBlockRequired   = isCatchBlockRequired   || IsApplied(builder, builders, BuildStep.Catch);
				isFinallyBlockRequired = isFinallyBlockRequired || IsApplied(builder, builders, BuildStep.Finally);
			}

			BeginEmitMethod(methdoInfo);

			Build(BuildStep.Begin,  builders);

			var emit        = _context.MethodBuilder.Emitter;
			var returnLabel = _context.ReturnLabel;

			// Begin catch block.
			//

			if (isCatchBlockRequired || isFinallyBlockRequired)
			{
				_context.ReturnLabel = emit.DefineLabel();
				emit.BeginExceptionBlock();
			}

			Build(BuildStep.Before, builders);
			Build(BuildStep.Build,  builders);
			Build(BuildStep.After,  builders);

			if (isCatchBlockRequired || isFinallyBlockRequired)
			{
				emit.MarkLabel(_context.ReturnLabel);
				_context.ReturnLabel = returnLabel;
			}

			// End catch block.
			//
			if (isCatchBlockRequired)
			{
				emit
					.BeginCatchBlock(typeof(Exception));

				_context.ReturnLabel = emit.DefineLabel();
				_context.Exception   = emit.DeclareLocal(typeof(Exception));

				emit
					.stloc (_context.Exception);

				Build(BuildStep.Catch, builders);

				emit
					.rethrow
					.end();

				emit.MarkLabel(_context.ReturnLabel);
				_context.ReturnLabel = returnLabel;
				_context.Exception   = null;
			}

			if (isFinallyBlockRequired)
			{
				emit.BeginFinallyBlock();
				_context.ReturnLabel = emit.DefineLabel();

				Build(BuildStep.Finally, builders);

				emit.MarkLabel(_context.ReturnLabel);
				_context.ReturnLabel = returnLabel;
			}

			if (isCatchBlockRequired || isFinallyBlockRequired)
				emit.EndExceptionBlock();

			Build(BuildStep.End, builders);

			EndEmitMethod();
		}

		private void EndEmitMethod()
		{
			var emit = _context.MethodBuilder.Emitter;

			// Prepare return.
			//
			emit.MarkLabel(_context.ReturnLabel);

			if (_context.ReturnValue != null)
				emit.ldloc(_context.ReturnValue);

			emit.ret();

			// Cleanup the context.
			//
			_context.ReturnValue   = null;
			_context.CurrentMethod = null;
			_context.MethodBuilder = null;
		}

		private static AbstractTypeBuilderList GetBuilders(object[] attributes, object target)
		{
			var builders = new AbstractTypeBuilderList(attributes.Length);

			foreach (AbstractTypeBuilderAttribute attr in attributes)
			{
				var builder = attr.TypeBuilder;

				builder.TargetElement = target;
				builders.Add(builder);
			}

			return builders;
		}

		private static AbstractTypeBuilderList GetBuilders(MemberInfo memberInfo)
		{
			return GetBuilders(
				memberInfo.GetCustomAttributes(typeof(AbstractTypeBuilderAttribute), true), memberInfo);
		}

		private static AbstractTypeBuilderList GetBuilders(ParameterInfo parameterInfo)
		{
			return GetBuilders(
				parameterInfo.GetCustomAttributes(typeof(AbstractTypeBuilderAttribute), true), parameterInfo);
		}

		private static AbstractTypeBuilderList GetBuilders(ParameterInfo[] parameters)
		{
			var builders = new AbstractTypeBuilderList();

			foreach (var pi in parameters)
			{
				var attributes = pi.GetCustomAttributes(typeof(AbstractTypeBuilderAttribute), true);

				foreach (AbstractTypeBuilderAttribute attr in attributes)
				{
					var builder = attr.TypeBuilder;

					builder.TargetElement = pi;
					builders.Add(builder);
				}
			}

			return builders;
		}

		private static AbstractTypeBuilderList Combine(params AbstractTypeBuilderList[] builders)
		{
			var list = new AbstractTypeBuilderList();

			foreach (var l in builders)
				list.AddRange(l);

			return list;
		}

		private bool IsApplied(IAbstractTypeBuilder builder, AbstractTypeBuilderList builders, BuildStep buildStep)
		{
			_context.Step = buildStep;
			return builder.IsApplied(_context, builders);
		}

		private bool IsApplied(BuildElement element, AbstractTypeBuilderList builders)
		{
			_context.BuildElement = element;

			foreach (var builder in builders)
			{
				if (IsApplied(builder, builders, BuildStep.Before))  return true;
				if (IsApplied(builder, builders, BuildStep.Build))   return true;
				if (IsApplied(builder, builders, BuildStep.After))   return true;
				if (IsApplied(builder, builders, BuildStep.Catch))   return true;
				if (IsApplied(builder, builders, BuildStep.Finally)) return true;
			}

			return false;
		}

		private static void GetAbstractProperties(Type type, List<PropertyInfo> props)
		{
			if (props.FirstOrDefault(mi => mi.DeclaringType == type) == null)
			{
				props.AddRange(
					type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

				if (type.IsInterface)
					foreach (var t in type.GetInterfaces())
						GetAbstractProperties(t, props);
			}
		}

		private void DefineAbstractProperties()
		{
			var props = new List<PropertyInfo>();

			GetAbstractProperties(_context.Type, props);

			foreach (var pi in props)
			{
				_context.CurrentProperty = pi;

				var propertyBuilders = GetBuilders(pi);

				var getter = pi.GetGetMethod(true);
				var setter = pi.GetSetMethod(true);

				if (getter != null && getter.IsAbstract ||
					setter != null && setter.IsAbstract)
				{
					DefineAbstractGetter(pi, getter, propertyBuilders);
					DefineAbstractSetter(pi, setter, propertyBuilders);
				}
			}

			_context.CurrentProperty = null;
		}

		private void DefineAbstractGetter(
			PropertyInfo propertyInfo, MethodInfo getter, AbstractTypeBuilderList propertyBuilders)
		{
			// Getter can be not defined. We will generate it anyway.
			//
			if (getter == null)
#if SILVERLIGHT
				return;
#else
				getter = new FakeGetter(propertyInfo);
#endif

			var builders = Combine(
				GetBuilders(getter.GetParameters()),
				GetBuilders(getter.ReturnParameter),
				GetBuilders(getter),
				propertyBuilders,
				_builders);

			EmitMethod(builders, getter, BuildElement.AbstractGetter);
		}

		private void DefineAbstractSetter(
			PropertyInfo            propertyInfo,
			MethodInfo              setter,
			AbstractTypeBuilderList propertyBuilders)
		{
			// Setter can be not defined. We will generate it anyway.
			//
			if (setter == null)
#if SILVERLIGHT
				return;
#else
				setter = new FakeSetter(propertyInfo);
#endif

			var builders = Combine(
				GetBuilders(setter.GetParameters()),
				GetBuilders(setter.ReturnParameter),
				GetBuilders(setter),
				propertyBuilders,
				_builders);

			EmitMethod(builders, setter, BuildElement.AbstractSetter);
		}

		private static void GetAbstractMethods(Type type, List<MethodInfo> methods)
		{
			if (!methods.Exists(mi => mi.DeclaringType == type))
			{
				methods.AddRange(
					type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

				if (type.IsInterface)
					foreach (var t in type.GetInterfaces())
						GetAbstractMethods(t, methods);
			}
		}

		private void DefineAbstractMethods()
		{
			var methods = new List<MethodInfo>();

			GetAbstractMethods(_context.Type, methods);

			foreach (var method in methods)
			{
				if (method.IsAbstract && (method.Attributes & MethodAttributes.SpecialName) == 0)
				{
					var builders = Combine(
						GetBuilders(method.GetParameters()),
						GetBuilders(method.ReturnParameter),
						GetBuilders(method),
						_builders);

					EmitMethod(builders, method, BuildElement.AbstractMethod);
				}
			}
		}

		private void OverrideVirtualProperties()
		{
			var props = _context.Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var pi in props)
			{
				_context.CurrentProperty = pi;

				var propertyBuilders = GetBuilders(pi);

				var getter = pi.GetGetMethod(true);

				if (getter != null && getter.IsVirtual && !getter.IsAbstract && !getter.IsFinal)
					OverrideGetter(getter, propertyBuilders);

				var setter = pi.GetSetMethod(true);

				if (setter != null && setter.IsVirtual && !setter.IsAbstract && !setter.IsFinal)
					OverrideSetter(setter, propertyBuilders);
			}

			_context.CurrentProperty = null;
		}

		private void OverrideGetter(MethodInfo getter, AbstractTypeBuilderList propertyBuilders)
		{
			var builders = Combine(
				GetBuilders(getter.GetParameters()),
				GetBuilders(getter.ReturnParameter),
				GetBuilders(getter),
				propertyBuilders,
				_builders);

			if (IsApplied(BuildElement.VirtualGetter, builders))
				EmitMethod(builders, getter, BuildElement.VirtualGetter);
		}

		private void OverrideSetter(MethodInfo setter, AbstractTypeBuilderList propertyBuilders)
		{
			var builders = Combine(
				GetBuilders(setter.GetParameters()),
				GetBuilders(setter.ReturnParameter),
				GetBuilders(setter),
				propertyBuilders,
				_builders);

			if (IsApplied(BuildElement.VirtualSetter, builders))
				EmitMethod(builders, setter, BuildElement.VirtualSetter);
		}

		private void OverrideVirtualMethods()
		{
			var methods = _context.Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var method in methods)
			{
				if (method.IsVirtual &&
					method.IsAbstract == false &&
					method.IsFinal    == false &&
					(method.Attributes & MethodAttributes.SpecialName) == 0 &&
					method.DeclaringType != typeof(object))
				{
					var builders = Combine(
						GetBuilders(method.GetParameters()),
						GetBuilders(method.ReturnParameter),
						GetBuilders(method),
						_builders);

					if (IsApplied(BuildElement.VirtualMethod, builders))
						EmitMethod(builders, method, BuildElement.VirtualMethod);
				}
			}
		}

		private void DefineInterfaces()
		{
			foreach (var de in _context.InterfaceMap)
			{
				_context.CurrentInterface = de.Key;

				var interfaceMethods = _context.CurrentInterface.GetMethods();

				foreach (var m in interfaceMethods)
				{
					if (_context.TypeBuilder.OverriddenMethods.ContainsKey(m))
						continue;

					BeginEmitMethod(m);

					// Call builder to build the method.
					//
					var builder = de.Value;

					if (builder != null)
					{
						builder.ID = ++_idCounter;

						_context.BuildElement = BuildElement.InterfaceMethod;
						_context.Step         = BuildStep.Build;
						builder.Build(_context);
					}

					EndEmitMethod();
				}

				_context.CurrentInterface = null;
			}
		}
	}
}
