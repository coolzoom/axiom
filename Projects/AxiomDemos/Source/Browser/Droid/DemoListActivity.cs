﻿using System;
using System.IO;
using System.Reflection;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Axiom.Collections;
using System.Collections.Generic;
using Axiom.Demos;

namespace Droid
{
	public class DemoListActivity : ListActivity
	{
		private readonly string[] demos;

		private readonly string DemoAssembly = /*Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
											   System.IO.Path.DirectorySeparatorChar +*/ @"Axiom.Demos.dll";

		public DemoListActivity(IntPtr handle)
			: base(handle)
		{
			demos = LoadDemos( "Axiom.Demos.dll" );
		}

		protected override void OnCreate( Bundle savedInstanceState )
		{
			base.OnCreate( savedInstanceState );
			ListAdapter = new ArrayAdapter<string>( this, R.layout.demo_item, demos );

			ListView.TextFilterEnabled = true;

			ListView.ItemClick += delegate(AdapterView<object> parent, View view, int position, long id)
			{
				// When clicked, show a toast with the TextView text
				Toast.MakeText(Application, ((TextView)view).Text, Toast.LengthShort).Show();
			};
		}

		public string[] LoadDemos(string DemoAssembly)
		{
			TechDemo test = new CameraTrack();

			Axiom.Collections.AxiomSortedCollection<string, DemoItem> demoList = new Axiom.Collections.AxiomSortedCollection<string, DemoItem>();

			Assembly demos = AppDomain.CurrentDomain.GetAssemblies().Where((assembly) => { return assembly.ManifestModule.Name == DemoAssembly; }).First();
			Type[] demoTypes = demos.GetTypes();
			Type techDemo = demos.GetType("Axiom.Demos.TechDemo");

			foreach (Type demoType in demoTypes)
			{
				if (demoType.IsSubclassOf(techDemo))
				{
					demoList.Add(demoType.Name, new DemoItem(demoType.Name, demoType));
				}
			}
			string[] demoStrings = new string[demoList.Count];
			int i = 0;
			foreach (var demo in demoList)
			{
				demoStrings[i++] = demo.Key;
			}
			return demoStrings ;
		}

		private struct DemoItem
		{
			public DemoItem(string name, Type demo)
			{
				Name = name;
				Demo = demo;
			}

			public string Name;
			public Type Demo;
			public override string ToString()
			{
				return Name;
			}
		}

	}


}

