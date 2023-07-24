using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace LinqToDB.Reflection
{
	using System.Diagnostics.CodeAnalysis;
	using Common;
	using Expressions;
	using Extensions;
	using LinqToDB.Common.Internal;
	using Mapping;

	public class MemberAccessor
	{
		static readonly ConstructorInfo ArgumentExceptionConstructorInfo = typeof(ArgumentException).GetConstructor(new[] {typeof(string)})!;

		internal MemberAccessor(TypeAccessor typeAccessor, string memberName, EntityDescriptor? ed)
		{
			TypeAccessor = typeAccessor;

			if (memberName.IndexOf('.') < 0)
			{
				SetSimple(ExpressionHelper.PropertyOrField(Expression.Constant(null, typeAccessor.Type), memberName).Member, ed);
			}
			else
			{
				IsComplex = true;
				HasGetter = true;
				HasSetter = true;

				var members  = memberName.Split('.');
				var objParam = Expression.Parameter(TypeAccessor.Type, "obj");
				var expr     = (Expression)objParam;
				var infos    = members.Select(m =>
				{
					expr = ExpressionHelper.PropertyOrField(expr, m);
					return new
					{
						member = ((MemberExpression)expr).Member,
						type   = expr.Type,
					};
				}).ToArray();

				var lastInfo = infos[infos.Length - 1];

				MemberInfo = lastInfo.member;
				Type       = lastInfo.type;

				var checkNull = infos.Take(infos.Length - 1).Any(info => info.type.IsNullableType());

				// Build getter.
				//
				if (checkNull)
				{
					var ret = Expression.Variable(Type, "ret");

					Expression MakeGetter(Expression ex, int i)
					{
						var info = infos[i];
						var next = Expression.MakeMemberAccess(ex, info.member);

						if (i == infos.Length - 1)
							return Expression.Assign(ret, next);

						if (next.Type.IsNullableType())
						{
							var local = Expression.Variable(next.Type);

							return Expression.Block(
								new[] { local },
								Expression.Assign(local, next),
								Expression.IfThen(
									Expression.NotEqual(local, ExpressionInstances.UntypedNull),
									MakeGetter(local, i + 1)));
						}

						return MakeGetter(next, i + 1);
					}

					expr = Expression.Block(
						new[] { ret },
						Expression.Assign(ret, new DefaultValueExpression(ed?.MappingSchema ?? MappingSchema.Default, Type)),
						MakeGetter(objParam, 0),
						ret);
				}
				else
				{
					expr = objParam;
					foreach (var info in infos)
						expr = Expression.MakeMemberAccess(expr, info.member);
				}

				GetterExpression = Expression.Lambda(expr, objParam);

				// Build setter.
				//
				HasSetter = !infos.Any(info => info.member is PropertyInfo pi && pi.GetSetMethod(true) == null);

				var valueParam = Expression.Parameter(Type, "value");

				if (HasSetter)
				{
					if (checkNull)
					{
						var vars  = new List<ParameterExpression>();
						var exprs = new List<Expression>();

						void MakeSetter(Expression ex, int i)
						{
							var info = infos[i];
							var next = Expression.MakeMemberAccess(ex, info.member);

							if (i == infos.Length - 1)
							{
								exprs.Add(Expression.Assign(next, valueParam));
							}
							else
							{
								if (next.Type.IsNullableType())
								{
									var local = Expression.Variable(next.Type);

									vars.Add(local);

									exprs.Add(Expression.Assign(local, next));
									exprs.Add(
										Expression.IfThen(
											Expression.Equal(local, ExpressionInstances.UntypedNull),
											Expression.Block(
												Expression.Assign(local, Expression.New(local.Type)),
												Expression.Assign(next, local))));

									MakeSetter(local, i + 1);
								}
								else
								{
									MakeSetter(next, i + 1);
								}
							}
						}

						MakeSetter(objParam, 0);

						expr = Expression.Block(vars, exprs);
					}
					else
					{
						expr = objParam;
						foreach (var info in infos)
							expr = Expression.MakeMemberAccess(expr, info.member);
						expr = Expression.Assign(expr, valueParam);
					}

					SetterExpression = Expression.Lambda(expr, objParam, valueParam);
				}
				else
				{
					var fakeParam = Expression.Parameter(typeof(int));

					SetterExpression = Expression.Lambda(
						Expression.Block(
							new[] { fakeParam },
							Expression.Assign(fakeParam, ExpressionInstances.Constant0)),
						objParam,
						valueParam);
				}
			}

			SetExpressions();
		}

		public MemberAccessor(TypeAccessor typeAccessor, MemberInfo memberInfo, EntityDescriptor? ed)
		{
			TypeAccessor = typeAccessor;

			SetSimple(memberInfo, ed);
			SetExpressions();
		}

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[MemberNotNull(nameof(Type), nameof(MemberInfo), nameof(GetterExpression), nameof(SetterExpression))]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		void SetSimple(MemberInfo memberInfo, EntityDescriptor? ed)
		{
			MemberInfo = memberInfo;
			Type       = MemberInfo is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)MemberInfo).FieldType;

			if (memberInfo is PropertyInfo info)
			{
				HasGetter = info.GetGetMethod(true) != null;
				HasSetter = info.GetSetMethod(true) != null;
			}
			else
			{
				HasGetter = true;
				HasSetter = !((FieldInfo)memberInfo).IsInitOnly;
			}

			var objParam   = Expression.Parameter(TypeAccessor.Type, "obj");
			var valueParam = Expression.Parameter(Type, "value");
			var getterType = typeof(Func<,>).MakeGenericType(TypeAccessor.Type, Type);
			var setterType = typeof(Action<,>).MakeGenericType(TypeAccessor.Type, Type);

			if (HasGetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (ed?.DynamicColumnGetter != null)
				{
					GetterExpression = Expression.Lambda(
						getterType,
						Expression.Convert(
							ed.DynamicColumnGetter.GetBody(
								objParam,
								Expression.Constant(memberInfo.Name),
								Expression.Convert(new DefaultValueExpression(ed.MappingSchema, Type), typeof(object))),
							Type),
						objParam);
				}
				else
					// dynamic columns store was not provided, throw exception when accessed
					// @mace_windu: why not throw it immediately? Fail fast
					GetterExpression = Expression.Lambda(
						getterType,
						Expression.Call(_throwOnDynamicStoreMissingMethod.MakeGenericMethod(Type)),
						objParam);
			}
			else if (HasGetter)
				GetterExpression = Expression.Lambda(getterType, Expression.MakeMemberAccess(objParam, memberInfo), objParam);
			else
				GetterExpression = Expression.Lambda(getterType, new DefaultValueExpression(ed?.MappingSchema ?? MappingSchema.Default, Type), objParam);

			if (HasSetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (ed?.DynamicColumnSetter != null)
				{
					SetterExpression = Expression.Lambda(
						setterType,
						ed.DynamicColumnSetter.GetBody(
							objParam,
							Expression.Constant(memberInfo.Name),
							valueParam),
						objParam,
						valueParam);
				}
				else
					// dynamic columns store was not provided, throw exception when accessed
					// @mace_windu: why not throw it immediately? Fail fast
					SetterExpression = Expression.Lambda(
						setterType,
						Expression.Block(
							Expression.Throw(
								Expression.New(
									ArgumentExceptionConstructorInfo,
									Expression.Constant("Tried setting dynamic column value, without setting dynamic column store on type."))),
							Expression.Constant(DefaultValue.GetValue(valueParam.Type), valueParam.Type)
						),
						objParam,
						valueParam);

			}
			else if (HasSetter)
				SetterExpression = Expression.Lambda(
					setterType,
					Expression.Assign(Expression.MakeMemberAccess(objParam, memberInfo), valueParam),
					objParam,
					valueParam);
			else
			{
				var fakeParam = Expression.Parameter(typeof(int));

				SetterExpression = Expression.Lambda(
					setterType,
					Expression.Block(
						new[] { fakeParam },
						new Expression[] { Expression.Assign(fakeParam, ExpressionInstances.Constant0) }),
					objParam,
					valueParam);
			}
		}

		void SetExpressions()
		{
			var objParam   = Expression.Parameter(typeof(object), "obj");
			var getterExpr = GetterExpression.GetBody(Expression.Convert(objParam, TypeAccessor.Type));
			var getter     = Expression.Lambda<Func<object,object?>>(Expression.Convert(getterExpr, typeof(object)), objParam);

			Getter = getter.CompileExpression();

			var valueParam = Expression.Parameter(typeof(object), "value");

			if (SetterExpression != null)
			{
				var setterExpr = SetterExpression.GetBody(
					Expression.Convert(objParam, TypeAccessor.Type),
					Expression.Convert(valueParam, Type));
				var setter = Expression.Lambda<Action<object, object?>>(setterExpr, objParam, valueParam);

				Setter = setter.CompileExpression();
			}
		}

		static readonly MethodInfo _throwOnDynamicStoreMissingMethod = MemberHelper.MethodOf(() => ThrowOnDynamicStoreMissing<int>()).GetGenericMethodDefinition();
		static T ThrowOnDynamicStoreMissing<T>()
		{
			throw new ArgumentException("Tried getting dynamic column value, without setting dynamic column store on type.");
		}

		#region Public Properties

		public MemberInfo              MemberInfo       { get; private set; }
		public TypeAccessor            TypeAccessor     { get; private set; }
		public bool                    HasGetter        { get; private set; }
		public bool                    HasSetter        { get; private set; }
		public Type                    Type             { get; private set; }
		public bool                    IsComplex        { get; private set; }
		public LambdaExpression        GetterExpression { get; private set; }
		public LambdaExpression        SetterExpression { get; private set; }
		public Func  <object,object?>? Getter           { get; private set; }
		public Action<object,object?>? Setter           { get; private set; }

		public string Name
		{
			get { return MemberInfo.Name; }
		}

		#endregion

		#region Set/Get Value

		public virtual object? GetValue(object o)
		{
			return Getter!(o);
		}

		public virtual void SetValue(object o, object? value)
		{
			Setter!(o, value);
		}

		#endregion
	}
}
