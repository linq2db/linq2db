﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Mapping
{
	using Expressions;
	using Metadata;

	/// <summary>
	/// Fluent mapping builder.
	/// </summary>
	public class FluentMappingBuilder
	{
		#region Init

		/// <summary>
		/// Creates fluent mapping builder for specified mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema.</param>
		public FluentMappingBuilder(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			MappingSchema.AddMetadataReader(_reader);
		}

		/// <summary>
		/// Gets builder's mapping schema.
		/// </summary>
		public MappingSchema MappingSchema { get; }

		readonly FluentMetadataReader _reader = new ();

		#endregion

		#region GetAtributes

		/// <summary>
		/// Gets attributes of type <typeparamref name="T"/>, applied to specified type.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Type with attributes.</param>
		/// <returns>Returns attributes of specified type, applied to <paramref name="type"/>.</returns>
		public T[] GetAttributes<T>(Type type)
			where T : Attribute
		{
			return _reader.GetAttributes<T>(type, false);
		}

		/// <summary>
		/// Gets attributes of type <typeparamref name="T"/>, applied to specified member. Search for member in specified
		/// type or it's parents.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Member owner type.</param>
		/// <param name="memberInfo">Member descriptor.</param>
		/// <returns>Returns attributes of specified type, applied to <paramref name="memberInfo"/>.</returns>
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo)
			where T : Attribute
		{
			return _reader.GetAttributes<T>(type, memberInfo);
		}

		#endregion

		#region HasAtribute

		/// <summary>
		/// Adds mapping attribute to specified type.
		/// </summary>
		/// <param name="type">Target type.</param>
		/// <param name="attribute">Mapping attribute to add to specified type.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(Type type, MappingAttribute attribute)
		{
			_reader.AddAttribute(type, attribute);
			MappingSchema.ResetID();
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to specified type.
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="attribute">Mapping attribute to add to specified type.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute<T>(MappingAttribute attribute)
		{
			_reader.AddAttribute(typeof(T), attribute);
			MappingSchema.ResetID();
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to specified member.
		/// </summary>
		/// <param name="memberInfo">Target member.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(MemberInfo memberInfo, MappingAttribute attribute)
		{
			_reader.AddAttribute(memberInfo, attribute);
			MappingSchema.ResetID();
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(LambdaExpression func, MappingAttribute attribute)
		{
			var memberInfo = MemberHelper.GetMemberInfo(func);
			_reader.AddAttribute(memberInfo, attribute);
			MappingSchema.ResetID();
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <typeparam name="T">Type of labmda expression parameter.</typeparam>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute<T>(Expression<Func<T,object?>> func, MappingAttribute attribute)
		{
			var memberInfo = MemberHelper.MemberOf(func);
			_reader.AddAttribute(memberInfo, attribute);
			MappingSchema.ResetID();
			return this;
		}

		#endregion

		/// <summary>
		/// Creates entity builder for specified mapping type.
		/// </summary>
		/// <typeparam name="T">Mapping type.</typeparam>
		/// <param name="configuration">Optional mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.</param>
		/// <returns>Returns entity fluent mapping builder.</returns>
		public EntityMappingBuilder<T> Entity<T>(string? configuration = null)
		{
			return new (this, configuration);
		}
	}
}
