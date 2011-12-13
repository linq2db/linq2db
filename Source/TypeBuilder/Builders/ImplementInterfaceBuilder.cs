using System;
using System.Reflection.Emit;

namespace LinqToDB.TypeBuilder.Builders
{
	using Reflection;

	class ImplementInterfaceBuilder : AbstractTypeBuilderBase
	{
		public ImplementInterfaceBuilder(Type type)
		{
			_type = type;
		}

		private readonly Type _type;

		public override Type[] GetInterfaces()
		{
			return new[] { _type };
		}

		public override bool IsApplied(BuildContext context, AbstractTypeBuilderList builders)
		{
			if (context == null) throw new ArgumentNullException("context");

			return context.BuildElement == BuildElement.InterfaceMethod;
		}

		protected override void BuildInterfaceMethod()
		{
			var returnIfNonZero = false;
			var returnIfZero    = false;
			
			if (Context.ReturnValue != null)
			{
				var attrs = Context.MethodBuilder.OverriddenMethod.ReturnTypeCustomAttributes.GetCustomAttributes(true);

				foreach (var o in attrs)
				{
					if      (o is ReturnIfNonZeroAttribute) returnIfNonZero = true;
					else if (o is ReturnIfZeroAttribute)    returnIfZero    = true;
				}
			}

			var interfaceType = Context.CurrentInterface;
			var emit          = Context.MethodBuilder.Emitter;

			foreach (var de in Context.Fields)
			{
				var property = de.Key;
				var field    = de.Value;

				if (field.FieldType.IsPrimitive || field.FieldType == typeof(string))
					continue;

				var types = field.FieldType.GetInterfaces();

				foreach (var type in types)
				{
					if (type != interfaceType.Type)
						continue;

					var im = field.FieldType.GetInterfaceMap(type);

					for (var j = 0; j < im.InterfaceMethods.Length; j++)
					{
						if (im.InterfaceMethods[j] == Context.MethodBuilder.OverriddenMethod)
						{
							var targetMethod = im.TargetMethods[j];

							var label     = new Label();
							var checkNull = false;

							if (CallLazyInstanceInsurer(field) == false && field.FieldType.IsClass)
							{
								// Check if field is null.
								//
								checkNull = true;

								label = emit.DefineLabel();

								emit
									.ldarg_0
									.ldfld     (field)
									.brfalse_s (label)
									;
							}

							// this.
							//
							emit
								.ldarg_0
								.end();

							// Load the field and prepare it for interface method call if the method is private.
							//
							if (field.FieldType.IsValueType)
							{
								if (targetMethod.IsPublic)
									emit.ldflda (field);
								else
									emit
										.ldfld  (field)
										.box    (field.FieldType);
							}
							else
							{
								if (targetMethod.IsPublic)
									emit.ldfld (field);
								else
									emit
										.ldfld     (field)
										.castclass (interfaceType);
							}

							// Check parameter attributes.
							//
							var pi = Context.MethodBuilder.OverriddenMethod.GetParameters();

							for (var k = 0; k < pi.Length; k++)
							{
								var attrs = pi[k].GetCustomAttributes(true);
								var stop  = false;

								foreach (var a in attrs)
								{
									// Parent - set this.
									//
									if (a is ParentAttribute)
									{
										emit
											.ldarg_0
											.end()
											;

										if (!TypeHelper.IsSameOrParent(pi[k].ParameterType, Context.Type))
											emit
												.castclass (pi[k].ParameterType)
												;

										stop = true;

										break;
									}

									// PropertyInfo.
									//
									if (a is PropertyInfoAttribute)
									{
										var ifb = GetPropertyInfoField(property);

										emit.ldsfld(ifb);
										stop = true;

										break;
									}
								}

								if (stop)
									continue;

								// Pass argument.
								//
								emit.ldarg ((byte)(k + 1));
							}

							// Call the method.
							//
							if (field.FieldType.IsValueType)
							{
								if (targetMethod.IsPublic) emit.call     (targetMethod);
								else                       emit.callvirt (im.InterfaceMethods[j]);
							}
							else
							{
								if (targetMethod.IsPublic) emit.callvirt (targetMethod);
								else                       emit.callvirt (im.InterfaceMethods[j]);
							}

							// Return if appropriated result.
							//
							if (Context.ReturnValue != null)
							{
								emit.stloc(Context.ReturnValue);

								if (returnIfNonZero)
								{
									emit
										.ldloc  (Context.ReturnValue)
										.brtrue (Context.ReturnLabel);
								}
								else if (returnIfZero)
								{
									emit
										.ldloc   (Context.ReturnValue)
										.brfalse (Context.ReturnLabel);
								}
							}

							if (checkNull)
								emit.MarkLabel(label);

							break;
						}
					}

					break;
				}
			}
		}
	}
}
