using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Extensions;
	using Mapping;

	public class MemberAccessor
	{
		public MemberAccessor(TypeAccessor typeAccessor, MappingSchema mappingSchema, string memberName)
		{
			TypeAccessor = typeAccessor;

			if (memberName.IndexOf('.') < 0)
			{
				Init(Expression.PropertyOrField(Expression.Constant(null, typeAccessor.Type), memberName).Member);
			}
			else
			{
				var members  = memberName.Split('.');
				var param    = Expression.Parameter(TypeAccessor.Type, "obj");
				var expr     = param as Expression;
				/*
				var defValue = Expression.Constant(mappingSchema.GetDefaultValue(field.SystemType), field.SystemType);

				for (var i = 0; i < members.Length; i++)
				{
					var        member = members[i];
					Expression pof    = Expression.PropertyOrField(getter, member);

					getter = i == 0 ? pof : Expression.Condition(Expression.Equal(getter, Expression.Constant(null)), defValue, pof);
				}


				Getter = Expression.Lambda(
				Expression.MakeMemberAccess(Expression.Convert(param, TypeAccessor.Type), memberInfo),
				param);

				var defValue = Expression.Constant(dataContext.MappingSchema.NewSchema.GetDefaultValue(field.SystemType), field.SystemType);

				for (var i = 0; i < members.Length; i++)
				{
					var        member = members[i];
					Expression pof    = Expression.PropertyOrField(getter, member);

					getter = i == 0 ? pof : Expression.Condition(Expression.Equal(getter, Expression.Constant(null)), defValue, pof);
				}
				*/
			}
		}

		public MemberAccessor(TypeAccessor typeAccessor, MemberInfo memberInfo)
		{
			TypeAccessor = typeAccessor;
			Init(memberInfo);
		}

		void Init(MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;
			Type       = MemberInfo is PropertyInfo ? ((PropertyInfo)MemberInfo).PropertyType : ((FieldInfo)MemberInfo).FieldType;

			HasGetter = true;
			HasSetter = !(memberInfo is PropertyInfo) || ((PropertyInfo)memberInfo).GetSetMethod(true) != null;

			var param   = Expression.Parameter(TypeAccessor.Type, "obj");

			Getter = Expression.Lambda(
				Expression.MakeMemberAccess(Expression.Convert(param, TypeAccessor.Type), memberInfo),
				param);

			var objParam   = Expression.Parameter(typeof(object), "obj");
			var memberExpr = Expression.MakeMemberAccess(Expression.Convert(objParam, TypeAccessor.Type), memberInfo);
			var getterExpr = Expression.Lambda<Func<object,object>>(
				Expression.Convert(memberExpr, typeof(object)),
				objParam);

			_getter = getterExpr.Compile();

			if (HasSetter)
			{
				var valueParam = Expression.Parameter(typeof(object), "obj");
				var setterExpr = Expression.Lambda<Action<object,object>>(
					Expression.Assign(memberExpr, Expression.Convert(valueParam, Type)),
					objParam, valueParam);

				_setter = setterExpr.Compile();
			}
			else
			{
				_setter = (_,__) => { };
			}
		}

		#region Public Properties

		public MemberInfo       MemberInfo    { get; private set; }
		public TypeAccessor     TypeAccessor  { get; private set; }
		public bool             HasGetter     { get; private set; }
		public bool             HasSetter     { get; private set; }
		public Type             Type          { get; private set; }
		public LambdaExpression Getter        { get; private set; }

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
