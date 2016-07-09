﻿using System;
using SD = System.Diagnostics;
using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Widget;

using RestSharp;

using CRMLite.Entities;

namespace CRMLite
{
	[Activity(Label = "SyncActivity")]
	public class SyncActivity : Activity
	{
		Button GetData;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your application here
			SetContentView(Resource.Layout.Sync);

			GetData = FindViewById<Button>(Resource.Id.saGetDataB);

			GetData.Click += GetData_Click;
		}

		void GetData_Click(object sender, EventArgs e)
		{
			var client = new RestClient(@"http://front-sblcrm.rhcloud.com/");

			LoadSubways(client);
			LoadRegions(client);
			LoadPlaces(client);
		}

		void LoadSubways(RestClient client)
		{
			var request = new RestRequest(@"Subway?limit=300", Method.GET);
			var response = client.Execute<List<Subway>>(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				SD.Debug.WriteLine(response.Data.Count);
				MainDatabase.SaveSubways(response.Data);
			}
		}

		void LoadRegions(RestClient client)
		{
			var request = new RestRequest(@"Region?limit=300", Method.GET);
			var response = client.Execute<List<Region>>(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				SD.Debug.WriteLine(response.Data.Count);
				MainDatabase.SaveRegions(response.Data);
			}
		}

		void LoadPlaces(RestClient client)
		{
			var request = new RestRequest(@"Place?limit=300", Method.GET);
			var response = client.Execute<List<Place>>(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				SD.Debug.WriteLine(response.Data.Count);
				MainDatabase.SavePlaces(response.Data);
			}
		}
	}
}
