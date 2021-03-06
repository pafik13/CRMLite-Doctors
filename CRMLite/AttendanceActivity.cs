﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.OS;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Android.Views.InputMethods;
using V4App = Android.Support.V4.App;
using Android.Support.V4.View;

using CRMLite.Dialogs;
using CRMLite.Entities;
using Android.Locations;
using Android.Runtime;
using Android.Net;

namespace CRMLite
{
	[Activity(Label = "AttendanceActivity", ScreenOrientation=ScreenOrientation.Landscape, WindowSoftInputMode=SoftInput.AdjustPan)]
	public class AttendanceActivity : V4App.FragmentActivity, ViewPager.IOnPageChangeListener, ILocationListener
	{
		public const int C_NUM_PAGES = 4;

		ViewPager Pager;
		TextView FragmentTitle;
		Button Close;
		ImageView Contracts;
		ImageView Finance;
		ImageView History;
		ImageView Material;
		IList<Material> Materials;

		LocationManager LocMgr;
		List<Location> Locations;

		string PharmacyUUID;

		DateTimeOffset? AttendanceStart;
		Attendance AttendanceLast;

		public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
		{
			return;
		}

		public void OnPageScrollStateChanged(int state)
		{
			return;
		}

		public void OnPageSelected(int position)
		{
			switch (position) {
				case 0:
					FragmentTitle.Text = @"АПТЕКА";
					break;
				case 1:
					FragmentTitle.Text = @"СОТРУДНИКИ";
					break;
				case 2:
					FragmentTitle.Text = (AttendanceLast == null) || AttendanceStart.HasValue ? 
						@"СОБИРАЕМАЯ ИНФОРМАЦИЯ" : string.Format(@"ИНФОРМАЦИЯ С ВИЗИТА ОТ {0}", AttendanceLast.When.LocalDateTime);
					var w = new Stopwatch();
					w.Start();
					var emp = GetFragment(1);
					if (emp is EmployeeFragment) {
						((EmployeeFragment)emp).SaveAllEmployees();
					}
					var info = GetFragment(2);
					if (info is InfoFragment) {
						((InfoFragment)info).RefreshEmployees();
					}
					w.Stop();
					Console.WriteLine("OnPageSelected: {0}", w.ElapsedMilliseconds);
					break;
				case 3:
					FragmentTitle.Text = (AttendanceLast == null) || AttendanceStart.HasValue ?
						@"ФОТО НА ВИЗИТЕ" : string.Format(@"ФОТО С ВИЗИТА ОТ {0}", AttendanceLast.When.LocalDateTime);
					break;
				default:
					FragmentTitle.Text = @"СТРАНИЦА " + (position + 1);
					break;;
			}
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			RequestWindowFeature(WindowFeatures.NoTitle);
			Window.AddFlags(WindowManagerFlags.KeepScreenOn);

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.activity_screen_slide);

			PharmacyUUID = Intent.GetStringExtra("UUID");
			if (string.IsNullOrEmpty(PharmacyUUID)) return;

			AttendanceLast = MainDatabase.GetAttendaces(PharmacyUUID).OrderByDescending(i => i.When).FirstOrDefault();
			var attendanceLastUUID = AttendanceLast == null ? string.Empty : AttendanceLast.UUID;

			Materials = MainDatabase.GetItems<Material>();

			FragmentTitle = FindViewById<TextView>(Resource.Id.aaTitleTV);
			FragmentTitle.Text = @"АПТЕКА";

			Pager = FindViewById<ViewPager>(Resource.Id.aaContainerVP);
			Pager.AddOnPageChangeListener(this);
			Pager.OffscreenPageLimit = 3;
			Pager.Adapter = new AttendancePagerAdapter(SupportFragmentManager, PharmacyUUID, attendanceLastUUID);

			var btnStartStop = FindViewById<Button>(Resource.Id.aaStartOrStopAttendanceB);
			btnStartStop.Click += (sender, e) =>
			{
				if (!IsLocationActive() || !IsInternetActive()) return;

				if (AttendanceStart == null) {
					AttendanceStart = DateTimeOffset.Now;

					// Location
					Locations = new List<Location>();

					LocMgr = GetSystemService(LocationService) as LocationManager;
					var locationCriteria = new Criteria();
					locationCriteria.Accuracy = Accuracy.Coarse;
					locationCriteria.PowerRequirement = Power.Medium;
					string locationProvider = LocMgr.GetBestProvider(locationCriteria, true);
					System.Diagnostics.Debug.Print("Starting location updates with " + locationProvider);
					LocMgr.RequestLocationUpdates(locationProvider, 5000, 1, this);
					// !Location	

					if (Pager.CurrentItem == 2) {
						FragmentTitle.Text = @"СОБИРАЕМАЯ ИНФОРМАЦИЯ";
					}

					if (Pager.CurrentItem == 3) {
						FragmentTitle.Text = @"ФОТО НА ВИЗИТЕ";
					}

					for (int f = 0; f < C_NUM_PAGES; f++) {
						var fragment = GetFragment(f);
						if (fragment is IAttendanceControl) {
							(fragment as IAttendanceControl).OnAttendanceStart(AttendanceStart);
						}
					}

					Close.Visibility = ViewStates.Invisible;

					Contracts.Visibility = ViewStates.Visible;

					Finance.Visibility = ViewStates.Visible;
					var financeParams = Finance.LayoutParameters as RelativeLayout.LayoutParams;
					financeParams.AddRule(LayoutRules.LeftOf, Resource.Id.aaContractsIV);

					History.Visibility = ViewStates.Visible;
					var historyParams = History.LayoutParameters as RelativeLayout.LayoutParams;
					historyParams.AddRule(LayoutRules.LeftOf, Resource.Id.aaFinanceIV);

					Material.Visibility = ViewStates.Visible;
					var materialParams = Material.LayoutParameters as RelativeLayout.LayoutParams;
					materialParams.AddRule(LayoutRules.LeftOf, Resource.Id.aaHistoryIV);

					var button = sender as Button;
					button.SetBackgroundResource(Resource.Color.Deep_Orange_500);
					button.Text = "ЗАКОНЧИТЬ ВИЗИТ";
					return;
				}

				if ((DateTimeOffset.Now - AttendanceStart.Value).TotalSeconds < 30) return;

				if (CurrentFocus != null) {
					var imm = (InputMethodManager)GetSystemService(InputMethodService);
					imm.HideSoftInputFromWindow(CurrentFocus.WindowToken, HideSoftInputFlags.None);
				}

				var fragmentTransaction = SupportFragmentManager.BeginTransaction();
				var prev = SupportFragmentManager.FindFragmentByTag(LockDialog.TAG);
				if (prev != null)
				{
					fragmentTransaction.Remove(prev);
				}
				fragmentTransaction.AddToBackStack(null);

				var lockDialog = LockDialog.Create("Идет сохранение данных...", Resource.Color.Deep_Orange_500);
				lockDialog.Cancelable = false;
				lockDialog.Show(fragmentTransaction, LockDialog.TAG);

				LocMgr.RemoveUpdates(this);

				new Task(() => {
					Thread.Sleep(2000); // иначе не успеет показаться диалог

					RunOnUiThread(() => {
						var transaction = MainDatabase.BeginTransaction();
						var attendance = MainDatabase.Create2<Attendance>();
						attendance.Pharmacy = PharmacyUUID;
						attendance.When = AttendanceStart.Value;
						attendance.Duration = (DateTimeOffset.Now - AttendanceStart.Value).TotalMilliseconds;

						foreach (var location in Locations) {
							var gps = MainDatabase.Create2<GPSData>();
							gps.Attendance = attendance.UUID;
							gps.Accuracy = location.Accuracy;
							gps.Altitude = location.Altitude;
							gps.Bearing = location.Bearing;
							gps.ElapsedRealtimeNanos = location.ElapsedRealtimeNanos;
							gps.IsFromMockProvider = location.IsFromMockProvider;
							gps.Latitude = location.Latitude;
							gps.Longitude = location.Longitude;
							gps.Provider = location.Provider;
							gps.Speed = location.Speed;
						}

						// Оповещаем фрагменты о завершении визита
						for (int f = 0; f < C_NUM_PAGES; f++) {
							var fragment = GetFragment(f);
							if (fragment is IAttendanceControl) {
								(fragment as IAttendanceControl).OnAttendanceStop(transaction, attendance);
							}
						}

						transaction.Commit();

						//if (lockDialog != null) {
						//	lockDialog.DismissAllowingStateLoss();
						//}

						Finish();
					});
				}).Start();
			};
			// TODO: uncomment
			if (AttendanceLast != null) {
				if (AttendanceLast.When.Date == DateTimeOffset.UtcNow.Date) {
					btnStartStop.Visibility = ViewStates.Gone;
				}
			}

			Close = FindViewById<Button>(Resource.Id.aaCloseB);
			Close.Click += (sender, e) =>
			{
				Finish();
			};

			Contracts = FindViewById<ImageView>(Resource.Id.aaContractsIV);
			Contracts.Click += (sender, e) => {
				var contractActivity = new Intent(this, typeof(ContractActivity));
				contractActivity.PutExtra(ContractActivity.C_PHARMACY_UUID, PharmacyUUID);
				StartActivity(contractActivity);
			};

			Finance = FindViewById<ImageView>(Resource.Id.aaFinanceIV);
			Finance.Click += (sender, e) => {
				var financeActivity = new Intent(this, typeof(FinanceActivity));
				financeActivity.PutExtra(@"UUID", PharmacyUUID);
				financeActivity.PutExtra(FinanceActivity.C_IS_CAN_ADD, false);
				StartActivity(financeActivity);
			};

			History = FindViewById<ImageView>(Resource.Id.aaHistoryIV);
			History.Click += (sender, e) => {
				var historyActivity = new Intent(this, typeof(HistoryActivity));
				historyActivity.PutExtra(@"UUID", PharmacyUUID);
				StartActivity(historyActivity);
			};

			Material = FindViewById<ImageView>(Resource.Id.aaMaterialIV);
			Material.Click += (sender, e) => {
				if (Materials.Count == 0) return;

				var materialItems = new List<MaterialItem>();
				var materialFiles = MainDatabase.GetItems<MaterialFile>();
				foreach (var mf in materialFiles) {
					var file = new Java.IO.File(Helper.MaterialDir, mf.fileName);
					if (file.Exists()) {
						var found = Materials.FirstOrDefault(m => m.uuid == mf.material);
						if (found != null) {
							materialItems.Add(new MaterialItem(found.name, file));
						}
					}
				}

				if (materialItems.Count == 0) return;

				new AlertDialog.Builder(this)
				               .SetTitle("Выберите материал для показа:")
				               .SetCancelable(true)
				               .SetItems(
					               materialItems.Select(item => item.MaterialName).ToArray(), 
					               (caller, arguments) => {
										var intent = new Intent(Intent.ActionView);
									    var uri = Android.Net.Uri.FromFile(materialItems[arguments.Which].FilePath);
										intent.SetDataAndType(uri, "application/pdf");
								   		intent.SetFlags(ActivityFlags.NoHistory);
								   		StartActivity(intent);
							   })
				               .Show();
			};
		}

		void MakeFullScreen()
		{
			View decorView = Window.DecorView;
			var uiOptions = (int)decorView.SystemUiVisibility;
			var newUiOptions = (int)uiOptions;

			newUiOptions |= (int)SystemUiFlags.LayoutStable;
			newUiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
			newUiOptions |= (int)SystemUiFlags.LayoutFullscreen;
			newUiOptions |= (int)SystemUiFlags.HideNavigation;
			newUiOptions |= (int)SystemUiFlags.Fullscreen;
			newUiOptions |= (int)SystemUiFlags.Immersive;

			decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
		}


		bool IsLocationActive()
		{
			var locMgr = GetSystemService(LocationService) as LocationManager;

			if (locMgr.IsProviderEnabled(LocationManager.NetworkProvider)
			  || locMgr.IsProviderEnabled(LocationManager.GpsProvider)
			   ) {
				return true;
			}

			new AlertDialog.Builder(this)
						   .SetTitle(Resource.String.warning_caption)
						   .SetMessage(Resource.String.no_location_provider)
						   .SetCancelable(false)
						   .SetPositiveButton(Resource.String.on_button, delegate {
							   var intent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
							   StartActivity(intent);
						   })
						   .Show();

			return false;
		}

		bool IsInternetActive()
		{
			var cm = GetSystemService(ConnectivityService) as ConnectivityManager;
			if (cm.ActiveNetworkInfo != null) {
				if (cm.ActiveNetworkInfo.IsConnectedOrConnecting) {
					return true;
				}
				new AlertDialog.Builder(this)
							   .SetTitle(Resource.String.error_caption)
							   .SetMessage(Resource.String.no_internet_connection)
							   .SetCancelable(false)
							   .SetNegativeButton(Resource.String.cancel_button, (sender, args) => {
								   if (sender is Dialog) {
									   (sender as Dialog).Dismiss();
								   }
							   })
							   .Show();
				return false;
			}
			new AlertDialog.Builder(this)
						   .SetTitle(Resource.String.warning_caption)
						   .SetMessage(Resource.String.no_internet_provider)
						   .SetCancelable(false)
						   .SetPositiveButton(Resource.String.on_button, delegate {
							   var intent = new Intent(Android.Provider.Settings.ActionWirelessSettings);
							   StartActivity(intent);
						   })
						   .Show();
			return false;
		}

		protected override void OnResume()
		{
			base.OnResume();
			//if (AttendanceStart.HasValue) {
			//	MakeFullScreen();
			//}
		}

		protected override void OnPause()
		{
			base.OnPause();

			// MainDatabase.Dispose();
		}

		protected override void OnStop()
		{
			base.OnStop();
		}

		public override void OnBackPressed()
		{
			if (AttendanceStart.HasValue) return;

			base.OnBackPressed();
		}

		/**
		 * @param containerViewId the ViewPager this adapter is being supplied to
		 * @param id pass in getItemId(position) as this is whats used internally in this class
		 * @return the tag used for this pages fragment
		 */
		public string MakeFragmentName(int containerViewId, long id)
		{
			return "android:switcher:" + containerViewId + ":" + id;
		}

		/**
		 * @return may return null if the fragment has not been instantiated yet for that position - this depends on if the fragment has been viewed
		 * yet OR is a sibling covered by {@link android.support.v4.view.ViewPager#setOffscreenPageLimit(int)}. Can use this to call methods on
		 * the current positions fragment.
		 */
		public V4App.Fragment GetFragment(int position)
		{
			string tag = MakeFragmentName(Pager.Id, position);
			var fragment = SupportFragmentManager.FindFragmentByTag(tag);
			return fragment;
		}

		public void OnLocationChanged(Location location)
		{
			Locations.Add(location);
		}

		public void OnProviderDisabled(string provider)
		{
			System.Diagnostics.Debug.Print(provider + " disabled by user");
		}
		public void OnProviderEnabled(string provider)
		{
			System.Diagnostics.Debug.Print(provider + " enabled by user");
		}
		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			System.Diagnostics.Debug.Print(provider + " availability has changed to " + status);
		}

		/**
		 * A pager adapter that represents <NUM_PAGES> fragments, in sequence.
		 */
		class AttendancePagerAdapter : V4App.FragmentPagerAdapter
		{
			readonly string PharmacyUUID;
			readonly string AttendanceLastUUID;

			public AttendancePagerAdapter(V4App.FragmentManager fm, string pharmacyUUID, string attendanceLastUUID) : base(fm)
			{
				PharmacyUUID = pharmacyUUID;
				AttendanceLastUUID = attendanceLastUUID;
			}

			public override int Count {
				get {
					return C_NUM_PAGES;
				}
			}

			public override V4App.Fragment GetItem(int position)
			{
				switch (position) {
					case 0:
						return PharmacyFragment.create(PharmacyUUID);
					case 1:
						return EmployeeFragment.create(PharmacyUUID);
					case 2:
					return InfoFragment.create(PharmacyUUID, AttendanceLastUUID);
					case 3:
						return PhotoFragment.create(PharmacyUUID, AttendanceLastUUID);
					default:
						return ScreenSlidePageFragment.create(position, false);
				}

			}
		}
	}
}

