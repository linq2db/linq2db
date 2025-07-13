using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB.Reflection
{
	[DebuggerDisplay("{Name}: {Type.Name}")]
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

				_getterArguments  = new[] { objParam };
				_getterExpression = expr;

				// Build setter.
				//
				HasSetter = !infos.Any(info => info.member is PropertyInfo pi && pi.GetSetMethod(true) == null);

				var valueParam   = Expression.Parameter(Type, "value");
				_setterArguments = new[] { objParam, valueParam };

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

					_setterExpression = expr;
				}
				else
				{
					var fakeParam = Expression.Parameter(typeof(int));

					_setterExpression = Expression.Block(
						new[] { fakeParam },
						Expression.Assign(fakeParam, ExpressionInstances.Constant0));
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
		[MemberNotNull(nameof(Type), nameof(MemberInfo), nameof(_getterExpression), nameof(_getterArguments), nameof(_setterExpression), nameof(_setterArguments))]
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

			_getterArguments = new[] { objParam };

			if (HasGetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (ed?.DynamicColumnGetter != null)
				{
					_getterExpression = Expression.Convert(
						ed.DynamicColumnGetter.GetBody(
							objParam,
							Expression.Constant(memberInfo.Name),
							Expression.Convert(new DefaultValueExpression(ed.MappingSchema, Type), typeof(object))),
						Type);
				}
				else
					// dynamic columns store was not provided, throw exception when accessed
					// @mace_windu: why not throw it immediately? Fail fast
					_getterExpression = Expression.Call(_throwOnDynamicStoreMissingMethod.MakeGenericMethod(Type));
			}
			else if (HasGetter)
				_getterExpression = Expression.MakeMemberAccess(objParam, memberInfo);
			else
				_getterExpression = new DefaultValueExpression(ed?.MappingSchema ?? MappingSchema.Default, Type);

			_setterArguments = new[] { objParam, valueParam };
			if (HasSetter && memberInfo.IsDynamicColumnPropertyEx())
			{
				IsComplex = true;

				if (ed?.DynamicColumnSetter != null)
				{
					_setterExpression = ed.DynamicColumnSetter.GetBody(
						objParam,
						Expression.Constant(memberInfo.Name),
						valueParam);
				}
				else
					// dynamic columns store was not provided, throw exception when accessed
					// @mace_windu: why not throw it immediately? Fail fast
					_setterExpression = Expression.Block(
						Expression.Throw(
							Expression.New(
								ArgumentExceptionConstructorInfo,
								Expression.Constant("Tried setting dynamic column value, without setting dynamic column store on type."))),
						Expression.Constant(DefaultValue.GetValue(valueParam.Type), valueParam.Type));

			}
			else if (HasSetter)
				_setterExpression = Expression.Assign(Expression.MakeMemberAccess(objParam, memberInfo), valueParam);
			else
			{
				var fakeParam = Expression.Parameter(typeof(int));

				_setterExpression = Expression.Block(
					new[] { fakeParam },
					new Expression[] { Expression.Assign(fakeParam, ExpressionInstances.Constant0) });
			}
		}

		void SetExpressions()
		{
			// lazy init as those delegates used in rare cases and compilation is expensive
			_getter = new Lazy<Func<object, object?>>(() =>
			{
				var objParam   = Expression.Parameter(typeof(object), "obj");
				var getterExpr = GetGetterExpression(Expression.Convert(objParam, TypeAccessor.Type));
				var getter     = Expression.Lambda<Func<object,object?>>(Expression.Convert(getterExpr, typeof(object)), objParam);

				return getter.CompileExpression();
			});

			_setter = new Lazy<Action<object, object?>>(() =>
			{
				var objParam   = Expression.Parameter(typeof(object), "obj");
				var valueParam = Expression.Parameter(typeof(object), "value");
				var setterExpr = GetSetterExpression(
					Expression.Convert(objParam, TypeAccessor.Type),
					Expression.Convert(valueParam, Type));
				var setter = Expression.Lambda<Action<object, object?>>(setterExpr, objParam, valueParam);

				return setter.CompileExpression();
			});
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

		public string Name
		{
			get { return MemberInfo.Name; }
		}

		#endregion

		#region Set/Get Value

		private Lazy<Func<object, object?>>?   _getter;
		private Lazy<Action<object, object?>>? _setter;

		private Expression            _getterExpression;
		private ParameterExpression[] _getterArguments;

		private Expression            _setterExpression;
		private ParameterExpression[] _setterArguments;

		public Expression GetGetterExpression(Expression instance)
		{
			return _getterExpression.Transform(
				(parameters: _getterArguments, instance),
				static (context, e) => e == context.parameters[0] ? context.instance : e);
		}

		public Expression GetSetterExpression(Expression instance, Expression value)
		{
			return _setterExpression.Transform(
				(parameters: _setterArguments, instance, value),
				static (context, e) =>
					e == context.parameters[0] ? context.instance :
					e == context.parameters[1] ? context.value : e);
		}

		public virtual object? GetValue(object o)
		{
			return _getter!.Value(o);
		}

		public virtual void SetValue(object o, object? value)
		{
			_setter!.Value(o, value);
		}

		#endregion
	}
}
