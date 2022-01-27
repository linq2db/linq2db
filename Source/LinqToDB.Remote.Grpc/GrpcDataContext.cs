using System;

namespace LinqToDB.Remote.Grpc
{
	public class GrpcDataContext : RemoteDataContextBase
	{
		private readonly string _address;

		#region Init

		public GrpcDataContext(
			string address
			)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				throw new ArgumentException($"'{nameof(address)}' cannot be null or whitespace.", nameof(address));
			}

			_address = address;
		}
		
		#endregion

		#region Overrides

		protected override ILinqClient GetClient()
		{
			return new GrpcLinqServiceClient(_address);
		}

		protected override IDataContext Clone()
		{
			return new GrpcDataContext(_address)
			{
				MappingSchema = MappingSchema,
				Configuration = Configuration
			};
		}

		protected override string ContextIDPrefix => "GrpcRemoteLinqService";

		#endregion
	}
}
