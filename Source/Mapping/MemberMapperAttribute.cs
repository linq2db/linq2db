using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(
		AttributeTargets.Class    | AttributeTargets.Interface | 
		AttributeTargets.Property | AttributeTargets.Field,
		AllowMultiple=true)]
	public class MemberMapperAttribute : MapImplicitAttribute
	{
		public MemberMapperAttribute(Type memberMapperType)
			: this(null, memberMapperType)
		{
		}

		public MemberMapperAttribute(Type memberType, Type memberMapperType)
		{
			if (memberMapperType == null) throw new ArgumentNullException("memberMapperType");

			_memberType       = memberType;
			_memberMapperType = memberMapperType;
		}

		private readonly Type _memberType;
		public           Type  MemberType
		{
			get { return _memberType; }
		}

		private readonly Type _memberMapperType;
		public           Type  MemberMapperType
		{
			get { return _memberMapperType; }
		}

		public virtual MemberMapper  MemberMapper
		{
			get
			{
				var mm = Activator.CreateInstance(_memberMapperType) as MemberMapper;

				if (mm == null)
					throw new ArgumentException(
						string.Format("Type '{0}' is not MemberMapper.", _memberMapperType));

				mm.IsExplicit = true;

				return mm;
			}
		}
	}
}
