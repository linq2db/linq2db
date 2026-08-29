using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Mapping;

using NHibernate;
using NHibernate.Engine;
using NHibernate.Type;
using NHibernate.UserTypes;

namespace LinqToDB.NHibernate
{
	// Bridges a single-column NHibernate IUserType to a linq2db value converter.
	//
	// linq2db expresses a conversion as two expressions over single values (model <-> provider), while an IUserType
	// is imperative: it reads from a DbDataReader and writes to a DbCommand parameter, and may span several columns.
	// For the single-column case the two shapes are reconcilable: drive NullSafeGet/NullSafeSet against a one-value
	// reader and a one-parameter command, and wrap those calls in the expressions linq2db needs.
	//
	// Multi-column user types (including ICompositeUserType, whose NullSafeSet takes a per-column settable[]) have no
	// single value to convert, so they are declined and the member is left unconverted.
	partial class NHMetadataReader
	{
		const string UserTypeColumnName = "c";

		readonly ConcurrentDictionary<MemberInfo, ValueConverterAttribute?> _valueConverterCache = new();

		static readonly MethodInfo _toProviderMethod   = MemberHelper.MethodOf<UserTypeConverterContext>(c => c.ToProvider(null));
		static readonly MethodInfo _fromProviderMethod = MemberHelper.MethodOf<UserTypeConverterContext>(c => c.FromProvider(null));

		/// <summary>
		/// Returns a <see cref="ValueConverterAttribute"/> for a member mapped through a single-column
		/// <see cref="IUserType"/>, or <see langword="null"/> when the member is not user-typed or the user type
		/// cannot be expressed as a single value conversion.
		/// </summary>
		ValueConverterAttribute? BuildValueConverterAttribute(PropInfo prop)
		{
			return _valueConverterCache.GetOrAdd(prop.MemberInfo, _ => CreateValueConverterAttribute(prop));
		}

		ValueConverterAttribute? CreateValueConverterAttribute(PropInfo prop)
		{
			if (prop.PropType is not CustomType customType)
				return null;

			var userType = customType.UserType;

			// Only a single-column user type maps to a single value linq2db can convert.
			var sqlTypes = userType.SqlTypes;
			if (sqlTypes.Length != 1 || prop.ColumnNames.Length != 1)
				return null;

			var modelType = userType.ReturnedType;
			if (modelType == null)
				return null;

			var providerType = DbTypeToClrType(sqlTypes[0].DbType);
			if (providerType == null)
				return null;

			// A nullable provider type lets a user type that maps a value to NULL (and back) flow through.
			if (providerType.IsValueType)
				providerType = typeof(Nullable<>).MakeGenericType(providerType);

			var context      = new UserTypeConverterContext(userType, _sessionFactory);
			var contextConst = Expression.Constant(context);

			// v => (TProvider)context.ToProvider((object)v)
			var toParam  = Expression.Parameter(modelType, "v");
			var toLambda = Expression.Lambda(
				Expression.Convert(
					Expression.Call(contextConst, _toProviderMethod, Expression.Convert(toParam, typeof(object))),
					providerType),
				toParam);

			// v => (TModel)context.FromProvider((object)v)
			var fromParam  = Expression.Parameter(providerType, "v");
			var fromLambda = Expression.Lambda(
				Expression.Convert(
					Expression.Call(contextConst, _fromProviderMethod, Expression.Convert(fromParam, typeof(object))),
					modelType),
				fromParam);

			return new NHValueConverterAttribute(userType.GetType())
			{
				// HandlesNulls stays false: linq2db maps null <-> null itself and only calls the conversion for
				// actual values, which keeps the user type off the null path it would otherwise have to re-handle.
				ValueConverter = new UserTypeValueConverter(toLambda, fromLambda),
			};
		}

		// Drives one user type's reader/command API for a single value.
		//
		// NHibernate's own types reach through the session while binding a parameter (AbstractStringType.Set asks
		// session.Factory.ConnectionProvider.Driver to adjust it), so a session has to be supplied. It is resolved
		// lazily, on the first conversion that needs one, from the session factory the metadata was read from — a
		// stateless session, which acquires no database connection unless something actually queries through it.
		sealed class UserTypeConverterContext
		{
			readonly IUserType                  _userType;
			readonly Lazy<IStatelessSession?>   _session;

			public UserTypeConverterContext(IUserType userType, ISessionFactory? sessionFactory)
			{
				_userType = userType;
				_session  = new Lazy<IStatelessSession?>(() => sessionFactory?.OpenStatelessSession());
			}

			ISessionImplementor? Session => _session.Value?.GetSessionImplementation();

			/// <summary>
			/// Writes the model value through the user type and returns what it put into the command parameter.
			/// </summary>
			public object? ToProvider(object? value)
			{
				var command = new SingleParameterCommand();

				try
				{
					_userType.NullSafeSet(command, value, 0, Session!);
				}
				catch (Exception ex)
				{
					throw new LinqToDBForNHibernateToolsException($"Could not convert a value of NHibernate user type '{_userType.GetType()}' to its database value.", ex);
				}

				var result = command.Parameters[0].Value;
				return result == DBNull.Value ? null : result;
			}

			/// <summary>
			/// Reads the database value through the user type and returns the model value it produced.
			/// </summary>
			public object? FromProvider(object? value)
			{
				var reader = new SingleValueDataReader(value);

				try
				{
					return _userType.NullSafeGet(reader, new[] { UserTypeColumnName }, Session!, null!);
				}
				catch (Exception ex)
				{
					throw new LinqToDBForNHibernateToolsException($"Could not convert a database value to NHibernate user type '{_userType.GetType()}'.", ex);
				}
			}
		}

		static Type? DbTypeToClrType(DbType dbType)
		{
			return dbType switch
			{
				DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.String or DbType.StringFixedLength or DbType.Xml => typeof(string),
				DbType.Boolean                                       => typeof(bool),
				DbType.Byte                                          => typeof(byte),
				DbType.SByte                                         => typeof(sbyte),
				DbType.Int16                                         => typeof(short),
				DbType.Int32                                         => typeof(int),
				DbType.Int64                                         => typeof(long),
				DbType.UInt16                                        => typeof(ushort),
				DbType.UInt32                                        => typeof(uint),
				DbType.UInt64                                        => typeof(ulong),
				DbType.Single                                        => typeof(float),
				DbType.Double                                        => typeof(double),
				DbType.Decimal or DbType.Currency or DbType.VarNumeric => typeof(decimal),
				DbType.Date or DbType.DateTime or DbType.DateTime2   => typeof(DateTime),
				DbType.DateTimeOffset                                => typeof(DateTimeOffset),
				DbType.Time                                          => typeof(TimeSpan),
				DbType.Guid                                          => typeof(Guid),
				DbType.Binary                                        => typeof(byte[]),
				_                                                    => null,
			};
		}

		sealed class UserTypeValueConverter : IValueConverter
		{
			public UserTypeValueConverter(LambdaExpression toProviderExpression, LambdaExpression fromProviderExpression)
			{
				ToProviderExpression   = toProviderExpression;
				FromProviderExpression = fromProviderExpression;
			}

			public bool             HandlesNulls           => false;
			public LambdaExpression FromProviderExpression { get; }
			public LambdaExpression ToProviderExpression   { get; }
		}

		// The base attribute derives its object ID from ConverterType, which is null here (the converter is supplied
		// as an instance), so every user-typed column would otherwise share one ID in linq2db's mapping-schema cache
		// key. Identify the attribute by the user type instead.
		sealed class NHValueConverterAttribute : ValueConverterAttribute
		{
			readonly Type _userType;

			public NHValueConverterAttribute(Type userType)
			{
				_userType = userType;
			}

			public override string GetObjectID() => $".{Configuration}.{_userType.FullName}.";
		}

		// Serves one column holding one value, so a user type's NullSafeGet can read through its usual reader calls.
		sealed class SingleValueDataReader : DbDataReader
		{
			readonly object? _value;

			public SingleValueDataReader(object? value)
			{
				_value = value;
			}

			public override object this[int ordinal]   => GetValue(ordinal);
			public override object this[string name]   => GetValue(GetOrdinal(name));
			public override int    Depth               => 0;
			public override int    FieldCount          => 1;
			public override bool   HasRows             => true;
			public override bool   IsClosed            => false;
			public override int    RecordsAffected     => 0;

			public override bool     IsDBNull(int ordinal)     => _value == null || _value == DBNull.Value;
			public override object   GetValue(int ordinal)     => _value ?? DBNull.Value;
			public override string   GetName(int ordinal)      => UserTypeColumnName;
			public override int      GetOrdinal(string name)   => 0;
			public override Type     GetFieldType(int ordinal) => _value?.GetType() ?? typeof(object);
			public override string   GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

			public override bool     GetBoolean(int ordinal)  => Convert.ToBoolean(_value, CultureInfo.InvariantCulture);
			public override byte     GetByte(int ordinal)     => Convert.ToByte(_value, CultureInfo.InvariantCulture);
			public override char     GetChar(int ordinal)     => Convert.ToChar(_value, CultureInfo.InvariantCulture);
			public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(_value, CultureInfo.InvariantCulture);
			public override decimal  GetDecimal(int ordinal)  => Convert.ToDecimal(_value, CultureInfo.InvariantCulture);
			public override double   GetDouble(int ordinal)   => Convert.ToDouble(_value, CultureInfo.InvariantCulture);
			public override float    GetFloat(int ordinal)    => Convert.ToSingle(_value, CultureInfo.InvariantCulture);
			public override short    GetInt16(int ordinal)    => Convert.ToInt16(_value, CultureInfo.InvariantCulture);
			public override int      GetInt32(int ordinal)    => Convert.ToInt32(_value, CultureInfo.InvariantCulture);
			public override long     GetInt64(int ordinal)    => Convert.ToInt64(_value, CultureInfo.InvariantCulture);
			public override string   GetString(int ordinal)   => Convert.ToString(_value, CultureInfo.InvariantCulture)!;
			public override Guid     GetGuid(int ordinal)     => _value is Guid guid ? guid : new Guid(Convert.ToString(_value, CultureInfo.InvariantCulture)!);

			public override int GetValues(object[] values)
			{
				if (values.Length > 0)
				{
					values[0] = GetValue(0);
					return 1;
				}

				return 0;
			}

			public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
			{
				var bytes = (byte[])GetValue(ordinal);
				if (buffer == null)
					return bytes.Length;

				var count = Math.Min(length, bytes.Length - (int)dataOffset);
				Array.Copy(bytes, dataOffset, buffer, bufferOffset, count);
				return count;
			}

			public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
			{
				var chars = Convert.ToString(_value, CultureInfo.InvariantCulture)!.ToCharArray();
				if (buffer == null)
					return chars.Length;

				var count = Math.Min(length, chars.Length - (int)dataOffset);
				Array.Copy(chars, dataOffset, buffer, bufferOffset, count);
				return count;
			}

			public override bool Read()       => true;
			public override bool NextResult() => false;

			public override IEnumerator GetEnumerator() => new List<object>().GetEnumerator();
		}

		// Holds one parameter, so a user type's NullSafeSet can assign the database value the usual way.
		sealed class SingleParameterCommand : DbCommand
		{
			readonly SingleParameterCollection _parameters = new();

			string _commandText = string.Empty;

			[AllowNull]
			public override string                CommandText         { get => _commandText; set => _commandText = value ?? string.Empty; }
			public override int                   CommandTimeout      { get; set; }
			public override CommandType           CommandType         { get; set; }
			public override bool                  DesignTimeVisible   { get; set; }
			public override UpdateRowSource        UpdatedRowSource   { get; set; }
			protected override DbConnection?      DbConnection        { get; set; }
			protected override DbTransaction?     DbTransaction       { get; set; }
			protected override DbParameterCollection DbParameterCollection => _parameters;

			public override void Cancel()                     { }
			public override void Prepare()                    { }
			public override int  ExecuteNonQuery()            => throw new NotSupportedException();
			public override object? ExecuteScalar()           => throw new NotSupportedException();
			protected override DbParameter CreateDbParameter() => new SingleParameter();

			protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
		}

		sealed class SingleParameterCollection : DbParameterCollection
		{
			readonly List<DbParameter> _parameters = new() { new SingleParameter() };

			public override int    Count    => _parameters.Count;
			public override object SyncRoot => _parameters;

			public override int  Add(object value)              { _parameters.Add((DbParameter)value); return _parameters.Count - 1; }
			public override void AddRange(Array values)         { foreach (var v in values) Add(v!); }
			public override void Clear()                        => _parameters.Clear();
			public override bool Contains(object value)         => _parameters.Contains((DbParameter)value);
			public override bool Contains(string value)         => IndexOf(value) >= 0;
			public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
			public override IEnumerator GetEnumerator()         => _parameters.GetEnumerator();
			public override int  IndexOf(object value)          => _parameters.IndexOf((DbParameter)value);
			public override int  IndexOf(string parameterName)  => _parameters.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.Ordinal));
			public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
			public override void Remove(object value)           => _parameters.Remove((DbParameter)value);
			public override void RemoveAt(int index)            => _parameters.RemoveAt(index);
			public override void RemoveAt(string parameterName) => _parameters.RemoveAt(IndexOf(parameterName));

			protected override DbParameter GetParameter(int index)                                => _parameters[index];
			protected override DbParameter GetParameter(string parameterName)                     => _parameters[IndexOf(parameterName)];
			protected override void SetParameter(int index, DbParameter value)                    => _parameters[index] = value;
			protected override void SetParameter(string parameterName, DbParameter value)         => _parameters[IndexOf(parameterName)] = value;
		}

		sealed class SingleParameter : DbParameter
		{
			public override DbType           DbType                  { get; set; }
			public override ParameterDirection Direction             { get; set; }
			public override bool             IsNullable              { get; set; }
			string _parameterName = string.Empty;
			string _sourceColumn  = string.Empty;

			[AllowNull]
			public override string           ParameterName           { get => _parameterName; set => _parameterName = value ?? string.Empty; }
			public override int              Size                    { get; set; }
			[AllowNull]
			public override string           SourceColumn            { get => _sourceColumn; set => _sourceColumn = value ?? string.Empty; }
			public override bool             SourceColumnNullMapping { get; set; }
			public override object?          Value                   { get; set; }

			public override void ResetDbType() => DbType = DbType.Object;
		}
	}
}
