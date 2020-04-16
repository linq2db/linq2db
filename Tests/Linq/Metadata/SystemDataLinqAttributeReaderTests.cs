#if NET45 || NET46
using LinqToDB;
using LinqToDB.Metadata;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Linq;

namespace Tests.Metadata
{
	[TestFixture]
	public class SystemDataLinqAttributeReaderTests : TestBase
	{
		// this is the reduced part of Northwind DB Linq2sql mapping
		[System.Data.Linq.Mapping.Table(Name = "dbo.Shippers")]
		public partial class Shipper : INotifyPropertyChanging, INotifyPropertyChanged
		{

			private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(string.Empty);

			private int _ShipperID;

			private string? _CompanyName;

			private string? _Phone;

			#region Extensibility Method Definitions
			partial void OnLoaded();
			partial void OnValidate(System.Data.Linq.ChangeAction action);
			partial void OnCreated();
			partial void OnShipperIDChanging(int value);
			partial void OnShipperIDChanged();
			partial void OnCompanyNameChanging(string? value);
			partial void OnCompanyNameChanged();
			partial void OnPhoneChanging(string? value);
			partial void OnPhoneChanged();
			#endregion

			public Shipper()
			{
				OnCreated();
			}

			[System.Data.Linq.Mapping.Column(Storage = "_ShipperID", AutoSync = System.Data.Linq.Mapping.AutoSync.OnInsert, DbType = "Int NOT NULL IDENTITY", IsPrimaryKey = true, IsDbGenerated = true)]
			public int ShipperID {
				get {
					return _ShipperID;
				}
				set {
					if (_ShipperID != value) {
						OnShipperIDChanging(value);
						SendPropertyChanging();
						_ShipperID = value;
						SendPropertyChanged("ShipperID");
						OnShipperIDChanged();
					}
				}
			}

			[System.Data.Linq.Mapping.Column(Storage = "_CompanyName", DbType = "NVarChar(40) NOT NULL", CanBeNull = false)]
			public string? CompanyName {
				get {
					return _CompanyName;
				}
				set {
					if (_CompanyName != value) {
						OnCompanyNameChanging(value);
						SendPropertyChanging();
						_CompanyName = value;
						SendPropertyChanged("CompanyName");
						OnCompanyNameChanged();
					}
				}
			}

			[global::System.Data.Linq.Mapping.Column(Storage = "_Phone", DbType = "NVarChar(24)")]
			public string? Phone {
				get {
					return _Phone;
				}
				set {
					if (_Phone != value) {
						OnPhoneChanging(value);
						SendPropertyChanging();
						_Phone = value;
						SendPropertyChanged("Phone");
						OnPhoneChanged();
					}
				}
			}

			public event PropertyChangingEventHandler? PropertyChanging;

			public event PropertyChangedEventHandler?  PropertyChanged;

			protected virtual void SendPropertyChanging()
			{
				PropertyChanging?.Invoke(this, emptyChangingEventArgs);
			}

			protected virtual void SendPropertyChanged(string propertyName)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		[Test]
		public void ParseTableAttribute() {
			var rd     = new SystemDataLinqAttributeReader();
			var attrs = rd.GetAttributes<LinqToDB.Mapping.TableAttribute>(typeof(Shipper), true);

			Assert.NotNull(attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual("Shippers", attrs[0].Name);
			Assert.AreEqual("dbo",      attrs[0].Schema);
		}

		[Test]
		public void SmokeSelect([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from s in db.GetTable<Shipper>()
					orderby s.CompanyName
					select new { s.CompanyName, s.Phone };
				var records = query.ToArray();
			}
		}
	}
}
#endif
