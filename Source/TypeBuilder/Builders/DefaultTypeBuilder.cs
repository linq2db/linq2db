using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace LinqToDB.TypeBuilder.Builders
{
	using Properties;
	using Reflection;
	using Reflection.Emit;

	public class DefaultTypeBuilder : AbstractTypeBuilderBase
	{
		#region Interface Overrides

		public override bool IsCompatible(BuildContext context, IAbstractTypeBuilder typeBuilder)
		{
			return IsRelative(typeBuilder) == false;
		}

		public override bool IsApplied(BuildContext context, AbstractTypeBuilderList builders)
		{
			if (context == null) throw new ArgumentNullException("context");

			if (context.IsAbstractProperty && context.IsBeforeOrBuildStep)
			{
				return context.CurrentProperty.GetIndexParameters().Length <= 1;
			}

			return context.BuildElement == BuildElement.Type && context.IsAfterStep;
		}

		#endregion

		#region Get/Set Implementation

		protected override void BuildAbstractGetter()
		{
			var field = GetField();
			var index = Context.CurrentProperty.GetIndexParameters();

			switch (index.Length)
			{
				case 0:
					Context.MethodBuilder.Emitter
						.ldarg_0
						.ldfld   (field)
						.stloc   (Context.ReturnValue)
						;
					break;

				case 1:
					Context.MethodBuilder.Emitter
						.ldarg_0
						.ldfld          (field)
						.ldarg_1
						.boxIfValueType (index[0].ParameterType)
						.callvirt       (typeof(Dictionary<object,object>), "get_Item", typeof(object))
						.castType       (Context.CurrentProperty.PropertyType)
						.stloc          (Context.ReturnValue)
						;
					break;
			}
		}

		protected override void BuildAbstractSetter()
		{
			var field = GetField();
			var index = Context.CurrentProperty.GetIndexParameters();

			switch (index.Length)
			{
				case 0:
					Context.MethodBuilder.Emitter
						.ldarg_0
						.ldarg_1
						.stfld   (field)
						;
					//Context.MethodBuilder.Emitter.AddMaxStackSize(6);
					break;

				case 1:
					Context.MethodBuilder.Emitter
						.ldarg_0
						.ldfld          (field)
						.ldarg_1
						.boxIfValueType (index[0].ParameterType)
						.ldarg_2
						.boxIfValueType (Context.CurrentProperty.PropertyType)
						.callvirt       (typeof(Dictionary<object,object>), "set_Item", typeof(object), typeof(object))
					;
					break;
			}
		}

		#endregion

		#region Call Base Method

		protected override void BuildVirtualGetter()
		{
			CallBaseMethod();
		}

		protected override void BuildVirtualSetter()
		{
			CallBaseMethod();
		}

		protected override void BuildVirtualMethod()
		{
			CallBaseMethod();
		}

		private void CallBaseMethod()
		{
			var emit   = Context.MethodBuilder.Emitter;
			var method = Context.MethodBuilder.OverriddenMethod;
			var ps     = method.GetParameters();

			emit.ldarg_0.end();

			for (var i = 0; i < ps.Length; i++)
				emit.ldarg(i + 1);

			emit.call(method);

			if (Context.ReturnValue != null)
				emit.stloc(Context.ReturnValue);
		}

		#endregion

		#region Properties

		private   static TypeHelper _initContextType;
		protected static TypeHelper  InitContextType
		{
			get { return _initContextType ?? (_initContextType = new TypeHelper(typeof(InitContext))); }
		}

		#endregion

		#region Field Initialization

		#region Overrides

		protected override void BeforeBuildAbstractGetter()
		{
			CallLazyInstanceInsurer(GetField());
		}

		protected override void BeforeBuildAbstractSetter()
		{
			var field = GetField();

			if (field.FieldType != Context.CurrentProperty.PropertyType)
				CallLazyInstanceInsurer(field);
		}

		#endregion

		#region Common

		protected FieldBuilder GetField()
		{
			var propertyInfo = Context.CurrentProperty;
			var fieldName    = GetFieldName();
			var fieldType    = GetFieldType();
			var field        = Context.GetField(fieldName);

			if (field == null)
			{
				field = Context.CreatePrivateField(propertyInfo, fieldName, fieldType);

				if (TypeAccessor.IsInstanceBuildable(fieldType))
				{
					var noInstance = propertyInfo.GetCustomAttributes(typeof(NoInstanceAttribute), true).Length > 0;

					if (IsObjectHolder && noInstance)
					{
						BuildHolderInstance(Context.TypeBuilder.DefaultConstructor.Emitter);
						BuildHolderInstance(Context.TypeBuilder.InitConstructor.Emitter);
					}
					else if (!noInstance)
					{
						if (fieldType.IsClass && IsLazyInstance(fieldType))
						{
							BuildLazyInstanceEnsurer();
						}
						else
						{
							BuildDefaultInstance();
							BuildInitContextInstance();
						}
					}
				}
			}

			return field;
		}

		#endregion

		#region Build

		private void BuildHolderInstance(EmitHelper emit)
		{
			var fieldName  = GetFieldName();
			var field      = Context.GetField(fieldName);
			var fieldType  = new TypeHelper(field.FieldType);
			var objectType = new TypeHelper(GetObjectType());

			var ci = fieldType.GetPublicDefaultConstructor();

			if (ci != null)
			{
				emit
					.ldarg_0
					.newobj (ci)
					.stfld  (field)
					;
			}
			else
			{
				if (!CheckObjectHolderCtor(fieldType, objectType))
					return;

				emit
					.ldarg_0
					.ldnull
					.newobj (fieldType, objectType)
					.stfld  (field)
					;
			}
		}

		private void CreateDefaultInstance(
			FieldBuilder field, TypeHelper fieldType, TypeHelper objectType, EmitHelper emit)
		{
			if (!CheckObjectHolderCtor(fieldType, objectType))
				return;

			if (objectType.Type == typeof(string))
			{
				emit
					.ldarg_0
					.LoadInitValue (objectType)
					;
			}
			else if (objectType.IsArray)
			{
				var initializer = GetArrayInitializer(objectType);

				emit
					.ldarg_0
					.ldsfld  (initializer)
					;
			}
			else
			{
				var ci = objectType.GetPublicDefaultConstructor();

				if (ci == null)
				{
					if (objectType.Type.IsValueType)
						return;

					var message = string.Format(
						Resources.TypeBuilder_PropertyTypeHasNoPublicDefaultCtor,
						Context.CurrentProperty.Name,
						Context.Type.FullName,
						objectType.FullName);

					emit
						.ldstr  (message)
						.newobj (typeof(TypeBuilderException), typeof(string))
						.@throw
						.end()
						;

					return;
				}

				emit
					.ldarg_0
					.newobj  (ci)
					;
			}

			if (IsObjectHolder)
			{
				emit
					.newobj (fieldType, objectType)
					;
			}

			emit
				.stfld (field)
				;
		}

		private void CreateParametrizedInstance(
			FieldBuilder field, TypeHelper fieldType, TypeHelper objectType, EmitHelper emit, object[] parameters)
		{
			if (!CheckObjectHolderCtor(fieldType, objectType))
				return;

			Stack<ConstructorInfo> genericNestedConstructors;
			if (parameters.Length == 1)
			{
				var o = parameters[0];

				if (o == null)
				{
					if (objectType.IsValueType == false)
						emit
							.ldarg_0
							.ldnull
							.end()
							;

					if (IsObjectHolder)
					{
						emit
							.newobj (fieldType, objectType)
							;
					}
					else
					{
						if (objectType.Type.IsGenericType)
						{
							Type nullableType = null;
							genericNestedConstructors = GetGenericNestedConstructors(
								objectType,
								typeHelper =>
									typeHelper.IsValueType == false ||
									(typeHelper.Type.IsGenericType && typeHelper.Type.GetGenericTypeDefinition() == typeof (Nullable<>)),
								typeHelper => { nullableType = typeHelper.Type; },
								() => nullableType != null);

							if (nullableType == null)
								throw new Exception("Cannot find nullable type in generic types chain");

							if (nullableType.IsValueType == false)
							{
								emit
									.ldarg_0
									.ldnull
									.end()
									;
							}
							else
							{
								var nullable = emit.DeclareLocal(nullableType);

								emit
									.ldloca(nullable)
									.initobj(nullableType)
									.ldarg_0
									.ldloc(nullable)
									;

								if (genericNestedConstructors != null)
								{
									while (genericNestedConstructors.Count != 0)
									{
										emit
											.newobj(genericNestedConstructors.Pop())
											;
									}
								}
							}
						}
					}

					emit
						.stfld (field)
						;

					return;
				}
				
				if (objectType.Type == o.GetType())
				{
					if (objectType.Type == typeof(string))
					{
						emit
							.ldarg_0
							.ldstr   ((string)o)
							.stfld   (field)
							;

						return;
					}

					if (objectType.IsValueType)
					{
						emit.ldarg_0.end();

						if (emit.LoadWellKnownValue(o) == false)
						{
							emit
								.ldsfld     (GetParameterField())
								.ldc_i4_0
								.ldelem_ref
								.end()
								;
						}

						emit.stfld(field);

						return;
					}
				}
			}

			var types = new Type[parameters.Length];

			for (var i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] != null)
				{
					var t = parameters[i].GetType();

					types[i] = (t.IsEnum) ? Enum.GetUnderlyingType(t) : t;
				}
				else
					types[i] = typeof(object);
			}

			// Do some heuristics for Nullable<DateTime> and EditableValue<Decimal>
			//
			ConstructorInfo objectCtor = null;
			genericNestedConstructors = GetGenericNestedConstructors(
				objectType,
				typeHelper => true,
				typeHelper => { objectCtor = typeHelper.GetPublicConstructor(types); },
				() => objectCtor != null);

			if (objectCtor == null)
			{
				if (objectType.IsValueType)
					return;

				throw new TypeBuilderException(
					string.Format(types.Length == 0?
							Resources.TypeBuilder_PropertyTypeHasNoPublicDefaultCtor:
							Resources.TypeBuilder_PropertyTypeHasNoPublicCtor,
						Context.CurrentProperty.Name,
						Context.Type.FullName,
						objectType.FullName));
			}

			var pi = objectCtor.GetParameters();

			emit.ldarg_0.end();

			for (var i = 0; i < parameters.Length; i++)
			{
				var o     = parameters[i];
				var oType = o.GetType();

				if (emit.LoadWellKnownValue(o))
				{
					if (oType.IsValueType)
					{
						if (!pi[i].ParameterType.IsValueType)
							emit.box(oType);
						else if (Type.GetTypeCode(oType) != Type.GetTypeCode(pi[i].ParameterType))
							emit.conv(pi[i].ParameterType);
					}
				}
				else
				{
					emit
						.ldsfld         (GetParameterField())
						.ldc_i4         (i)
						.ldelem_ref
						.CastFromObject (types[i])
						;

					if (oType.IsValueType && !pi[i].ParameterType.IsValueType)
						emit.box(oType);
				}
			}

			emit
				.newobj (objectCtor)
				;

			if (genericNestedConstructors != null)
			{
				while (genericNestedConstructors.Count != 0)
				{
					emit
						.newobj(genericNestedConstructors.Pop())
						;
				}
			}

			if (IsObjectHolder)
			{
				emit
					.newobj (fieldType, objectType)
					;
			}

			emit
				.stfld  (field)
				;
		}

		private Stack<ConstructorInfo> GetGenericNestedConstructors(TypeHelper objectType, 
			Predicate<TypeHelper> isActionable, 
			Action<TypeHelper> action, 
			Func<bool> isBreakCondition)
		{
			Stack<ConstructorInfo> genericNestedConstructors = null;

			if (isActionable(objectType))
				action(objectType);

			while (objectType.Type.IsGenericType && !isBreakCondition())
			{
				var typeArgs = objectType.Type.GetGenericArguments();

				if (typeArgs.Length == 1)
				{
					var genericCtor = objectType.GetPublicConstructor(typeArgs[0]);

					if (genericCtor != null)
					{
						if (genericNestedConstructors == null)
							genericNestedConstructors = new Stack<ConstructorInfo>();

						genericNestedConstructors.Push(genericCtor);
						objectType = typeArgs[0];

						if (isActionable(objectType))
							action(objectType);
					}
				}
				else
				{
					throw new TypeBuilderException(
						string.Format(Resources.TypeBuilder_GenericShouldBeSingleTyped,
							Context.CurrentProperty.Name,
							Context.Type.FullName,
							objectType.FullName));
				}
			}

			return genericNestedConstructors;
		}

		#endregion

		#region Build InitContext Instance

		private void BuildInitContextInstance()
		{
			var fieldName  = GetFieldName();
			var field      = Context.GetField(fieldName);
			var fieldType  = new TypeHelper(field.FieldType);
			var objectType = new TypeHelper(GetObjectType());

			var emit = Context.TypeBuilder.InitConstructor.Emitter;

			var parameters = TypeHelper.GetPropertyParameters(Context.CurrentProperty);
			var ci = objectType.GetPublicConstructor(typeof(InitContext));

			if (ci != null && ci.GetParameters()[0].ParameterType != typeof(InitContext))
				ci = null;

			if (ci != null || objectType.IsAbstract)
				CreateAbstractInitContextInstance(field, fieldType, objectType, emit, parameters);
			else if (parameters == null)
				CreateDefaultInstance(field, fieldType, objectType, emit);
			else
				CreateParametrizedInstance(field, fieldType, objectType, emit, parameters);
		}

		private void CreateAbstractInitContextInstance(
			FieldBuilder field, TypeHelper fieldType, TypeHelper objectType, EmitHelper emit, object[] parameters)
		{
			if (!CheckObjectHolderCtor(fieldType, objectType))
				return;

			var memberParams = InitContextType.GetProperty("MemberParameters").GetSetMethod();
			var parentField  = Context.GetItem<LocalBuilder>("$LinqToDB.InitContext.Parent");

			if (parentField == null)
			{
				Context.Items.Add("$LinqToDB.InitContext.Parent", parentField = emit.DeclareLocal(typeof(object)));

				var label = emit.DefineLabel();

				emit
					.ldarg_1
					.brtrue_s  (label)

					.newobj    (InitContextType.GetPublicDefaultConstructor())
					.starg     (1)

					.ldarg_1
					.ldc_i4_1
					.callvirt  (InitContextType.GetProperty("IsInternal").GetSetMethod())

					.MarkLabel (label)

					.ldarg_1
					.callvirt  (InitContextType.GetProperty("Parent").GetGetMethod())
					.stloc     (parentField)
					;
			}

			emit
				.ldarg_1
				.ldarg_0
				.callvirt (InitContextType.GetProperty("Parent").GetSetMethod())
				;

			var isDirty = Context.GetItem<bool?>("$LinqToDB.InitContext.DirtyParameters");

			if (parameters != null)
			{
				emit
					.ldarg_1
					.ldsfld   (GetParameterField())
					.callvirt (memberParams)
					;
			}
			else if (isDirty != null && (bool)isDirty)
			{
				emit
					.ldarg_1
					.ldnull
					.callvirt (memberParams)
					;
			}

			if (Context.Items.ContainsKey("$LinqToDB.InitContext.DirtyParameters"))
				Context.Items["$LinqToDB.InitContext.DirtyParameters"] = (bool?)(parameters != null);
			else
				Context.Items.Add("$LinqToDB.InitContext.DirtyParameters", (bool?)(parameters != null));

			if (objectType.IsAbstract)
			{
				emit
					.ldarg_0
					.ldsfld             (GetTypeAccessorField())
					.ldarg_1
					.callvirtNoGenerics (typeof(TypeAccessor), "CreateInstanceEx", _initContextType)
					.isinst             (objectType)
					;
			}
			else
			{
				emit
					.ldarg_0
					.ldarg_1
					.newobj  (objectType.GetPublicConstructor(typeof(InitContext)))
					;
			}

			if (IsObjectHolder)
			{
				emit
					.newobj (fieldType, objectType)
					;
			}

			emit
				.stfld (field)
				;
		}

		#endregion

		#region Build Default Instance

		private void BuildDefaultInstance()
		{
			var fieldName  = GetFieldName();
			var field      = Context.GetField(fieldName);
			var fieldType  = new TypeHelper(field.FieldType);
			var objectType = new TypeHelper(GetObjectType());

			var emit = Context.TypeBuilder.DefaultConstructor.Emitter;
			var ps   = TypeHelper.GetPropertyParameters(Context.CurrentProperty);
			var ci   = objectType.GetPublicConstructor(typeof(InitContext));

			if (ci != null && ci.GetParameters()[0].ParameterType != typeof(InitContext))
				ci = null;

			if (ci != null || objectType.IsAbstract)
				CreateInitContextDefaultInstance(
					"$LinqToDB.DefaultInitContext.", field, fieldType, objectType, emit, ps);
			else if (ps == null)
				CreateDefaultInstance(field, fieldType, objectType, emit);
			else
				CreateParametrizedInstance(field, fieldType, objectType, emit, ps);
		}

		private bool CheckObjectHolderCtor(TypeHelper fieldType, TypeHelper objectType)
		{
			if (IsObjectHolder)
			{
				var holderCi = fieldType.GetPublicConstructor(objectType);

				if (holderCi == null)
				{
					var message = string.Format(
						Resources.TypeBuilder_PropertyTypeHasNoCtorWithParamType,
						Context.CurrentProperty.Name,
						Context.Type.FullName,
						fieldType.FullName,
						objectType.FullName);

					Context.TypeBuilder.DefaultConstructor.Emitter
						.ldstr  (message)
						.newobj (typeof(TypeBuilderException), typeof(string))
						.@throw
						.end()
						;

					return false;
				}
			}

			return true;
		}

		private void CreateInitContextDefaultInstance(
			string       initContextName,
			FieldBuilder field,
			TypeHelper   fieldType,
			TypeHelper   objectType,
			EmitHelper   emit,
			object[]     parameters)
		{
			if (!CheckObjectHolderCtor(fieldType, objectType))
				return;

			var initField    = GetInitContextBuilder(initContextName, emit);
			var memberParams = InitContextType.GetProperty("MemberParameters").GetSetMethod();

			if (parameters != null)
			{
				emit
					.ldloc    (initField)
					.ldsfld   (GetParameterField())
					.callvirt (memberParams)
					;
			}
			else if ((bool)Context.Items["$LinqToDB.Default.DirtyParameters"])
			{
				emit
					.ldloc    (initField)
					.ldnull
					.callvirt (memberParams)
					;
			}

			Context.Items["$LinqToDB.Default.DirtyParameters"] = parameters != null;

			if (objectType.IsAbstract)
			{
				emit
					.ldarg_0
					.ldsfld             (GetTypeAccessorField())
					.ldloc              (initField)
					.callvirtNoGenerics (typeof(TypeAccessor), "CreateInstanceEx", _initContextType)
					.isinst             (objectType)
					;
			}
			else
			{
				emit
					.ldarg_0
					.ldloc   (initField)
					.newobj  (objectType.GetPublicConstructor(typeof(InitContext)))
					;
			}

			if (IsObjectHolder)
			{
				emit
					.newobj (fieldType, objectType)
					;
			}

			emit
				.stfld (field)
				;
		}

		private LocalBuilder GetInitContextBuilder(
			string initContextName, EmitHelper emit)
		{
			var initField = Context.GetItem<LocalBuilder>(initContextName);

			if (initField == null)
			{
				Context.Items.Add(initContextName, initField = emit.DeclareLocal(InitContextType));

				emit
					.newobj   (InitContextType.GetPublicDefaultConstructor())

					.dup
					.ldarg_0
					.callvirt (InitContextType.GetProperty("Parent").GetSetMethod())

					.dup
					.ldc_i4_1
					.callvirt (InitContextType.GetProperty("IsInternal").GetSetMethod())

					.stloc    (initField)
					;

				Context.Items.Add("$LinqToDB.Default.DirtyParameters", false);
			}

			return initField;
		}

		#endregion

		#region Build Lazy Instance

		private bool IsLazyInstance(Type type)
		{
			var attrs = Context.CurrentProperty.GetCustomAttributes(typeof(LazyInstanceAttribute), true);

			if (attrs.Length > 0)
				return ((LazyInstanceAttribute)attrs[0]).IsLazy;

			attrs = Context.Type.GetAttributes(typeof(LazyInstancesAttribute));

			foreach (LazyInstancesAttribute a in attrs)
				if (a.Type == typeof(object) || type == a.Type || type.IsSubclassOf(a.Type))
					return a.IsLazy;

			return false;
		}

		private void BuildLazyInstanceEnsurer()
		{
			var fieldName  = GetFieldName();
			var field      = Context.GetField(fieldName);
			var fieldType  = new TypeHelper(field.FieldType);
			var objectType = new TypeHelper(GetObjectType());
			var ensurer    = Context.TypeBuilder.DefineMethod(
				string.Format("$EnsureInstance{0}", fieldName),
				MethodAttributes.Private | MethodAttributes.HideBySig);

			var emit = ensurer.Emitter;
			var end  = emit.DefineLabel();

			emit
				.ldarg_0
				.ldfld    (field)
				.brtrue_s (end)
				;

			var parameters = TypeHelper.GetPropertyParameters(Context.CurrentProperty);
			var ci         = objectType.GetPublicConstructor(typeof(InitContext));

			if (ci != null || objectType.IsAbstract)
				CreateInitContextLazyInstance(field, fieldType, objectType, emit, parameters);
			else if (parameters == null)
				CreateDefaultInstance(field, fieldType, objectType, emit);
			else
				CreateParametrizedInstance(field, fieldType, objectType, emit, parameters);

			emit
				.MarkLabel(end)
				.ret()
				;

			Context.Items.Add("$LinqToDB.FieldInstanceEnsurer." + fieldName, ensurer);
		}

		private void CreateInitContextLazyInstance(
			FieldBuilder field,
			TypeHelper   fieldType,
			TypeHelper   objectType,
			EmitHelper   emit,
			object[]     parameters)
		{
			if (!CheckObjectHolderCtor(fieldType, objectType))
				return;

			var initField = emit.DeclareLocal(InitContextType);

			emit
				.newobj   (InitContextType.GetPublicDefaultConstructor())

				.dup
				.ldarg_0
				.callvirt (InitContextType.GetProperty("Parent").GetSetMethod())

				.dup
				.ldc_i4_1
				.callvirt (InitContextType.GetProperty("IsInternal").GetSetMethod())

				.dup
				.ldc_i4_1
				.callvirt (InitContextType.GetProperty("IsLazyInstance").GetSetMethod())

				;

			if (parameters != null)
			{
				emit
					.dup
					.ldsfld   (GetParameterField())
					.callvirt (InitContextType.GetProperty("MemberParameters").GetSetMethod())
					;
			}

			emit
				.stloc    (initField);

			if (objectType.IsAbstract)
			{
				emit
					.ldarg_0
					.ldsfld             (GetTypeAccessorField())
					.ldloc              (initField)
					.callvirtNoGenerics (typeof(TypeAccessor), "CreateInstanceEx", _initContextType)
					.isinst             (objectType)
					;
			}
			else
			{
				emit
					.ldarg_0
					.ldloc   (initField)
					.newobj  (objectType.GetPublicConstructor(typeof(InitContext)))
					;
			}

			if (IsObjectHolder)
			{
				emit
					.newobj (fieldType, objectType)
					;
			}

			emit
				.stfld (field)
				;
		}

		#endregion

		#region Finalize Type

		protected override void AfterBuildType()
		{
			var isDirty = Context.GetItem<bool?>("$LinqToDB.InitContext.DirtyParameters");

			if (isDirty != null && isDirty.Value)
			{
				Context.TypeBuilder.InitConstructor.Emitter
					.ldarg_1
					.ldnull
					.callvirt (InitContextType.GetProperty("MemberParameters").GetSetMethod())
					;
			}

			var localBuilder = Context.GetItem<LocalBuilder>("$LinqToDB.InitContext.Parent");

			if (localBuilder != null)
			{
				Context.TypeBuilder.InitConstructor.Emitter
					.ldarg_1
					.ldloc    (localBuilder)
					.callvirt (InitContextType.GetProperty("Parent").GetSetMethod())
					;
			}

			FinalizeDefaultConstructors();
			FinalizeInitContextConstructors();
		}

		private void FinalizeDefaultConstructors()
		{
			var ci = Context.Type.GetDefaultConstructor();

			if (ci == null || Context.TypeBuilder.IsDefaultConstructorDefined)
			{
				var emit = Context.TypeBuilder.DefaultConstructor.Emitter;

				if (ci != null)
				{
					emit.ldarg_0.call(ci);
				}
				else
				{
					ci = Context.Type.GetConstructor(typeof(InitContext));

					if (ci != null)
					{
						var initField = GetInitContextBuilder("$LinqToDB.DefaultInitContext.", emit);

						emit
							.ldarg_0
							.ldloc   (initField)
							.call    (ci);
					}
					else
					{
						if (Context.Type.GetConstructors().Length > 0)
							throw new TypeBuilderException(string.Format(
								Resources.TypeBuilder_NoDefaultCtor,
								Context.Type.FullName));
					}
				}
			}
		}

		private void FinalizeInitContextConstructors()
		{
			var ci = Context.Type.GetConstructor(typeof(InitContext));

			if (ci != null || Context.TypeBuilder.IsInitConstructorDefined)
			{
				var emit = Context.TypeBuilder.InitConstructor.Emitter;

				if (ci != null)
				{
					emit
						.ldarg_0
						.ldarg_1
						.call    (ci);
				}
				else
				{
					ci = Context.Type.GetDefaultConstructor();

					if (ci != null)
					{
						emit.ldarg_0.call(ci);
					}
					else
					{
						if (Context.Type.GetConstructors().Length > 0)
							throw new TypeBuilderException(
								string.Format(Resources.TypeBuilder_NoDefaultCtor,
									Context.Type.FullName));
					}
				}
			}
		}

		#endregion

		#endregion
	}
}
