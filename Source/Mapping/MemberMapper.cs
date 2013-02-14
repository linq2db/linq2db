using System;
using System.Data;
using System.Diagnostics;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	public class MemberMapper
	{
		#region Init

		public virtual void Init(MapMemberInfo mapMemberInfo)
		{
			if (mapMemberInfo == null) throw new ArgumentNullException("mapMemberInfo");

			MapMemberInfo          = mapMemberInfo;
			Name                   = mapMemberInfo.Name;
			MemberName             = mapMemberInfo.MemberName;
			Storage                = mapMemberInfo.Storage;
			DbType                 = mapMemberInfo.DbType;
			_type                  = mapMemberInfo.Type;
			MemberAccessor         = mapMemberInfo.MemberAccessor;
			_complexMemberAccessor = mapMemberInfo.ComplexMemberAccessor;
			MappingSchema          = mapMemberInfo.MappingSchema;

			if (Storage != null)
				MemberAccessor = ExprMemberAccessor.GetMemberAccessor(MemberAccessor.TypeAccessor, Storage);
		}

		internal static MemberMapper CreateMemberMapper(MapMemberInfo mi)
		{
			return new DefaultMemberMapper();
		}

		#endregion

		#region Public Properties

		public MappingSchemaOld  MappingSchema  { get; private set; }
		public string         Name           { get; private set; }
		public string         MemberName     { get; private set; }
		public string         Storage        { get; private set; }
		public DbType         DbType         { get; private set; }
		public MapMemberInfo  MapMemberInfo  { get; private set; }
		public int            Ordinal        { get; private set; }
		public MemberAccessor MemberAccessor { get; private set; }

		internal void SetOrdinal(int ordinal)
		{
			Ordinal = ordinal;
		}

		private MemberAccessor _complexMemberAccessor;
		public  MemberAccessor  ComplexMemberAccessor
		{
			[DebuggerStepThrough]
			get { return _complexMemberAccessor ?? MemberAccessor; }
		}

		Type _type;
		public virtual Type Type
		{
			get { return _type; }
		}

		#endregion

		#region Default Members (GetValue, SetValue)

		public virtual object GetValue(object o)
		{
			return MemberAccessor.GetValue(o);
		}

		public virtual void SetValue(object o, object value)
		{
			MemberAccessor.SetValue(o, value);
		}

		public virtual void CloneValue    (object source, object dest)  { MemberAccessor.CloneValue(source, dest); }

		#endregion

		#region Intermal Mappers

		#region Complex Mapper

		internal sealed class ComplexMapper : MemberMapper
		{
			public ComplexMapper(MemberMapper memberMapper)
			{
				_mapper = memberMapper;
			}

			private readonly MemberMapper _mapper;

			#region GetValue

			public override object GetValue(object o)
			{
				var obj = MemberAccessor.GetValue(o);
				return obj == null? null: _mapper.GetValue(obj);
			}

			#endregion

			#region SetValue

			public override void SetValue(object o, object value)
			{
				var obj = MemberAccessor.GetValue(o);

				if (obj != null)
					_mapper.SetValue(obj, value);
			}

			#endregion
		}

		#endregion

		#endregion

		#region MapFrom, MapTo

		protected object MapFrom(object value)
		{
			return MapFrom(value, MapMemberInfo);
		}

		static readonly char[] _trim = { ' ' };

		protected object MapFrom(object value, MapMemberInfo mapInfo)
		{
			if (mapInfo == null) throw new ArgumentNullException("mapInfo");

			if (value == null)
				return mapInfo.NullValue;

			if (mapInfo.Trimmable && value is string)
				value = value.ToString().TrimEnd(_trim);

			if (mapInfo.MapValues != null)
			{
				var comp = (IComparable)value;

				foreach (var mv       in mapInfo.MapValues)
				foreach (var mapValue in mv.MapValues)
				{
					try
					{
						if (comp is string && ((string)comp).Length == 1 && mapValue is char)
						{
							if (((string)comp)[0] == (char)mapValue)
								return mv.OrigValue;
						}
						else if (comp.CompareTo(mapValue) == 0)
							return mv.OrigValue;
					}
					catch
					{
					}
				}
			}

			var valueType  = value.GetType();
			var memberType = mapInfo.Type;

			if (!memberType.IsSameOrParentOf(valueType))
			{
				if (memberType.IsGenericType)
				{
					var underlyingType = Nullable.GetUnderlyingType(memberType);

					if (valueType == underlyingType)
						return value;

					memberType = underlyingType;
				}

				if (memberType.IsEnum)
				{
					var underlyingType = mapInfo.MemberAccessor.UnderlyingType;

					if (valueType != underlyingType)
						//value = _mappingSchema.ConvertChangeType(value, underlyingType);
						return MapFrom(Converter.ChangeType(value, underlyingType, MappingSchema.NewSchema), mapInfo);

					//value = Enum.Parse(type, Enum.GetName(type, value));
					value = Enum.ToObject(memberType, value);
				}
				else
				{
					value = Converter.ChangeType(value, memberType, MappingSchema.NewSchema);
				}
			}

			return value;
		}

		protected object MapTo(object value)
		{
			return MapTo(value, MapMemberInfo);
		}

		protected static object MapTo(object value, MapMemberInfo mapInfo)
		{
			if (mapInfo == null) throw new ArgumentNullException("mapInfo");

			if (value == null)
				return null;

			if (mapInfo.Nullable && mapInfo.NullValue != null)
			{
				try
				{
					var comp = (IComparable)value;

					if (comp.CompareTo(mapInfo.NullValue) == 0)
						return null;
				}
				catch
				{
				}
			}

			if (mapInfo.MapValues != null)
			{
				var comp = (IComparable)value;

				foreach (var mv in mapInfo.MapValues)
				{
					try
					{
						if (comp.CompareTo(mv.OrigValue) == 0)
							return mv.MapValues[0];
					}
					catch
					{
					}
				}
			}

			return value;
		}

		#endregion
	}
}
