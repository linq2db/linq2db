// BASEDON: https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/Query/Internal/ExpressionEqualityComparer.cs

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Reflection;

// ReSharper disable SwitchStatementMissingSomeCases
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
namespace LinqToDB.Expressions
{
	/// <summary>
	///     This API supports the linq2db infrastructure and is not intended to be used
	///     directly from your code. This API may change or be removed in future releases.
	/// </summary>
	public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression>
	{
		public static ExpressionEqualityComparer Instance { get; } = new ExpressionEqualityComparer();

		ExpressionEqualityComparer()
		{
		}

		public int GetHashCode(Expression? obj)
		{
			var hashCode = new HashCode();
			UpdateHashCode(in hashCode, obj);
			return hashCode.ToHashCode();
		}

		void UpdateHashCode(ref readonly HashCode hashCode, Expression? obj)
		{
			if (obj is null)
				return;

			hashCode.Add(obj.NodeType);
			hashCode.Add(obj.Type);

			switch (obj.NodeType)
			{
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.ArrayLength:
				case ExpressionType.Quote:
				case ExpressionType.TypeAs:
				case ExpressionType.UnaryPlus:
				case ExpressionType.Throw:
				{
					var unaryExpression = (UnaryExpression)obj;

					hashCode.Add(unaryExpression.Method);
					hashCode.Add(GetHashCode(unaryExpression.Operand));

					break;
				}

				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Divide:
				case ExpressionType.Modulo:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.Coalesce:
				case ExpressionType.ArrayIndex:
				case ExpressionType.RightShift:
				case ExpressionType.LeftShift:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Power:
				case ExpressionType.Assign:
				{
					var binaryExpression = (BinaryExpression)obj;

					hashCode.Add(GetHashCode(binaryExpression.Left));
					hashCode.Add(GetHashCode(binaryExpression.Right));

					break;
				}

				case ExpressionType.TypeIs:
				{
					var typeBinaryExpression = (TypeBinaryExpression)obj;

					hashCode.Add(GetHashCode(typeBinaryExpression.Expression));
					hashCode.Add(typeBinaryExpression.TypeOperand);

					break;
				}

				case ExpressionType.Constant:
				{
					var constantExpression = (ConstantExpression)obj;

					if (constantExpression.Value != null
						&& constantExpression.Value is not IQueryable 
						&& (constantExpression.Value is string or not IEnumerable))
					{
						hashCode.Add(constantExpression.Value);
					}

					break;
				}

				case ExpressionType.Parameter:
				{
					var parameterExpression = (ParameterExpression)obj;

					if (parameterExpression.Name is not null)
						hashCode.Add(parameterExpression.Name);

					break;
				}

				case ExpressionType.MemberAccess:
				{
					var memberExpression = (MemberExpression)obj;

					hashCode.Add(memberExpression.Member, MemberInfoEqualityComparer.Default);
					hashCode.Add(GetHashCode(memberExpression.Expression));

					break;
				}

				case ExpressionType.Call:
				{
					var methodCallExpression = (MethodCallExpression)obj;

					hashCode.Add(methodCallExpression.Method);
					hashCode.Add(GetHashCode(methodCallExpression.Object));
					UpdateHashCode(in hashCode, methodCallExpression.Arguments);

					break;
				}

				case ExpressionType.Lambda:
				{
					var lambdaExpression = (LambdaExpression)obj;

					hashCode.Add(lambdaExpression.ReturnType);
					hashCode.Add(GetHashCode(lambdaExpression.Body));
					UpdateHashCode(in hashCode, lambdaExpression.Parameters);

					break;
				}

				case ExpressionType.New:
				{
					var newExpression = (NewExpression)obj;

					hashCode.Add(newExpression.Constructor);

					if (newExpression.Members != null)
					{
						foreach (var m in newExpression.Members)
							hashCode.Add(m);
					}

					UpdateHashCode(in hashCode, newExpression.Arguments);

					break;
				}

				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
				{
					var newArrayExpression = (NewArrayExpression)obj;
					UpdateHashCode(in hashCode, newArrayExpression.Expressions);
					break;
				}

				case ExpressionType.Invoke:
				{
					var invocationExpression = (InvocationExpression)obj;

					hashCode.Add(GetHashCode(invocationExpression.Expression));
					UpdateHashCode(in hashCode, invocationExpression.Arguments);

					break;
				}

				case ExpressionType.MemberInit:
				{
					var memberInitExpression = (MemberInitExpression)obj;

					hashCode.Add(GetHashCode(memberInitExpression.NewExpression));
					UpdateHashCode(in hashCode, memberInitExpression.Bindings);

					break;
				}

				case ExpressionType.ListInit:
				{
					var listInitExpression = (ListInitExpression)obj;

					hashCode.Add(GetHashCode(listInitExpression.NewExpression));

					foreach (var i in listInitExpression.Initializers)
						UpdateHashCode(in hashCode, i.Arguments);

					break;
				}

				case ExpressionType.Conditional:
				{
					var conditionalExpression = (ConditionalExpression)obj;

					hashCode.Add(GetHashCode(conditionalExpression.Test));
					hashCode.Add(GetHashCode(conditionalExpression.IfTrue));
					hashCode.Add(GetHashCode(conditionalExpression.IfFalse));

					break;
				}

				case ExpressionType.Default:
				{
					hashCode.Add(obj.Type);
					break;
				}

				case ExpressionType.Extension:
				{
					hashCode.Add(obj);
					break;
				}

				case ChangeTypeExpression.ChangeTypeType:
				{
					hashCode.Add(obj);
					break;
				}

				case ExpressionType.Block:
				{
					var blockExpression = (BlockExpression)obj;

					foreach (var v in blockExpression.Variables)
						hashCode.Add(GetHashCode(v));

					foreach (var e in blockExpression.Expressions)
						hashCode.Add(GetHashCode(e));

					break;
				}

				case ExpressionType.Index:
				{
					var indexExpression = (IndexExpression)obj;
					hashCode.Add(GetHashCode(indexExpression.Object));

					foreach (var a in indexExpression.Arguments)
						hashCode.Add(GetHashCode(a));

					break;
				}

				case ExpressionType.Switch:
				{
					var switchExpression = (SwitchExpression)obj;

					hashCode.Add(GetHashCode(switchExpression.SwitchValue));

					foreach (var c in switchExpression.Cases)
					{
						UpdateHashCode(in hashCode, c.TestValues);
						hashCode.Add(GetHashCode(c.Body));
					}

					hashCode.Add(GetHashCode(switchExpression.DefaultBody));

					break;
				}
				default:
					throw new NotImplementedException();
			}
		}

		void UpdateHashCode(ref readonly HashCode hashCode, IList<MemberBinding> bindings)
		{
			foreach (var memberBinding in bindings)
			{
				hashCode.Add(memberBinding.Member);
				hashCode.Add(memberBinding.BindingType);

				switch (memberBinding.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var memberAssignment = (MemberAssignment)memberBinding;
						hashCode.Add(GetHashCode(memberAssignment.Expression));
						break;
					}

					case MemberBindingType.ListBinding:
					{
						var memberListBinding = (MemberListBinding)memberBinding;

						foreach (var i in memberListBinding.Initializers)
							UpdateHashCode(in hashCode, i.Arguments);

						break;
					}

					case MemberBindingType.MemberBinding:
					{
						var memberMemberBinding = (MemberMemberBinding)memberBinding;
						UpdateHashCode(in hashCode, memberMemberBinding.Bindings);
						break;
					}

					default:
						throw new NotImplementedException();
				}
			}
		}

		void UpdateHashCode<T>(ref readonly HashCode hashCode, IList<T> expressions)
			where T : Expression
		{
			foreach (var e in expressions)
				UpdateHashCode(in hashCode, e);
		}

		public bool Equals(Expression? x, Expression? y) => new ExpressionComparer().Compare(x, y);

		sealed class ExpressionComparer
		{
			ScopedDictionary<ParameterExpression, ParameterExpression>? _parameterScope;

			public bool Compare(Expression? a, Expression? b)
			{
				if (a == b)
				{
					return true;
				}

				if (a == null || b == null)
				{
					return false;
				}

				if (a.NodeType != b.NodeType)
				{
					return false;
				}

				if (a.Type != b.Type)
				{
					return false;
				}

				switch (a.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
					case ExpressionType.Not:
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					case ExpressionType.ArrayLength:
					case ExpressionType.Quote:
					case ExpressionType.TypeAs:
					case ExpressionType.UnaryPlus:
						return CompareUnary((UnaryExpression)a, (UnaryExpression)b);
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.Divide:
					case ExpressionType.Modulo:
					case ExpressionType.And:
					case ExpressionType.AndAlso:
					case ExpressionType.Or:
					case ExpressionType.OrElse:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.Equal:
					case ExpressionType.NotEqual:
					case ExpressionType.Coalesce:
					case ExpressionType.ArrayIndex:
					case ExpressionType.RightShift:
					case ExpressionType.LeftShift:
					case ExpressionType.ExclusiveOr:
					case ExpressionType.Power:
					case ExpressionType.Assign:
						return CompareBinary((BinaryExpression)a, (BinaryExpression)b);
					case ExpressionType.TypeIs:
						return CompareTypeIs((TypeBinaryExpression)a, (TypeBinaryExpression)b);
					case ExpressionType.Conditional:
						return CompareConditional((ConditionalExpression)a, (ConditionalExpression)b);
					case ExpressionType.Default: return true;
					case ExpressionType.Constant:
						return CompareConstant((ConstantExpression)a, (ConstantExpression)b);
					case ExpressionType.Parameter:
						return CompareParameter((ParameterExpression)a, (ParameterExpression)b);
					case ExpressionType.MemberAccess:
						return CompareMemberAccess((MemberExpression)a, (MemberExpression)b);
					case ExpressionType.Call:
						return CompareMethodCall((MethodCallExpression)a, (MethodCallExpression)b);
					case ExpressionType.Lambda:
						return CompareLambda((LambdaExpression)a, (LambdaExpression)b);
					case ExpressionType.New:
						return CompareNew((NewExpression)a, (NewExpression)b);
					case ExpressionType.NewArrayInit:
					case ExpressionType.NewArrayBounds:
						return CompareNewArray((NewArrayExpression)a, (NewArrayExpression)b);
					case ExpressionType.Invoke:
						return CompareInvocation((InvocationExpression)a, (InvocationExpression)b);
					case ExpressionType.MemberInit:
						return CompareMemberInit((MemberInitExpression)a, (MemberInitExpression)b);
					case ExpressionType.ListInit:
						return CompareListInit((ListInitExpression)a, (ListInitExpression)b);
					case ExpressionType.Extension:
						return CompareExtension(a, b);
					case ChangeTypeExpression.ChangeTypeType:
						return a.Equals(b);
					case ExpressionType.Block:
						return CompareBlock((BlockExpression)a, (BlockExpression)b);
					case ExpressionType.Throw:
						return CompareUnary((UnaryExpression)a, (UnaryExpression)b);
					case ExpressionType.Index:
						return CompareIndex((IndexExpression)a, (IndexExpression)b);
					case ExpressionType.Switch:
						return CompareSwitch((SwitchExpression)a, (SwitchExpression)b);
					default:
						throw new NotImplementedException();
				}
			}

			bool CompareIndex(IndexExpression a, IndexExpression b)
			{
				return Equals(a.Indexer, b.Indexer)
					&& Compare(a.Object, b.Object)
					&& CompareExpressionList(a.Arguments, b.Arguments);
			}

			bool CompareSwitch(SwitchExpression a, SwitchExpression b)
			{
				if (! (Compare(a.SwitchValue, b.SwitchValue)
						&& Compare(a.DefaultBody, b.DefaultBody)
						&& Equals(a.Comparison, b.Comparison)
						&& a.Cases.Count == b.Cases.Count))
				{
					return false;
				}

				for (var i = 0; i < a.Cases.Count; i++)
				{
					if (!Compare(a.Cases[i].Body, b.Cases[i].Body))
						return false;
					if (!Compare(a.Cases[i].Body, b.Cases[i].Body))
						return false;
					if (!CompareExpressionList(a.Cases[i].TestValues, b.Cases[i].TestValues))
						return false;
				}

				return true;
			}

			bool CompareUnary(UnaryExpression a, UnaryExpression b)
				=> Equals(a.Method, b.Method)
				   && a.IsLifted == b.IsLifted
				   && a.IsLiftedToNull == b.IsLiftedToNull
				   && Compare(a.Operand, b.Operand);

			bool CompareBinary(BinaryExpression a, BinaryExpression b)
				=> Equals(a.Method, b.Method)
				   && a.IsLifted == b.IsLifted
				   && a.IsLiftedToNull == b.IsLiftedToNull
				   && Compare(a.Left, b.Left)
				   && Compare(a.Right, b.Right);

			bool CompareTypeIs(TypeBinaryExpression a, TypeBinaryExpression b)
				=> a.TypeOperand == b.TypeOperand
				   && Compare(a.Expression, b.Expression);

			bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
				=> Compare(a.Test, b.Test)
				   && Compare(a.IfTrue, b.IfTrue)
				   && Compare(a.IfFalse, b.IfFalse);

			static bool CompareConstant(ConstantExpression a, ConstantExpression b)
			{
				if (a.Value == b.Value)
				{
					return true;
				}

				if (a.Value == null
					|| b.Value == null)
				{
					return false;
				}

				if (a.Value is EnumerableQuery
					&& b.Value is EnumerableQuery)
				{
					return false; // EnumerableQueries are opaque
				}

				if (a.Value is IEnumerable ae && b.Value is IEnumerable be)
				{
					var enum1 = ae.GetEnumerator();
					var enum2 = be.GetEnumerator();
					using (enum1 as IDisposable)
					using (enum2 as IDisposable)
					{
						while (enum1.MoveNext())
						{
							if (!enum2.MoveNext() || !Equals(enum1.Current, enum2.Current))
								return false;
						}

						if (enum2.MoveNext())
							return false;
					}

					return true;
				}

				if (typeof(ExpressionQuery<>).IsSameOrParentOf(a.GetType())
					&& typeof(ExpressionQuery<>).IsSameOrParentOf(b.GetType())
					&& a.Value.GetType() == b.Value.GetType())
				{
					return true;
				}

				return Equals(a.Value, b.Value);
			}

			bool CompareParameter(ParameterExpression a, ParameterExpression b)
			{
				if (_parameterScope != null)
				{
					if (_parameterScope.TryGetValue(a, out var mapped))
					{
						return mapped!.Name == b.Name
							   && mapped.Type == b.Type;
					}
				}

				return a.Name == b.Name
					   && a.Type == b.Type;
			}

			bool CompareMemberAccess(MemberExpression a, MemberExpression b)
				=> MemberInfoEqualityComparer.Default.Equals(a.Member, b.Member)
				   && Compare(a.Expression, b.Expression);

			bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
				=> Equals(a.Method, b.Method)
				   && Compare(a.Object, b.Object)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareLambda(LambdaExpression a, LambdaExpression b)
			{
				var n = a.Parameters.Count;

				if (b.Parameters.Count != n)
				{
					return false;
				}

				// all must have same type
				for (var i = 0; i < n; i++)
				{
					if (a.Parameters[i].Type != b.Parameters[i].Type)
					{
						return false;
					}
				}

				var save = _parameterScope;

				_parameterScope = new ScopedDictionary<ParameterExpression, ParameterExpression>(_parameterScope);

				try
				{
					for (var i = 0; i < n; i++)
					{
						_parameterScope.Add(a.Parameters[i], b.Parameters[i]);
					}

					return Compare(a.Body, b.Body);
				}
				finally
				{
					_parameterScope = save;
				}
			}

			bool CompareNew(NewExpression a, NewExpression b)
				=> Equals(a.Constructor, b.Constructor)
				   && CompareExpressionList(a.Arguments, b.Arguments)
				   && CompareMemberList(a.Members, b.Members);

			bool CompareExpressionList(IReadOnlyList<Expression>? a, IReadOnlyList<Expression>? b)
			{
				if (Equals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!Compare(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			static bool CompareMemberList(IReadOnlyList<MemberInfo>? a, IReadOnlyList<MemberInfo>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!Equals(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
				=> CompareExpressionList(a.Expressions, b.Expressions);

			bool CompareExtension(Expression a, Expression b)
			{
				return a.Equals(b);
			}

			bool CompareBlock(BlockExpression a, BlockExpression b)
			{
				return CompareExpressionList(a.Variables, b.Variables) && CompareExpressionList(b.Expressions, a.Expressions);
			}

			bool CompareInvocation(InvocationExpression a, InvocationExpression b)
				=> Compare(a.Expression, b.Expression)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
				   && CompareBindingList(a.Bindings, b.Bindings);

			bool CompareBindingList(IReadOnlyList<MemberBinding>? a, IReadOnlyList<MemberBinding>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!CompareBinding(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareBinding(MemberBinding? a, MemberBinding? b)
			{
				if (a == b)
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.BindingType != b.BindingType)
				{
					return false;
				}

				if (!Equals(a.Member, b.Member))
				{
					return false;
				}

				return a.BindingType switch
				{
					MemberBindingType.Assignment	=> CompareMemberAssignment((MemberAssignment)a, (MemberAssignment)b),
					MemberBindingType.ListBinding	=> CompareMemberListBinding((MemberListBinding)a, (MemberListBinding)b),
					MemberBindingType.MemberBinding => CompareMemberMemberBinding((MemberMemberBinding)a, (MemberMemberBinding)b),
					_                               => throw new NotImplementedException(),
				};
			}

			bool CompareMemberAssignment(MemberAssignment a, MemberAssignment b)
				=> Equals(a.Member, b.Member)
				   && Compare(a.Expression, b.Expression);

			bool CompareMemberListBinding(MemberListBinding a, MemberListBinding b)
				=> Equals(a.Member, b.Member)
				   && CompareElementInitList(a.Initializers, b.Initializers);

			bool CompareMemberMemberBinding(MemberMemberBinding a, MemberMemberBinding b)
				=> Equals(a.Member, b.Member)
				   && CompareBindingList(a.Bindings, b.Bindings);

			bool CompareListInit(ListInitExpression a, ListInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
				   && CompareElementInitList(a.Initializers, b.Initializers);

			bool CompareElementInitList(IReadOnlyList<ElementInit>? a, IReadOnlyList<ElementInit>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null || b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!CompareElementInit(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareElementInit(ElementInit a, ElementInit b)
				=> Equals(a.AddMethod, b.AddMethod)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			sealed class ScopedDictionary<TKey, TValue>
				where TKey : notnull
			{
				readonly ScopedDictionary<TKey, TValue>? _previous;
				readonly Dictionary<TKey, TValue>        _map;

				public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous)
				{
					_previous = previous;
					_map = new Dictionary<TKey, TValue>();
				}

				public void Add(TKey key, TValue value) => _map.Add(key, value);

				public bool TryGetValue(TKey key, out TValue? value)
				{
					for (var scope = this; scope != null; scope = scope._previous!)
					{
						if (scope._map.TryGetValue(key, out value))
						{
							return true;
						}
					}

					value = default;

					return false;
				}
			}
		}
	}
}
