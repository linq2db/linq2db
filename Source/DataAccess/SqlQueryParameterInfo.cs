using LinqToDB.Mapping;

namespace LinqToDB.DataAccess
{
	public class SqlQueryParameterInfo
	{
		private string _parameterName;
		public  string  ParameterName
		{
			get { return _parameterName;  }
			set { _parameterName = value; }
		}
	
		private string _fieldName;
		public  string  FieldName
		{
			get { return _fieldName;  }
			set { _fieldName = value; }
		}

		private MemberMapper _memberMapper;
		public  MemberMapper  MemberMapper
		{
			get { return _memberMapper; }
		}

        //NOTE Changed internal to public and added virtual
		public virtual void SetMemberMapper(ObjectMapper objectMapper)
		{
			_memberMapper = objectMapper[_fieldName];
		}
	}
}
