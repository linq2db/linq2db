using System;
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

				var defValue = new DefaultValueExpression(MappingSchema.Default, Type);

				// Build getter.
				//
				{
					expr = objParam;

					for (var i = 0; i < infos.Length; i++)
					{
						var info = infos[i];

						if (info.member is PropertyInfo && ((PropertyInfo)info.member).GetSetMethod(true) == null)
							HasSetter = false;

						if (i == 0 || !(info.type.IsClass || info.type.IsNullable()))
						{
							expr = Expression.MakeMemberAccess(expr, info.member);
						}
						else
						{
							var local = Expression.Parameter(expr.Type);

							expr = Expression.Block(
								new[] { local },
								new[]
								{
									Expression.Assign(local, expr) as Expression,
									Expression.Condition(
										Expression.Equal(local, Expression.Constant(null)),
										defValue,
										Expression.MakeMemberAccess(local, info.member))
								});
						}
					}

					Getter = Expression.Lambda(expr, objParam);
				}

				// Build setter.
				//
				{
					var valueParam = Expression.Parameter(Type, "value");

					if (HasSetter)
					{
						for (var i = 0; i < infos.Length - 1; i++)
						{
							var info = infos[i];

							
						}
					}
					else
					{
						var fakeParam = Expression.Parameter(typeof(int));

						Setter = Expression.Lambda(
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
			HasSetter = !(memberInfo is PropertyInfo) || ((PropertyInfo)memberInfo).GetSetMethod(true) != null;

			var objParam   = Expression.Parameter(TypeAccessor.Type, "obj");
			var valueParam = Expression.Parameter(Type, "value");

			Getter = Expression.Lambda(Expression.MakeMemberAccess(objParam, memberInfo), objParam);

			if (HasSetter)
			{
				Setter = Expression.Lambda(
					Expression.Assign(Getter.Body, valueParam),
					objParam,
					valueParam);
			}
			else
			{
				var fakeParam = Expression.Parameter(typeof(int));

				Setter = Expression.Lambda(
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
			var getterExpr = Getter.GetBody(Expression.Convert(objParam, TypeAccessor.Type));
			var getter     = Expression.Lambda<Func<object,object>>(Expression.Convert(getterExpr, typeof(object)), objParam);

			_getter = getter.Compile();

			var valueParam = Expression.Parameter(typeof(object), "value");
			var setterExpr = Setter.GetBody(
				Expression.Convert(objParam,   TypeAccessor.Type),
				Expression.Convert(valueParam, Type));
			var setter = Expression.Lambda<Action<object,object>>(setterExpr, objParam, valueParam);

			_setter = setter.Compile();
		}

		#region Public Properties

		public MemberInfo       MemberInfo    { get; private set; }
		public TypeAccessor     TypeAccessor  { get; private set; }
		public bool             HasGetter     { get; private set; }
		public bool             HasSetter     { get; private set; }
		public Type             Type          { get; private set; }
		public bool             IsComplex     { get; private set; }
		public LambdaExpression Getter        { get; private set; }
		public LambdaExpression Setter        { get; private set; }

		public string Name
		{
			get { return MemberInfo.Name; }
		}

		#endregion

		#region Public Methods

		public T GetAttribute<T>() where T : Attribute
		{
			var attrs = MemberInfo.GetCustomAttributes(typeof(T), true);

			return attrs.Length > 0? (T)attrs[0]: null;
		}

		public T[] GetAttributes<T>() where T : Attribute
		{
			Array attrs = MemberInfo.GetCustomAttributes(typeof(T), true);

			return attrs.Length > 0? (T[])attrs: null;
		}

		public object[] GetAttributes()
		{
			var attrs = MemberInfo.GetCustomAttributes(true);

			return attrs.Length > 0? attrs: null;
		}

		public T[] GetTypeAttributes<T>() where T : Attribute
		{
			return TypeAccessor.Type.GetAttributes<T>();
		}

		#endregion

		#region Set/Get Value

		Func  <object,object> _getter;
		Action<object,object> _setter;

		public virtual object GetValue(object o)
		{
			return _getter(o);
		}

		public virtual void SetValue(object o, object value)
		{
			_setter(o, value);
		}

		#endregion
	}
}
