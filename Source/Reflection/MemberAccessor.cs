using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace LinqToDB.Reflection
{
	using Expressions;
	using Extensions;
	using Mapping;

	public class MemberAccessor
	{
		public MemberAccessor(TypeAccessor typeAccessor, string memberName)
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
				var expr     = objParam as Expression;
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

						Func<Expression,int,Expression> makeGetter = null; makeGetter = (ex, i) =>
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
									new[]
									{
										Expression.Assign(local, next) as Expression,
										Expression.IfThen(
											Expression.NotEqual(local, Expression.Constant(null)),
											makeGetter(local, i + 1))
									});
							}

							return makeGetter(next, i + 1);
						};

						expr = Expression.Block(
							new[] { ret },
							new[]
							{
								Expression.Assign(ret, new DefaultValueExpression(MappingSchema.Default, Type)),
								makeGetter(objParam, 0),
								ret
							});
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

							Action<Expression,int> makeSetter = null; makeSetter = (ex, i) =>
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

										makeSetter(local, i + 1);
									}
									else
									{
										makeSetter(next, i + 1);
									}
								}
							};

							makeSetter(objParam, 0);

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
								new Expression[] { Expression.Assign(fakeParam, Expression.Constant(0)) }),
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
			Type       = MemberInfo is PropertyInfo ? ((PropertyInfo)MemberInfo).PropertyType : ((FieldInfo)MemberInfo).FieldType;

			HasGetter = true;
			HasSetter = !(memberInfo is PropertyInfo) || ((PropertyInfo)memberInfo).GetSetMethodEx(true) != null;

			var objParam   = Expression.Parameter(TypeAccessor.Type, "obj");
			var valueParam = Expression.Parameter(Type, "value");

			GetterExpression = Expression.Lambda(Expression.MakeMemberAccess(objParam, memberInfo), objParam);

			if (HasSetter)
			{
				SetterExpression = Expression.Lambda(
					Expression.Assign(GetterExpression.Body, valueParam),
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
			var setterExpr = SetterExpression.GetBody(
				Expression.Convert(objParam,   TypeAccessor.Type),
				Expression.Convert(valueParam, Type));
			var setter = Expression.Lambda<Action<object,object>>(setterExpr, objParam, valueParam);

			Setter = setter.Compile();
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
