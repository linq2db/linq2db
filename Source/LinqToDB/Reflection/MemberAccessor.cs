using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using LinqToDB.Common;

namespace LinqToDB.Reflection
{
	using Expressions;
	using Extensions;
	using Mapping;

	public class MemberAccessor
	{
		static readonly ConstructorInfo ArgumentExceptionConstructorInfo = typeof(ArgumentException).GetConstructor(new[] {typeof(string)}) ??
					            throw new Exception($"Can not retrieve information about constructor for {nameof(ArgumentException)}");

		internal MemberAccessor(TypeAccessor typeAccessor, string memberName)
		{
			TypeAccessor = typeAccessor;

			if (memberName.IndexOf('.') < 0)
			{
				SetSimple(Expression.PropertyOrField(Expression.Constant(null, typeAccessor.Type), memberName).Member);
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
					expr = Expression.PropertyOrField(expr, m);
					return new
					{
						member = ((MemberExpression)expr).Member,
						type   = expr.Type,
					};
				}).ToArray();

				var lastInfo = infos[infos.Length - 1];

				MemberInfo = lastInfo.member;
				Type       = lastInfo.type;

				var checkNull = infos.Take(infos.Length - 1).Any(info => info.type.IsClassEx() || info.type.IsNullable());

				// Build getter.
				//
				{
					if (checkNull)
					{
						var ret = Expression.Variable(Type, "ret");

						Expression MakeGetter(Expression ex, int i)
						{
							var info = infos[i];
							var next = Expression.MakeMemberAccess(ex, info.member);

							if (i == infos.Length - 1) 
								return Expression.Assign(ret, next);

							if (next.Type.IsClassEx() || next.Type.IsNullable())
							{
								var local = Expression.Variable(next.Type);

								return Expression.Block(
									new[] { local }, 
									Expression.Assign(local, next) as Expression,
									Expression.IfThen(
										Expression.NotEqual(local, Expression.Constant(null)), 
										MakeGetter(local, i + 1)));
							}

							return MakeGetter(next, i + 1);
						}

						expr = Expression.Block(
							new[] { ret },
							Expression.Assign(ret, new DefaultValueExpression(MappingSchema.Default, Type)),
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
				}

				// Build setter.
				//
				{
					HasSetter = !infos.Any(info => info.member is PropertyInfo && ((PropertyInfo)info.member).GetSetMethodEx(true) == null);

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
									if (next.Type.IsClassEx() || next.Type.IsNullable())
									{
										var local = Expression.Variable(next.Type);

										vars.Add(local);

										exprs.Add(Expression.Assign(local, next));
										exprs.Add(
											Expression.IfThen(
												Expression.Equal(local, Expression.Constant(null)),
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
								Expression.Assign(fakeParam, Expression.Constant(0))),
							objParam,
							valueParam);
					}
				}
			}

			SetExpressions();
		}

		public MemberAccessor(TypeAccessor typeAccessor, MemberInfo memberInfo)
		{
			TypeAccessor = typeAccessor;

			SetSimple(memberInfo);
			SetExpressions();
		}

		void SetSimple(MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;
			Type       = MemberInfo is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)MemberInfo).FieldType;

			if (memberInfo is PropertyInfo info)
			{
				HasGetter = info.GetGetMethodEx(true) != null;
				HasSetter = info.GetSetMethodEx(true) != null;
			}
			else
			{
				HasGetter = true;
				HasSetter = !((FieldInfo)memberInfo).IsInitOnly;
			}

			var objParam   = Expression.Parameter(TypeAccessor.Type, "obj");
			var valueParam = Expression.Parameter(Type, "value");

			if (HasGetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (TypeAccessor.DynamicColumnsStoreAccessor != null)
				{
					// get value via "Item" accessor; we're not null-checking

					var storageType = TypeAccessor.DynamicColumnsStoreAccessor.MemberInfo.GetMemberType();
					var storedType  = storageType.GetGenericArguments()[1];
					var outVar      = Expression.Variable(storedType);
					var resultVar   = Expression.Variable(Type);

					MethodInfo tryGetValueMethodInfo = storageType.GetMethod("TryGetValue");

					if (tryGetValueMethodInfo == null)
						throw new LinqToDBException("Storage property do not have method 'TryGetValue'");

					GetterExpression =
						Expression.Lambda(
							Expression.Block(
								new[] { outVar, resultVar },
								Expression.IfThenElse(
									Expression.Call(
										Expression.MakeMemberAccess(objParam,
											TypeAccessor.DynamicColumnsStoreAccessor.MemberInfo),
										tryGetValueMethodInfo,
										Expression.Constant(memberInfo.Name), outVar),
									Expression.Assign(resultVar, Expression.Convert(outVar, Type)),
									Expression.Assign(resultVar,
										new DefaultValueExpression(MappingSchema.Default, Type))
								),
								resultVar
							),
							objParam);
				}
				else
					// dynamic columns store was not provided, throw exception when accessed
					GetterExpression = Expression.Lambda(
						Expression.Throw(
							Expression.New(
								ArgumentExceptionConstructorInfo,
								Expression.Constant("Tried getting dynamic column value, without setting dynamic column store on type."))),
						objParam);
			}
			else if (HasGetter)
			{
				GetterExpression = Expression.Lambda(Expression.MakeMemberAccess(objParam, memberInfo), objParam);
			}
			else
			{
				GetterExpression = Expression.Lambda(new DefaultValueExpression(MappingSchema.Default, Type), objParam);
			}

			if (HasSetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (TypeAccessor.DynamicColumnsStoreAccessor != null)
					// if null, create new dictionary; then assign value
					SetterExpression =
						Expression.Lambda(
							Expression.Block(
								Expression.IfThen(
									Expression.ReferenceEqual(
										Expression.MakeMemberAccess(objParam, TypeAccessor.DynamicColumnsStoreAccessor.MemberInfo),
										Expression.Constant(null)),
									Expression.Assign(
										Expression.MakeMemberAccess(objParam, TypeAccessor.DynamicColumnsStoreAccessor.MemberInfo),
										Expression.New(typeof(Dictionary<string, object>)))),
								Expression.Assign(
									Expression.Property(
										Expression.MakeMemberAccess(objParam, TypeAccessor.DynamicColumnsStoreAccessor.MemberInfo),
										"Item",
										Expression.Constant(memberInfo.Name)),
									Expression.Convert(valueParam, typeof(object)))),
							objParam,
							valueParam);
				else
					// dynamic columns store was not provided, throw exception when accessed
					GetterExpression = Expression.Lambda(
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
			{
				SetterExpression = Expression.Lambda(
					Expression.Assign(Expression.MakeMemberAccess(objParam, memberInfo), valueParam),
					objParam,
					valueParam);
			}
			else
			{
				var fakeParam = Expression.Parameter(typeof(int));

				SetterExpression = Expression.Lambda(
					Expression.Block(
						new[] { fakeParam },
						new Expression[] { Expression.Assign(fakeParam, Expression.Constant(0)) }),
					objParam,
					valueParam);
			}
		}

		void SetExpressions()
		{
			var objParam   = Expression.Parameter(typeof(object), "obj");
			var getterExpr = GetterExpression.GetBody(Expression.Convert(objParam, TypeAccessor.Type));
			var getter     = Expression.Lambda<Func<object,object>>(Expression.Convert(getterExpr, typeof(object)), objParam);

			Getter = getter.Compile();

			var valueParam = Expression.Parameter(typeof(object), "value");

			if (SetterExpression != null)
			{
				var setterExpr = SetterExpression.GetBody(
					Expression.Convert(objParam, TypeAccessor.Type),
					Expression.Convert(valueParam, Type));
				var setter = Expression.Lambda<Action<object, object>>(setterExpr, objParam, valueParam);

				Setter = setter.Compile();
			}
		}

		#region Public Properties

		public MemberInfo            MemberInfo       { get; private set; }
		public TypeAccessor          TypeAccessor     { get; private set; }
		public bool                  HasGetter        { get; private set; }
		public bool                  HasSetter        { get; private set; }
		public Type                  Type             { get; private set; }
		public bool                  IsComplex        { get; private set; }
		public LambdaExpression      GetterExpression { get; private set; }
		public LambdaExpression      SetterExpression { get; private set; }
		public Func  <object,object> Getter           { get; private set; }
		public Action<object,object> Setter           { get; private set; }

		public string Name
		{
			get { return MemberInfo.Name; }
		}

		#endregion

		#region Public Methods

		public T GetAttribute<T>() where T : Attribute
		{
			var attrs = MemberInfo.GetCustomAttributesEx(typeof(T), true);

			return attrs.Length > 0? (T)attrs[0]: null;
		}

		public T[] GetAttributes<T>() where T : Attribute
		{
			Array attrs = MemberInfo.GetCustomAttributesEx(typeof(T), true);

			return attrs.Length > 0? (T[])attrs: null;
		}

		public object[] GetAttributes()
		{
			var attrs = MemberInfo.GetCustomAttributesEx(true);

			return attrs.Length > 0? attrs: null;
		}

		public T[] GetTypeAttributes<T>() where T : Attribute
		{
			return TypeAccessor.Type.GetAttributes<T>();
		}

		#endregion

		#region Set/Get Value

		public virtual object GetValue(object o)
		{
			return Getter(o);
		}

		public virtual void SetValue(object o, object value)
		{
			Setter(o, value);
		}

		#endregion
	}
}
