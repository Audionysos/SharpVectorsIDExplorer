using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyDrawingDocumentTest {

	/// <summary>Allows exploring WPF representation of an SVG image by correspoding "id" attributes.</summary>
	public class SVGExplorer {

		/// <summary>Name associated with the element. This is equivalent of "id" attribute SVG elements. 
		/// The name can be null if the attribute was not set.</summary>
		public string name { get; private set; }
		/// <summary>Stores view associated with this instance, or null if the instance represents a simple drawing.
		/// The <see cref="SVGExplorer"/> hold either a <see cref="drawing"/> or <see cref="view"/>.
		/// This view is actually a <see cref="Grid"/> extension which could be add as WPF control's child.</summary>
		public SVGView view { get; private set; }

		private Events _events;
		/// <summary>Allows to handle events associtead with the element. If this element don't have a <see cref="view"/>, it will be promoted when the propery is accessed.</summary>
		public Events events {
			get {
				promote();
				return _events = _events ?? new Events(view);
				//if (_events != null) return _events;
				//if (view != null) _events = new Events(view);
				//return _events;
			}
			
		}
		private Apparance _app;
		/// <summary>Specifies properties that affect apparance of the element.</summary>
		public Apparance look {
			get {
				return _app = _app ?? new Apparance(this);
			}
		}

		private SVGTransform _trans;
		public SVGTransform transform {
			get {
				promote();
				return _trans = _trans ?? new SVGTransform(this);
			}
		}

		/// <summary>Stores drawing associated with this instance, or null if the instance represents complex view.
		/// The <see cref="SVGExplorer"/> hold either a <see cref="drawing"/> or <see cref="view"/>.</summary>
		public Drawing drawing { get; private set; }
		/// <summary>Parent of this element. If this element is root of the image, the propery will return null.</summary>
		public SVGExplorer parent { get; private set; }
		/// <summary>Root element of SVG image this element is part of.</summary>
		public ExpolorerRoot root { get; private set; }

		private Dictionary<string, SVGExplorer> namesMap
			= new Dictionary<string, SVGExplorer>();
		private List<SVGExplorer> childs = new List<SVGExplorer>();

		#region Initialization
		public SVGExplorer(string svgFile) {
			FileSvgReader fr = new FileSvgReader(false, false,
				new DirectoryInfo(Environment.CurrentDirectory),
				null);
			var dg = fr.Read(svgFile);
			constructFromDrawing(dg, fr.DrawingDocument);
		}

		/// <summary>Creates root element from a given drawing document.</summary>
		/// <param name="d"></param>
		/// <param name="dd"></param>
		public SVGExplorer(Drawing d, WpfDrawingDocument dd) {
			constructFromDrawing(d, dd);
		}

		/// <summary>Creates root element from a given drawing document.</summary>
		private void constructFromDrawing(Drawing d, WpfDrawingDocument dd) {
			drawing = d;
			root = new ExpolorerRoot(dd);
			name = "___SVG_ROOT___";
			initialize();
		}

		private SVGExplorer(Drawing d, SVGExplorer parent,  ExpolorerRoot r) {
			drawing = d;
			root = r;
			this.parent = parent;
			initialize();
		}

		private void initialize() {
			name = name ?? root.getDrawingID(drawing);

			if(drawing is DrawingGroup dg) {
				drawing = null;
				view = new SVGView();
				view.RenderTransform = dg.Transform;
				SVGView v = null; SVGExplorer e = null;
				foreach (var d in dg.Children) {
					var ch = new SVGExplorer(d, this, root);
					//if (d is GlyphRunDrawing grd)
					//	grd.GlyphRun..Characters = "xxx".ToList();
					childs.Add(ch);
					if(ch.name != null) namesMap.Add(ch.name, ch);
					if (ch.drawing != null) {
						view.AddDrawing(ch.drawing);
						if (v != null) ch.promote();
					} else {
						view.Children.Add(ch.view);
						v = ch.view;
					}
				}
			}
		}
		#endregion

		/// <summary>Promotes this <see cref="SVGExplorer"/> form been a simple <see cref="drawing"/> holder to become a <see cref="view"/> holder and be able to handle mouse events, transforms etc.
		/// You probalby should not need to call this method manually, as the element will promote itself when you accesses more advanced properties.</summary>
		public void promote() {
			if (view != null) return;
			var prs = parent.view.RemoveTop(drawing);
			//parent.view.RemoveDrawing(drawing);
			//view = new SVGView();
			//view.AddDrawing(drawing);
			foreach (var cd in prs) {
				var cv = new SVGView();
				cv.AddDrawing(cd);
				if (view == null) view = cv;
				parent.view.Children.Add(cv);

			}
			//parent.view.Children.Add(view);
			drawing = null;
		}

		#region Children access
		public SVGExplorer this[string n] {
			get {
				if(namesMap.TryGetValue(n, out var r)) return r;
				return find(n);
			}
		}

		/// <summary>Searches for element with given name in whole subtree.</summary>
		public SVGExplorer find(string n) {
			if (namesMap.TryGetValue(n, out var r)) return r;
			foreach (var ch in childs) {
				r = ch.find(n);
				if (r != null) return r;
			}return null;
		}
		#endregion

		//TODO: Find my old spiral algoritm
		//private SVGExplorer dig(string n, Search s) {
		//	if(namesMap.TryGetValue(n, out var r)) return r;
		//	if(s == null) { s = new Search(1); }
		//	else {
		//		s.cl++;
		//	}
		//	for (int i = 0; i < childs.Count; i++) {
		//		var ch = childs[i];
		//		r = ch.dig(n, s);
		//	}

		//}

		//private class Search {
		//	public int ml;
		//	public int cl = 0;

		//	public Search(int ml) {
		//		this.ml = ml;
		//	}

		//}
	}

	/// <summary>View that is used by <see cref="SVGExplorer"/> to handle more complex scenarios of SVG elements.</summary>
	public class SVGView : Grid {
		List<Drawing> drawings = new List<Drawing>();
		List<bool> visible = new List<bool>();

		/// <summary>Returns drawing of this view but only if the view contains a single drawing.</summary>
		public Drawing drawing => drawings.Count == 1 ? drawings[0] : null;

		public SVGView() {

		}

		protected override void OnRender(DrawingContext dc) {
			for (int i = 0; i < drawings.Count; i++) {
				if (!visible[i]) continue;
				var d = drawings[i];
				dc.DrawDrawing(d);
			}
			base.OnRender(dc);
		}

		public void AddDrawing(Drawing d) {
			if (d == null) return;
			drawings.Add(d); visible.Add(true); //TODO: !add remove
		}

		public void RemoveDrawing(Drawing d) {
			var di = drawings.IndexOf(d);
			if(di < 0) return;
			drawings.RemoveAt(di);
			visible.RemoveAt(di);
			InvalidateVisual();
		}

		/// <summary>Removes and returns given drawing allong with all drawings which are on top of it.</summary>
		public List<Drawing> RemoveTop(Drawing d) {
			var di = drawings.IndexOf(d);
			if (di < 0) return null;
			var r = new List<Drawing>();
			for (int i = di; i < drawings.Count; i++)
				r.Add(drawings[i]);
			drawings.RemoveRange(di, drawings.Count - di);
			visible.RemoveRange(di, drawings.Count - di);
			return r;
		}


		#region Visibility swiching
		public void SetVisibility(Drawing d, bool value) {
			for (int i = 0; i < drawings.Count; i++) {
				if(drawings[i] == d) {
					visible[i] = value;
					InvalidateVisual();
					return;
				}
			}
		}

		public bool ToogleVisibility(Drawing d) {
			for (int i = 0; i < drawings.Count; i++) {
				if (drawings[i] == d) {
					visible[i] = !visible[i];
					InvalidateVisual();
					return visible[i];
				}
			}return false;
		}

		public bool GetVisibility(Drawing d) {
			for (int i = 0; i < drawings.Count; i++) {
				if (drawings[i] == d) return visible[i];
			}return true;
		}
		#endregion

	}

	public class ExpolorerRoot {
		public WpfDrawingDocument document { get; private set; }
		private Dictionary<Drawing, string> names { get; set; }
			= new Dictionary<Drawing, string>();

		public ExpolorerRoot(WpfDrawingDocument doc) {
			document = doc;
			foreach (var dn in doc.DrawingNames) {
				var d = doc.GetById(dn);
				names.Add(d, dn);
			}
		}

		public string getDrawingID(Drawing d) {
			names.TryGetValue(d, out var n); return n;
		}
	}

	/// <summary>Alters apparence of an <see cref="SVGExplorer"/> instance.</summary>
	public class Apparance {
		public Brush fill {
			get { return get(out GeometryDrawing g, t.drawing, t.view.drawing) ? g.Brush : null; }
			set {
				if (get(out GeometryDrawing g, t.drawing, t.view.drawing)) g.Brush = value;
			}
		}
		public Pen stroke {
			get { return get(out GeometryDrawing g, t.drawing, t.view.drawing) ? g.Pen : null; }
			set { if (get(out GeometryDrawing g, t.drawing, t.view.drawing)) g.Pen = value; }
		}
		/// <summary>Toggles visibility of the element. If the element is simple drawing <see cref="Visibility.Collapsed"/> attibute is not respected.</summary>
		public Visibility visibility {
			get {
				if (t.drawing == null) return t.view.Visibility;
				if (t.parent == null) return Visibility.Hidden;
				return t.parent.view.GetVisibility(t.drawing)
					? Visibility.Visible : Visibility.Hidden;
			}
			set {
				if (t.drawing == null) t.view.Visibility = value;
				else if (t.parent != null) {
					t.parent.view.SetVisibility(t.drawing,
						value == Visibility.Visible
					);
				}

			}
		}

		private bool get<T>(out T o, params Drawing[] drawings) where T : Drawing {
			foreach (var d in drawings) {
				if (d is T) { o = (T)d; return true; }
			}
			o = null; return false;
		}

		/// <summary>Tooggles visibility on/off.</summary>
		public void toggleVisibility() {
			if (t.drawing == null)
				t.view.Visibility = (Visibility)(((byte)t.view.Visibility + 1) % 2);
			else if (t.parent != null)
				t.parent.view.ToogleVisibility(t.drawing);
		}

		private SVGExplorer t;
		/// <summary>Creates new apparance that is bound to given element.</summary>
		/// <param name="target"></param>
		public Apparance(SVGExplorer target) {
			this.t = target;
		}
	}

	public class SVGTransform {
		internal SVGExplorer t;
		private TransformGroup tr;
		private TranslateTransform o = new TranslateTransform();
		//private ScaleTransform s = new ScaleTransform();

		private SVGRotation r = new SVGRotation();
		public SVGRotation rotation {
			get { return r; }
			set { r.angle = value; }
		}

		public double x {
			get => o.X;
			set => o.X = value;
		}

		public double y {
			get => o.Y;
			set => o.Y = value;
		}

		private SVGScale s = new SVGScale();
		public SVGScale scale {
			get => s;
			set => s.set(value.x, value.y);
		}

		private SVGPivot _p;
		public SVGPivot pivot {
			get => _p;
			set {
				_p.set(value.x, value.y);
			}
		}

		public SVGTransform(SVGExplorer target) {
			this.t = target;
			createTransform();
		}

		private void createTransform() {
			_p = new SVGPivot(this);
			tr = new TransformGroup();
			tr.Children = new TransformCollection() {
				s,
				r,
				o,
			};
			var cm = t.view.RenderTransform.Value;

			var tp = cm.Transform(new Point(0, 1));
			r.angle = 180 / Math.PI * Math.Atan2(tp.X, tp.Y);

			o.X = cm.OffsetX;
			o.Y = cm.OffsetY;

			s.x = Math.Sqrt(cm.M11 * cm.M11 + cm.M21 * cm.M21);
			s.y = Math.Sqrt(cm.M12 * cm.M12 + cm.M22 * cm.M22);

			//tr.Value.Append(t.view.RenderTransform.Value);
			t.view.RenderTransform = tr;

			_p.toBoundsCenter();
		}

		/// <summary>False if null.</summary>
		public static implicit operator bool(SVGTransform t) => t!=null;
	}

	public class SVGScale {
		private ScaleTransform s = new ScaleTransform();
		public ScaleTransform transform => s;

		public double x {
			get => s.ScaleX;
			set => s.ScaleX = value;
		}

		public double y {
			get => s.ScaleY;
			set => s.ScaleY = value;
		}

		public SVGScale() { }

		public SVGScale(double x, double y) {
			set(x, y);
		}

		public void set(double x, double y) {
			this.x = x; this.y = y;
		}

		public static implicit operator ScaleTransform(SVGScale s)
			=> s?.s;

		public static implicit operator SVGScale(double v)
			=> new SVGScale(v, v);

		public static implicit operator double(SVGScale s)
			=> Math.Max(s.x, s.y);

		public static implicit operator SVGScale((double x, double y) t)
			=> new SVGScale(t.x, t.y);

	}

	public class SVGRotation {
		private RotateTransform r = new RotateTransform();
		internal RotateTransform transform => r;

		public double angle {
			get => r.Angle;
			set => r.Angle = value;
		}

		public SVGRotation() { }

		public SVGRotation(double degrees) {
			angle = degrees;
		}


		public static implicit operator RotateTransform(SVGRotation s)
			=> s?.r;

		public static implicit operator SVGRotation(double v)
			=> new SVGRotation(v);

		public static implicit operator double(SVGRotation s)
			=> s != null ? s.angle : double.NaN;
	}

	public class SVGPivot {
		private SVGTransform t;

		private double _x;
		public double x {
			get => _x;
			set {
				_x = value;
				if (!t) return;
				t.scale.transform.CenterX = _x;
				t.rotation.transform.CenterX = _x;
			}
		}

		private double _y;
		public double y {
			get => _y;
			set {
				_y = value;
				if (!t) return;
				t.scale.transform.CenterY = _y;
				t.rotation.transform.CenterY = _y;
			}
		}

		public SVGPivot(SVGTransform t) {
			this.t = t;
		}
		public SVGPivot() { }
		public SVGPivot(double x, double y) {
			set(x, y);
		}

		public void set(double x, double y) {
			this.x = x; this.y = y;
		}

		public void toBoundsCenter() {
			var bb = t.t.view.drawing.Bounds;
			x = bb.Left + bb.Width * .5;
			y = bb.Top + bb.Height * .5;
		}

		/// <summary>Sets the pivot to center of the mass of the drawing.
		/// By defualt the pivot is set to center of AABB of the drawing, which is not always the best if you want to for example rotate in place a star.
		/// However this method is more expensive to calculate. For regural objects, such us circle or rectangle, both centers should match and calling this method for them is a waste.
		/// Right now this method only works on simple objects containing single <see cref="GeometryDrawing"/>, calling this on parent objects will do nothing.</summary>
		public void toMassCenter() {
			var d = t.t.view.drawing;
			var gd = d as GeometryDrawing;
			if (gd == null) return;
			var g = gd.Geometry;
			var p = g.GetOutlinedPathGeometry(0.001, ToleranceType.Relative);
			//TODO: remove dummy avaraging to prevent an overflow
			double x = 0, y = 0; int pc = 0;
			foreach (var f in p.Figures) {
				foreach (var s in f.Segments) {
					PointCollection points = new PointCollection();
					if (s is PolyBezierSegment b) { 
						points = b.Points; //not sure if this are proper points on path, just copied this form codeproject.
						 //ps. pretty sure we need to render the curve after seeing example https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.polybeziersegment?view=net-5.0
					} else if (s is LineSegment l) {
						points.Add(l.Point);
					} else if (s is PolyLineSegment pl) {
						points = pl.Points;
					}
					foreach (var pt in points) {
						x += pt.X; y += pt.Y;
					}
					pc += points.Count;
				}
			}
			this.x = x / pc;
			this.y = y / pc;
		}

		public static implicit operator SVGPivot(double v)
			=> new SVGPivot(v, v);

		public static implicit operator double(SVGPivot s)
			=> Math.Max(s.x, s.y);

		public static implicit operator SVGPivot((double x, double y) t)
			=> new SVGPivot(t.x, t.y);
	}

	#region Events
	public class Events {
		private MouseEvents _mouse;
		/// <summary>Events associated with mouse input.</summary>
		public MouseEvents mouse => _mouse = _mouse ?? new MouseEvents(target);

		private FrameworkElement target;
		public Events(FrameworkElement e) {
			target = e;
		}
	}

	public class MouseEvents {
		private static FrameworkElement dummy = new FrameworkElement();
		private FrameworkElement target = dummy;

		public event MouseButtonEventHandler DOWN {
			add => target.MouseDown += value;
			remove => target.MouseDown -= value;
		}

		public event MouseButtonEventHandler UP {
			add => target.MouseUp += value;
			remove => target.MouseUp -= value;
		}

		public MouseEvents(FrameworkElement target) {
			this.target = target;
		}
	}

	public class KeyboardEvents {
		private static FrameworkElement dummy = new FrameworkElement();
		private FrameworkElement target = dummy;

		public event KeyEventHandler DOWN {
			add => target.KeyDown += value;
			remove => target.KeyUp -= value;
		}

		public event KeyEventHandler UP {
			add => target.KeyDown += value;
			remove => target.KeyDown -= value;
		}

		public KeyboardEvents(FrameworkElement target) {
			this.target = target;
		}
	}
	#endregion
}
