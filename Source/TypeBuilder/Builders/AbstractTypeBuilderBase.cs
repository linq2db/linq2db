using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

using LinqToDB.Reflection;
using LinqToDB.Reflection.Emit;

namespace LinqToDB.TypeBuilder.Builders
{
	public abstract class AbstractTypeBuilderBase : IAbstractTypeBuilder
	{
		public virtual Type[] GetInterfaces()
		{
			return null;
		}

		private int _id;
		public  int  ID
		{
			get { return _id;  }
			set { _id = value; }
		}

		private object _targetElement;
		public  object  TargetElement
		{
			get { return _targetElement;  }
			set { _targetElement = value; }
		}

		private BuildContext _context;
		public  BuildContext  Context
		{
			[DebuggerStepThrough] get { return _context;  }
			[DebuggerStepThrough] set { _context = value; }
		}

		public virtual bool IsCompatible(BuildContext context, IAbstractTypeBuilder typeBuilder)
		{
			return true;
		}

		protected bool IsRelative(IAbstractTypeBuilder typeBuilder)
		{
			if (typeBuilder == null) throw new ArgumentNullException("typeBuilder");

			return GetType().IsInstanceOfType(typeBuilder) || typeBuilder.GetType().IsInstanceOfType(this);
		}

		public virtual bool IsApplied(BuildContext context, AbstractTypeBuilderList builders)
		{
			return false;
		}

		public virtual int GetPriority(BuildContext context)
		{
			return TypeBuilderConsts.Priority.Normal;
		}

		public virtual void Build(BuildContext context)
		{
			if (context == null) throw new ArgumentNullException("context");

			Context = context;

			switch (context.Step)
			{
				case BuildStep.Begin: BeginMethodBuild(); return;
				case BuildStep.End:   EndMethodBuild();   return;
			}

			switch (context.BuildElement)
			{
				case BuildElement.Type:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildType(); break;
						case BuildStep.Build:          BuildType(); break;
						case BuildStep.After:     AfterBuildType(); break;
						case BuildStep.Catch:     CatchBuildType(); break;
						case BuildStep.Finally: FinallyBuildType(); break;
					}

					break;

				case BuildElement.AbstractGetter:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildAbstractGetter(); break;
						case BuildStep.Build:          BuildAbstractGetter(); break;
						case BuildStep.After:     AfterBuildAbstractGetter(); break;
						case BuildStep.Catch:     CatchBuildAbstractGetter(); break;
						case BuildStep.Finally: FinallyBuildAbstractGetter(); break;
					}

					break;

				case BuildElement.AbstractSetter:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildAbstractSetter(); break;
						case BuildStep.Build:          BuildAbstractSetter(); break;
						case BuildStep.After:     AfterBuildAbstractSetter(); break;
						case BuildStep.Catch:     CatchBuildAbstractSetter(); break;
						case BuildStep.Finally: FinallyBuildAbstractSetter(); break;
					}

					break;

				case BuildElement.AbstractMethod:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildAbstractMethod(); break;
						case BuildStep.Build:          BuildAbstractMethod(); break;
						case BuildStep.After:     AfterBuildAbstractMethod(); break;
						case BuildStep.Catch:     CatchBuildAbstractMethod(); break;
						case BuildStep.Finally: FinallyBuildAbstractMethod(); break;
					}

					break;

				case BuildElement.VirtualGetter:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildVirtualGetter(); break;
						case BuildStep.Build:          BuildVirtualGetter(); break;
						case BuildStep.After:     AfterBuildVirtualGetter(); break;
						case BuildStep.Catch:     CatchBuildVirtualGetter(); break;
						case BuildStep.Finally: FinallyBuildVirtualGetter(); break;
					}

					break;

				case BuildElement.VirtualSetter:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildVirtualSetter(); break;
						case BuildStep.Build:          BuildVirtualSetter(); break;
						case BuildStep.After:     AfterBuildVirtualSetter(); break;
						case BuildStep.Catch:     CatchBuildVirtualSetter(); break;
						case BuildStep.Finally: FinallyBuildVirtualSetter(); break;
					}

					break;

				case BuildElement.VirtualMethod:
					switch (context.Step)
					{
						case BuildStep.Before:   BeforeBuildVirtualMethod(); break;
						case BuildStep.Build:          BuildVirtualMethod(); break;
						case BuildStep.After:     AfterBuildVirtualMethod(); break;
						case BuildStep.Catch:     CatchBuildVirtualMethod(); break;
						case BuildStep.Finally: FinallyBuildVirtualMethod(); break;
					}

					break;

				case BuildElement.InterfaceMethod:
					BuildInterfaceMethod();
					break;
			}
		}

		protected virtual void  BeforeBuildType          () {}
		protected virtual void        BuildType          () {}
		protected virtual void   AfterBuildType          () {}
		protected virtual void   CatchBuildType          () {}
		protected virtual void FinallyBuildType          () {}

		protected virtual void  BeforeBuildAbstractGetter() {}
		protected virtual void        BuildAbstractGetter() {}
		protected virtual void   AfterBuildAbstractGetter() {}
		protected virtual void   CatchBuildAbstractGetter() {}
		protected virtual void FinallyBuildAbstractGetter() {}

		protected virtual void  BeforeBuildAbstractSetter() {}
		protected virtual void        BuildAbstractSetter() {}
		protected virtual void   AfterBuildAbstractSetter() {}
		protected virtual void   CatchBuildAbstractSetter() {}
		protected virtual void FinallyBuildAbstractSetter() {}

		protected virtual void  BeforeBuildAbstractMethod() {}
		protected virtual void        BuildAbstractMethod() {}
		protected virtual void   AfterBuildAbstractMethod() {}
		protected virtual void   CatchBuildAbstractMethod() {}
		protected virtual void FinallyBuildAbstractMethod() {}

		protected virtual void  BeforeBuildVirtualGetter () {}
		protected virtual void        BuildVirtualGetter () {}
		protected virtual void   AfterBuildVirtualGetter () {}
		protected virtual void   CatchBuildVirtualGetter () {}
		protected virtual void FinallyBuildVirtualGetter () {}

		protected virtual void  BeforeBuildVirtualSetter () {}
		protected virtual void        BuildVirtualSetter () {}
		protected virtual void   AfterBuildVirtualSetter () {}
		protected virtual void   CatchBuildVirtualSetter () {}
		protected virtual void FinallyBuildVirtualSetter () {}

		protected virtual void  BeforeBuildVirtualMethod () {}
		protected virtual void        BuildVirtualMethod () {}
		protected virtual void   AfterBuildVirtualMethod () {}
		protected virtual void   CatchBuildVirtualMethod () {}
		protected virtual void FinallyBuildVirtualMethod () {}

		protected virtual void BuildInterfaceMethod      () {}

		protected virtual void BeginMethodBuild          () {}
		protected virtual void   EndMethodBuild          () {}

		#region Helpers

		protected bool CallLazyInstanceInsurer(FieldBuilder field)
		{
			if (field == null) throw new ArgumentNullException("field");

			MethodBuilderHelper ensurer = Context.GetFieldInstanceEnsurer(field.Name);

			if (ensurer != null)
			{
				Context.MethodBuilder.Emitter
					.ldarg_0
					.call    (ensurer);
			}

			return ensurer != null;
		}

		protected virtual string GetFieldName(PropertyInfo propertyInfo)
		{
			string name = propertyInfo.Name;

			if (char.IsUpper(name[0]) && name.Length > 1 && char.IsLower(name[1]))
				name = char.ToLower(name[0]) + name.Substring(1, name.Length - 1);

			name = "_" + name;

			foreach (ParameterInfo p in propertyInfo.GetIndexParameters())
				name += "." + p.ParameterType.FullName;//.Replace(".", "_").Replace("+", "_");

			return name;
		}

		protected string GetFieldName()
		{
			return GetFieldName(Context.CurrentProperty);
		}

		protected FieldBuilder GetPropertyInfoField(PropertyInfo property)
		{
			string       fieldName = GetFieldName(property) + "_$propertyInfo";
			FieldBuilder field     = Context.GetField(fieldName);

			if (field == null)
			{
				field = Context.CreatePrivateStaticField(fieldName, typeof(PropertyInfo));

				EmitHelper emit = Context.TypeBuilder.TypeInitializer.Emitter;

				ParameterInfo[] index      = property.GetIndexParameters();

				emit
					.LoadType (Context.Type)
					.ldstr    (property.Name)
					.LoadType (property.PropertyType)
					;

				if (index.Length == 0)
				{
					emit
						.ldsfld (typeof(Type).GetField("EmptyTypes"))
						;
				}
				else
				{
					emit
						.ldc_i4 (index.Length) 
						.newarr (typeof(Type))
						;

					for (int i = 0; i < index.Length; i++)
						emit
							.dup
							.ldc_i4     (i) 
							.LoadType   (index[i].ParameterType)
							.stelem_ref
							.end()
							;
				}

				emit
					.call   (typeof(TypeHelper).GetMethod("GetPropertyInfo"))
					.stsfld (field)
					;
			}

			return field;
		}

		protected FieldBuilder GetPropertyInfoField()
		{
			return GetPropertyInfoField(Context.CurrentProperty);
		}

		protected FieldBuilder GetParameterField()
		{
			string       fieldName = GetFieldName() + "_$parameters";
			FieldBuilder field     = Context.GetField(fieldName);

			if (field == null)
			{
				field = Context.CreatePrivateStaticField(fieldName, typeof(object[]));

				FieldBuilder piField = GetPropertyInfoField();
				EmitHelper   emit    = Context.TypeBuilder.TypeInitializer.Emitter;

				emit
					.ldsfld (piField)
					.call   (typeof(TypeHelper).GetMethod("GetPropertyParameters"))
					.stsfld (field)
					;
			}

			return field;
		}

		protected FieldBuilder GetTypeAccessorField()
		{
			string       fieldName = "_" + GetObjectType().FullName + "_$typeAccessor";
			FieldBuilder field     = Context.GetField(fieldName);

			if (field == null)
			{
				field = Context.CreatePrivateStaticField(fieldName, typeof(TypeAccessor));

				EmitHelper emit = Context.TypeBuilder.TypeInitializer.Emitter;

				emit
					.LoadType (GetObjectType())
					.call     (typeof(TypeAccessor), "GetAccessor", typeof(Type))
					.stsfld   (field)
					;
			}

			return field;
		}

		protected FieldBuilder GetArrayInitializer(Type arrayType)
		{
			string       fieldName = "_array_of_$_" + arrayType.FullName;
			FieldBuilder field     = Context.GetField(fieldName);

			if (field == null)
			{
				field = Context.CreatePrivateStaticField(fieldName, arrayType);

				EmitHelper emit = Context.TypeBuilder.TypeInitializer.Emitter;

				int rank = arrayType.GetArrayRank();

				if (rank > 1)
				{
					Type[] parameters = new Type[rank];

					for (int i = 0; i < parameters.Length; i++)
					{
						parameters[i] = typeof(int);
						emit.ldc_i4_0.end();
					}

					ConstructorInfo ci = TypeHelper.GetConstructor(arrayType, parameters);

					emit
						.newobj (ci)
						.stsfld (field)
						;
				}
				else
				{
					emit
						.ldc_i4_0
						.newarr   (arrayType.GetElementType())
						.stsfld   (field)
						;
				}
			}

			return field;
		}

		protected FieldBuilder GetArrayInitializer()
		{
			return GetArrayInitializer(Context.CurrentProperty.PropertyType);
		}

		protected virtual Type GetFieldType()
		{
			var pi    = Context.CurrentProperty;
			var index = pi.GetIndexParameters();

			switch (index.Length)
			{
				case 0: return pi.PropertyType;
				case 1: return typeof(Dictionary<object,object>);
				default:
					throw new InvalidOperationException();
			}
		}

		protected virtual Type GetObjectType()
		{
			return GetFieldType();
		}

		protected virtual bool IsObjectHolder
		{
			get { return false; }
		}

		#endregion
	}
}
