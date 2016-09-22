using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Views;
using Android.Widget;

using CRMLite.Entities;

namespace CRMLite.Adapters
{
	public class AttendanceByWeekAdapter : BaseAdapter<Dictionary<int, int>>
	{
		readonly Activity Context;
        readonly Dictionary<string, Dictionary<int, int>> Items;  // PharmacyUUID - YearWeek (Year * 100 + Week) - Count
        readonly int[] YearWeeks;
		readonly Dictionary<string, string> Matches;

		public AttendanceByWeekAdapter(Activity context, Dictionary<string, Dictionary<int, int>> items, DateTimeOffset[] dates, Dictionary<string, string> matches = null)
		{
			Context = context;
			Items = items;

			YearWeeks = new int[14];
            for (int d = 0; d < 14; d++)
            {
				YearWeeks[d] = dates[d].Year * 100 + Helper.GetIso8601WeekOfYear(dates[d].DateTime);
            }

			Matches = matches;
		}

		public override Dictionary<int, int> this[int position] {
			get {
				return Items[Items.Keys.ElementAt(position)];
			}
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override int Count {
			get {
				return Items.Keys.Count;
			}
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			// Get our object for position
			var key = Items.Keys.ElementAt(position);
			var pharmacy = MainDatabase.GetEntity<Pharmacy>(key);
            var item = Items[key];

            var view = (convertView ?? Context.LayoutInflater.Inflate(Resource.Layout.AttendanceByWeekTableItem, parent, false)
			           ) as LinearLayout; 

            view.FindViewById<TextView>(Resource.Id.abwtiPharmacyTV).Text = pharmacy.GetName();

			view.FindViewById<TextView>(Resource.Id.abwtiWeek1).Text = item[YearWeeks[0]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek2).Text = item[YearWeeks[1]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek3).Text = item[YearWeeks[2]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek4).Text = item[YearWeeks[3]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek5).Text = item[YearWeeks[4]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek6).Text = item[YearWeeks[5]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek7).Text = item[YearWeeks[6]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek8).Text = item[YearWeeks[7]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek9).Text = item[YearWeeks[8]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek10).Text = item[YearWeeks[9]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek11).Text = item[YearWeeks[10]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek12).Text = item[YearWeeks[11]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek13).Text = item[YearWeeks[12]].ToString();
            view.FindViewById<TextView>(Resource.Id.abwtiWeek14).Text = item[YearWeeks[13]].ToString();

            return view;
		}
	}
}

