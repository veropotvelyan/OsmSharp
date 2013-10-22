using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OsmSharp.UI.Map;
using OsmSharp.UI.Map.Layers;
using System.Reflection;
using OsmSharp.UI.Renderer;
using OsmSharp.UI.Renderer.Scene;
using System.Timers;
using OsmSharp.Math.Geo;
using OsmSharp.Routing;
using OsmSharp.Routing.CH;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.UI;
using System.Collections.Generic;
using OsmSharp.Math;
using OsmSharp.Math.Geo.Projections;
using OsmSharp.Math.Primitives;
using OsmSharp.Routing.TSP.Genetic;
using OsmSharp.UI.Animations;

namespace OsmSharp.iOS.UI.Sample
{
	public partial class OsmSharp_iOS_UI_SampleViewController : UIViewController
	{
		/// <summary>
		/// Holds the router.
		/// </summary>
		private Router _router;

		/// <summary>
		/// Holds the route layer.
		/// </summary>
		private LayerRoute _routeLayer;

		public OsmSharp_iOS_UI_SampleViewController () : base ("OsmSharp_iOS_UI_SampleViewController", null)
		{
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		public override void LoadView ()
		{
			base.LoadView ();

			// initialize a test-map.
			var map = new Map ();
			map.AddLayer (new LayerScene (Scene2DLayered.Deserialize (
				Assembly.GetExecutingAssembly ().GetManifestResourceStream ("OsmSharp.iOS.UI.Sample.wvl.map"), 
					true)));

			// Perform any additional setup after loading the view, typically from a nib.
			MapView mapView = new MapView ();
			//mapViewAnimator = new MapViewAnimator (mapView);
			mapView.Map = map;
			mapView.MapCenter = new GeoCoordinate(51.158075, 2.961545); // gistel
//			mapView.MapTapEvent+= delegate(GeoCoordinate geoCoordinate) {
//				mapView.AddMarker(geoCoordinate).TouchDown  += MapMarkerClicked;
//			};

			mapView.MapZoom = 18;
			mapView.MapTilt = 30;

			var routingSerializer = new OsmSharp.Routing.CH.Serialization.Sorted.CHEdgeDataDataSourceSerializer(false);
			var graphDeserialized = routingSerializer.Deserialize(
				Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.iOS.UI.Sample.wvl.routing"), true);

			_router = Router.CreateCHFrom(
				graphDeserialized, new CHRouter(graphDeserialized),
				new OsmRoutingInterpreter());

//			long before = DateTime.Now.Ticks;
//
//			var routeLocations = new GeoCoordinate[] {
//				new GeoCoordinate (50.8247730, 2.7524706),
//				new GeoCoordinate (50.8496394, 2.7301512),
//				new GeoCoordinate (50.8927741, 2.6138545),
//				new GeoCoordinate (50.8296363, 2.8869437)
//			};
//
//			var routerPoints = new RouterPoint[routeLocations.Length];
//			for (int idx = 0; idx < routeLocations.Length; idx++) {
//				routerPoints [idx] = router.Resolve (Vehicle.Car, routeLocations [idx]);
//
//				mapView.AddMarker (routeLocations [idx]);
//			}
//			OsmSharp.Routing.TSP.RouterTSPWrapper<RouterTSPAEXGenetic> tspRouter = new OsmSharp.Routing.TSP.RouterTSPWrapper<RouterTSPAEXGenetic> (
//				new RouterTSPAEXGenetic (10, 20), router);
//
//			Route route = tspRouter.CalculateTSP (Vehicle.Car, routerPoints);
//
//			long after = DateTime.Now.Ticks;
//
//			OsmSharp.Logging.Log.TraceEvent("OsmSharp.Android.UI.MapView", System.Diagnostics.TraceEventType.Information,"Routing & TSP in {0}ms", 
//			                                new TimeSpan (after - before).TotalMilliseconds);


			GeoCoordinate point1 = new GeoCoordinate(51.158075, 2.961545);
			GeoCoordinate point2 = new GeoCoordinate(51.190503, 3.004793);
			RouterPoint routerPoint1 = _router.Resolve(Vehicle.Car, point1);
			RouterPoint routerPoint2 = _router.Resolve(Vehicle.Car, point2);
			Route route1 = _router.Calculate(Vehicle.Car, routerPoint1, routerPoint2);
			_enumerator = route1.GetRouteEnumerable(2).GetEnumerator();
			_enumeratorNext = route1.GetRouteEnumerable(2).GetEnumerator();
			for (int idx = 0; idx < 20; idx++) {
				_enumeratorNext.MoveNext ();
				_enumeratorNext.MoveNext ();
				_enumeratorNext.MoveNext ();
			}
//
			_routeLayer = new LayerRoute(map.Projection);
			_routeLayer.AddRoute (route1);
			map.AddLayer(_routeLayer);

			View = mapView;
			

			mapView.AddMarker(new GeoCoordinate(51.1612, 2.9795));
			mapView.AddMarker(new GeoCoordinate(51.1447, 2.9483));

			//mapView.ZoomToMarkers();

			mapView.MapTapEvent += delegate(GeoCoordinate geoCoordinate)
			{
				mapView.ZoomToMarkers();
				//_mapView.AddMarker(geoCoordinate).Click += new EventHandler(MainActivity_Click);
				//mapViewAnimator.Stop();
				//mapViewAnimator.Start(geoCoordinate, 15, new TimeSpan(0, 0, 2));
			};

//			Timer timer = new Timer (150);
//			timer.Elapsed += new ElapsedEventHandler (TimerHandler);
//			timer.Start ();

			mapView.SetNeedsDisplay ();
		}

		private IEnumerator<GeoCoordinate> _enumeratorNext;

		private IEnumerator<GeoCoordinate> _enumerator;

		/// <summary>
		/// Holds the previous router point.
		/// </summary>
		private RouterPoint _previousRouterPoint = null;

		private void MapMarkerClicked(object sender, EventArgs e)
		{
			if (sender is MapMarker) {
				MapMarker marker = (sender as MapMarker);
				lock (_router) {
					RouterPoint resolved = _router.Resolve (Vehicle.Car, marker.Location);
					if (_previousRouterPoint != null) {
						Route route = _router.Calculate (Vehicle.Car, _previousRouterPoint, resolved);
						_routeLayer.Clear ();
						_routeLayer.AddRoute (route, SimpleColor.FromKnownColor (KnownColor.Blue, 75).Value, 16);

						(this.View as IMapView).Invalidate ();
					}
					_previousRouterPoint = resolved;
				}
			}
		}

		private void TimerHandler(object sender, ElapsedEventArgs e)
		{
			this.InvokeOnMainThread (MoveNext);
		}

		private void MoveNext()
		{
			if (_enumerator.MoveNext ()) {
				(this.View as MapView).MapCenter = _enumerator.Current;

				if (_enumeratorNext.MoveNext ()) {
					IProjection projection = (this.View as MapView).Map.Projection;

					VectorF2D direction = new PointF2D(projection.ToPixel(_enumeratorNext.Current)) - 
						new PointF2D(projection.ToPixel(_enumerator.Current));
					
					(this.View as MapView).MapTilt = direction.Angle (new VectorF2D (0, -1));
				}

				(this.View as MapView).SetNeedsDisplay ();
			}
		}

		private void IncreaseMapTilt()
		{
			//(this.View as MapView).MapTilt = (this.View as MapView).MapTilt + 5;
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true;
		}
	}
}