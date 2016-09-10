﻿using System;
using System.Linq;
using System.Collections.Concurrent;

using Realms;
using System.IO;

using CRMLite.Entities;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CRMLite
{
	public class MainDatabase
	{
		readonly Realm DB;

		ConcurrentDictionary<SyncItem, SyncItem> SyncDictionary;

		ConcurrentDictionary<string, SyncResult> ResultDictionary;

		//int QueueMaxSize = 20;

		protected static MainDatabase Me;

		static MainDatabase()
		{
			Me = new MainDatabase();
		}

		protected MainDatabase()
		{
			// instantiate the database	
			DB = Realm.GetInstance();

			SyncDictionary = new ConcurrentDictionary<SyncItem, SyncItem>();

			ResultDictionary = new ConcurrentDictionary<string, SyncResult>();
		}

		#region Net
		public static Net GetNet(string uuid)
		{
			return Me.DB.All<Net>().Single(item => item.uuid == uuid);
		}

		public static List<Net> GetNets()
		{
			return Me.DB.All<Net>().ToList();
		}

		public static void SaveNets(List<Net> nets)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in nets)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		internal static IList<T> GetPharmacyDatas<T>(string pharmacyUUID) where T: RealmObject, IPharmacyData
		{
			return Me.DB.All<T>().Where(item => item.Pharmacy == pharmacyUUID).ToList();
		}

		internal static void DeleteDistributions()
		{
			foreach (var item in Me.DB.All<DistributionData>().ToList()) {
				Me.DB.Remove(item);
			}		
		}

		internal static void DeleteAttendancies()
		{
			foreach (var item in Me.DB.All<Attendance>().ToList()) {
				Me.DB.Remove(item);
			}
		}
		#endregion

		#region Position
		public static Position GetPosition(string uuid)
		{
			return Me.DB.All<Position>().Single(item => item.uuid == uuid);
		}

		public static List<Position> GetPositions()
		{
			return Me.DB.All<Position>().ToList();
		}

		public static void SavePositions(List<Position> positions)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in positions)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}
		#endregion

		#region DrugSKU
		public static DrugSKU GetDrugSKU(string uuid)
		{
			return Me.DB.All<DrugSKU>().Single(item => item.uuid == uuid);
		}

		public static List<DrugSKU> GetDrugSKUs()
		{
			return Me.DB.All<DrugSKU>().ToList();
		}

		internal static void DeleteFinanceData()
		{
			foreach (var item in Me.DB.All<FinanceData>().ToList()) {
				Me.DB.Remove(item);
			}
		}

		internal static void DeleteSaleData()
		{
			foreach (var item in Me.DB.All<SaleDataByMonth>().ToList()) {
				Me.DB.Remove(item);
			}
		}

		public static void SaveDrugSKUs(List<DrugSKU> data)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in data)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}
		#endregion

		#region DrugBrand
		public static DrugBrand GetDrugBrand(string uuid)
		{
			return Me.DB.All<DrugBrand>().Single(item => item.uuid == uuid);
		}

		public static List<DrugBrand> GetDrugBrands()
		{
			return Me.DB.All<DrugBrand>().ToList();
		}

		public static void SaveDrugBrands(List<DrugBrand> data)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in data)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

	#endregion

		#region GENERIC
		public static T GetItem<T>(string uuid) where T : RealmObject, IEntiryFromServer
		{
			return Me.DB.All<T>().Single(item => item.uuid == uuid);
		}

		public static T GetEntity<T>(string uuid) where T : RealmObject, IEntity
		{
			return Me.DB.All<T>().Single(item => item.UUID == uuid);
		}

		public static void DeleteEntity<T>(string uuid) where T : RealmObject, IEntity
		{
			using (var trans = Me.DB.BeginWrite()) {
				var item = GetEntity<T>(uuid);

				Me.DB.Remove(item);

				trans.Commit();
			}
		}

		public static void DeleteEntity<T>(Transaction openedTransaction, T item) where T : RealmObject, IEntity
		{
			if (openedTransaction == null) {
				throw new ArgumentNullException(nameof(openedTransaction));
			}
			Me.DB.Remove(item);
		}

		internal static void SaveEntity<T>(Transaction openedTransaction, T item) where T : RealmObject, IEntity
		{
			if (openedTransaction == null) {
				throw new ArgumentNullException(nameof(openedTransaction));
			}

			Me.DB.Manage(item);
		}

		public static List<T> GetItems<T>() where T : RealmObject
		{
			return Me.DB.All<T>().ToList();
		}

		public static void SaveItems<T>(IList<T> data) where T : RealmObject
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in data)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		public static T CreateData<T>(string attendanceUUID) where T : RealmObject, IAttendanceData, IEntity, new()
		{
			var item = Me.DB.CreateObject<T>();
			item.UUID = Guid.NewGuid().ToString();
			item.Attendance = attendanceUUID;
			return item;
		}

		public static T Create<T>(string attendanceUUID) where T : RealmObject, IAttendanceData, IEntity, new()
		{
			var item = new T();
			item.UUID = Guid.NewGuid().ToString();
			item.Attendance = attendanceUUID;
			return item;
		}

		public static T Create<T>() where T : RealmObject, IEntity, new()
		{
			var item = Me.DB.CreateObject<T>();
			item.UUID = Guid.NewGuid().ToString();
			return item;
		}
		#endregion

		public static PhotoData CreatePhoto()
		{
			var photo = Me.DB.CreateObject<PhotoData>();
			photo.UUID = Guid.NewGuid().ToString();
			photo.Stamp = DateTimeOffset.Now;
			return photo;
		}

		internal static void SavePhoto(PhotoData photo)
		{
			Me.DB.Manage(photo);
		}

		public static IList<string> GetStates()
		{
			var list = new List<string>();
			list.Add("Активна");
			list.Add("В резерве");
			list.Add("Закрыта");

			return list;
		}

		public static Subway GetSubway(string uuid)
		{
			return Me.DB.All<Subway>().Single(item => item.uuid == uuid);
		}

		public static List<Subway> GetSubways()
		{
			return Me.DB.All<Subway>().ToList();
		}

		public static void SaveSubways(List<Subway> subways)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in subways)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		public static Region GetRegion(string uuid)
		{
			return Me.DB.All<Region>().Single(item => item.uuid == uuid);
		}

		public static List<Region> GetRegions()
		{
			return Me.DB.All<Region>().ToList();
		}

		public static void SaveRegions(List<Region> regions)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in regions)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		public static Category GetCategory(string uuid)
		{
			return Me.DB.All<Category>().Single(item => item.uuid == uuid);
		}

		public static List<Category> GetCategories(string type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if ((type != "net") && (type != "sell"))
			{
				throw new ArgumentException("Illegal value for parameter", nameof(type));
			}

			return Me.DB.All<Category>().Where(item => item.type == type).ToList();
		}

		public static void SaveCategories(List<Category> categories)
		{
			using (var trans = Me.DB.BeginWrite())
			{
				foreach (var item in categories)
				{
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		public static Place GetPlace(string uuid)
		{
			return Me.DB.All<Place>().Single(item => item.uuid == uuid);
		}

		public static List<Place> GetPlaces()
		{
			return Me.DB.All<Place>().ToList();
		}

		public static void SavePlaces(List<Place> places)
		{
			using (var trans = Me.DB.BeginWrite()) {
				foreach (var item in places) {
					Me.DB.Manage(item);
				}
				trans.Commit();
			}
		}

		#region Sync
		public static void AddToQueue(SyncItem item)
		{
			Me.SyncDictionary[item] = item;
		}

		public static void SaveSyncResult(string objectUUID, SyncResult data)
		{
			Me.ResultDictionary[objectUUID] = data;
		}

		public static SyncResult GetSyncResult(string objectUUID)
		{
			SyncResult result;
			Me.ResultDictionary.TryRemove(objectUUID, out result);
			return result;
		}

		public static ConcurrentDictionary<SyncItem, SyncItem> GetQueue()
		{
			return Me.SyncDictionary;
		}

		public static int GetQueueSize()
		{
			return Me.SyncDictionary.Count();
		}
		#endregion


		public static PresentationData CreatePresentationData(string attendanceUUID)
		{
			var item = Me.DB.CreateObject<PresentationData>();
			item.UUID = Guid.NewGuid().ToString();
			item.Attendance = attendanceUUID;
			return item;
		}

		public static MessageData CreateMessageData(string attendanceUUID)
		{
			var item = Me.DB.CreateObject<MessageData>();
			item.UUID = Guid.NewGuid().ToString();
			item.Attendance = attendanceUUID;
			return item;
		}

		public static void DeletePresentationDatas(IList<PresentationData> data)
		{
			foreach (var item in data)
			{
				Me.DB.Remove(item);
			}
		}

		public static CoterieData CreateCoterieData(string attendanceUUID)
		{
			var item = Me.DB.CreateObject<CoterieData>();
			item.UUID = Guid.NewGuid().ToString();
			item.Attendance = attendanceUUID;
			return item;
		}

		public static void DeleteCoterieDatas(IList<CoterieData> data)
		{
			foreach (var item in data)
			{
				Me.DB.Remove(item);
			}
		}

		#region Employee
		public static Employee CreateEmployee(string pharmacyUUID)
		{
			var employee = Me.DB.CreateObject<Employee>();
			employee.UUID = Guid.NewGuid().ToString();
			employee.Pharmacy = pharmacyUUID;
			return employee;
		}

		public static void DeleteEmployee(Employee employee)
		{
			Me.DB.Remove(employee);
		}

		public static IList<Employee> GetEmployees(string pharmacyUUID)
		{
			return Me.DB.All<Employee>().Where(item => item.Pharmacy == pharmacyUUID).ToList();
		}
		#endregion

		#region Attendance
		public static IList<Attendance> GetAttendaces(string pharmacyUUID)
		{
        	return Me.DB.All<Attendance>().Where(item => item.Pharmacy == pharmacyUUID).ToList();
        }

		public static void SaveAttendace(Attendance attendance)
		{
			Me.DB.Manage(attendance);
		}
		#endregion

		#region Hospital
		//public static Hospital CreateHospital(string pharmacyUUID)
		//{
		//	var hospital = Me.DB.CreateObject<Hospital>();
		//	hospital.UUID = Guid.NewGuid().ToString();
		//	hospital.Pharmacy = pharmacyUUID;
		//	return hospital;
		//}

		//public static void DeleteHospital(Hospital hospital)
		//{
		//	Me.DB.Remove(hospital);
		//}

		//public static IList<Hospital> GetHospitals(string pharmacyUUID)
		//{
		//	return Me.DB.All<Hospital>().Where(item => item.Pharmacy == pharmacyUUID).ToList();
		//}
		#endregion

		public static void ManageQueue()
		{
			SyncItem item;
			foreach (var sync in Me.SyncDictionary)
			{
				if (sync.Value.IsSynced)
				{
					Me.SyncDictionary.TryRemove(sync.Key, out item);
				}
			}
		}

		public static Transaction BeginTransaction()
		{
			return Me.DB.BeginWrite();
		}

		#region Pharmacy
		public static Pharmacy CreatePharmacy()
		{
			var pharmacy = Me.DB.CreateObject<Pharmacy>();
			pharmacy.UUID = Guid.NewGuid().ToString();
			pharmacy.SetState(PharmacyState.psActive);
			return pharmacy;
		}

		public static void DeletePharmacy(Pharmacy pharmacy)
		{
			Me.DB.Remove(pharmacy);
		}

		public static Pharmacy GetPharmacy(string UUID)
		{
			return Me.DB.All<Pharmacy>().Single(item => item.UUID == UUID);
		}

		public static List<Pharmacy> GetPharmacies()
		{
			return Me.DB.All<Pharmacy>().ToList();
		}


		public static IList<Pharmacy> GetPharmacies(int count)
		{
			return Me.DB.All<Pharmacy>()
				     .Take(count)
				     .ToList();
		}

		public static IList<Pharmacy> GetPharmacies(int count, int page)
		{
			return Me.DB.All<Pharmacy>()
				     .Skip(page * count)
				     .Take(count)
				     .ToList();
		}
		#endregion


		internal static IList<DistributionData> GetDistributions(string attendanceUUID)
		{
			return Me.DB.All<DistributionData>()
					 .Where(d => d.Attendance == attendanceUUID)
					 .ToList();
		}

		internal static IList<PresentationData> GetPresentationDatas(string attendanceUUID)
		{
			return Me.DB.All<PresentationData>()
					 .Where(pd => pd.Attendance == attendanceUUID)
					 .ToList();
		}

		// Employee -> Brand -> WorkTypes
		internal static Dictionary<string, Dictionary<string, List<WorkType>>> GetGroupedPresentationDatas(string attendanceUUID)
		{
			var presentations = Me.DB.All<PresentationData>()
			                      .Where(pd => pd.Attendance == attendanceUUID);

			var result = new Dictionary<string, Dictionary<string, List<WorkType>>>();
			foreach (var presentation in presentations) {
				if (result.ContainsKey(presentation.Employee)) {
					if (result[presentation.Employee].ContainsKey(presentation.Brand)) {
						result[presentation.Employee][presentation.Brand].Add(GetItem<WorkType>(presentation.WorkType));
					} else {
						result[presentation.Employee].Add(presentation.Brand, new List<WorkType>());
						result[presentation.Employee][presentation.Brand].Add(GetItem<WorkType>(presentation.WorkType));
					}
				} else {
					result.Add(presentation.Employee, new Dictionary<string, List<WorkType>>());
					result[presentation.Employee].Add(presentation.Brand, new List<WorkType>());
					result[presentation.Employee][presentation.Brand].Add(GetItem<WorkType>(presentation.WorkType));
				}
			}

			return result;
		}


		internal static CoterieDataGrouped GetCoterieDataGrouped(string attendanceUUID)
		{
			var coterieDatas = Me.DB.All<CoterieData>()
			                     .Where(cd => cd.Attendance == attendanceUUID)
			                     .ToList();

			var result = new CoterieDataGrouped(GetEntity<Attendance>(attendanceUUID));

			foreach (var coterieData in coterieDatas) {
				if (!result.Employees.ContainsKey(coterieData.Employee)) {
					result.Employees.Add(coterieData.Employee, GetEntity<Employee>(coterieData.Employee));
				}

				if (!result.Brands.ContainsKey(coterieData.Brand)) {
					result.Brands.Add(coterieData.Brand, GetItem<DrugBrand>(coterieData.Brand));
				}
			}

			return result;
		}

		internal static void DeleteItems<T>() where T : RealmObject
		{
			foreach (var item in Me.DB.All<T>()) {
				Me.DB.Remove(item);
			}
		}

		internal static T GetSingleData<T>(string attendanceUUID) where T : RealmObject, IAttendanceData
		{
			var result = Me.DB.All<T>().Where(item => item.Attendance == attendanceUUID).ToList();

			switch (result.Count()) {
				case 0:
					return null;
				case 1:
					return result[0];
				default:
					throw new Exception("Более чем 1 значение, когда ожидается одно или ни одного.");
			}
		}

		internal static List<T> GetDatas<T>(string attendanceUUID) where T : RealmObject, IAttendanceData
		{
			return Me.DB.All<T>().Where(item => item.Attendance == attendanceUUID).ToList();
		}

		internal static IEnumerable<SaleDataByMonth> GetSaleDatas(string pharmacyUUID, DateTimeOffset[] dates)
		{
			var months = dates.Select(m => m.Month).Distinct();
			var years = dates.Select(m => m.Year).Distinct();
			//var months = new List<int>();
			//months.Add(6);
			//months.Add(7);

			//var years = new List<int>();
			//years.Add(2016);

			return Me.DB.All<SaleDataByMonth>()
				     .Where(sd => sd.Pharmacy == pharmacyUUID)
				     .ToList()
				     .Where(sd => months.Contains(sd.Month) && years.Contains(sd.Year));
		}
	}
}

