using System;
using System.Reflection;
using System.Reflection.Emit;
using LinqToDB.Reflection.Emit;

namespace LinqToDB.TypeBuilder.Builders
{
	public class PropertyChangedBuilder : AbstractTypeBuilderBase
	{
		public PropertyChangedBuilder()
			: this(Common.Configuration.NotifyOnEqualSet, true, true)
		{
		}

		public PropertyChangedBuilder(bool notifyOnEqualSet, bool useReferenceEquals, bool skipSetterOnNoChange)
		{
			_notifyOnEqualSet     = notifyOnEqualSet;
			_useReferenceEquals   = useReferenceEquals;
			_skipSetterOnNoChange = skipSetterOnNoChange;
		}

		private readonly bool _notifyOnEqualSet;
		private readonly bool _useReferenceEquals;
		private readonly bool _skipSetterOnNoChange;

		public override bool IsApplied(BuildContext context, AbstractTypeBuilderList builders)
		{
			if (context == null) throw new ArgumentNullException("context");

			return context.IsSetter && (context.IsBeforeStep || context.IsAfterStep);
		}

		protected override void BeforeBuildAbstractSetter()
		{
			if (!_notifyOnEqualSet && Context.CurrentProperty.CanRead)
				GenerateIsSameValueComparison();
		}

		protected override void BeforeBuildVirtualSetter()
		{
			if (!_notifyOnEqualSet && Context.CurrentProperty.CanRead)
				GenerateIsSameValueComparison();
		}

		protected override void AfterBuildAbstractSetter()
		{
			BuildSetter();
		}

		protected override void AfterBuildVirtualSetter()
		{
			BuildSetter();
		}

		public override bool IsCompatible(BuildContext context, IAbstractTypeBuilder typeBuilder)
		{
			if (typeBuilder is PropertyChangedBuilder)
				return false;

			return base.IsCompatible(context, typeBuilder);
		}

		public override int GetPriority(BuildContext context)
		{
			return TypeBuilderConsts.Priority.PropChange;
		}

		private LocalBuilder _isSameValueBuilder;
		private Label        _afterNotificationLabel;

		private void GenerateIsSameValueComparison()
		{
			EmitHelper emit = Context.MethodBuilder.Emitter;

			if (_skipSetterOnNoChange)
				_afterNotificationLabel = emit.DefineLabel();
			else
				_isSameValueBuilder = emit.DeclareLocal(typeof(bool));

			MethodInfo op_InequalityMethod =
				Context.CurrentProperty.PropertyType.GetMethod("op_Inequality",
					new Type[]
						{
							Context.CurrentProperty.PropertyType,
							Context.CurrentProperty.PropertyType
						});

			if (op_InequalityMethod == null)
			{
				if (Context.CurrentProperty.PropertyType.IsValueType || !_useReferenceEquals)
				{
					emit
						.ldarg_0
						.callvirt  (Context.CurrentProperty.GetGetMethod(true))
						.ldarg_1
						.ceq
						.end();
				}
				else
				{
					emit
						.ldarg_0
						.callvirt  (Context.CurrentProperty.GetGetMethod(true))
						.ldarg_1
						.call      (typeof(object), "ReferenceEquals", typeof(object), typeof(object))
						.end();
				}
			}
			else
			{
				emit
					.ldarg_0
					.callvirt (Context.CurrentProperty.GetGetMethod(true))
					.ldarg_1
					.call     (op_InequalityMethod)
					.ldc_i4_0
					.ceq
					.end();
			}

			if (_skipSetterOnNoChange)
				emit.brtrue(_afterNotificationLabel);
			else
				emit.stloc(_isSameValueBuilder);
		}

		private void BuildSetter()
		{
			InterfaceMapping im   = Context.Type.GetInterfaceMap(typeof(IPropertyChanged));
			MethodInfo       mi   = im.TargetMethods[0];
			FieldBuilder     ifb  = GetPropertyInfoField();
			EmitHelper       emit = Context.MethodBuilder.Emitter;

			if (!_notifyOnEqualSet && Context.CurrentProperty.CanRead && !_skipSetterOnNoChange)
			{
				_afterNotificationLabel = emit.DefineLabel();
				emit
					.ldloc (_isSameValueBuilder)
					.brtrue(_afterNotificationLabel);
			}

			if (mi.IsPublic)
			{
				emit
					.ldarg_0
					.ldsfld   (ifb)
					.callvirt (mi)
					;
			}
			else
			{
				emit
					.ldarg_0
					.castclass (typeof(IPropertyChanged))
					.ldsfld    (ifb)
					.callvirt  (im.InterfaceMethods[0])
					;
			}

			if (!_notifyOnEqualSet && Context.CurrentProperty.CanRead)
				emit.MarkLabel(_afterNotificationLabel);
		}
	}
}
