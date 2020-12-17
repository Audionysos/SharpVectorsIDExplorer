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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyDrawingDocumentTest {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			//FileSvgReader fr = new FileSvgReader(false, false,
			//	new DirectoryInfo(Environment.CurrentDirectory),
			//	null);

			//var dg = fr.Read("data/test.svg");
			//var d = new SVGExplorer(dg, fr.DrawingDocument);


			var d = new SVGExplorer("data/test.svg");
			Content = d.view;
			d["star"].look.fill = Brushes.Black;
			d["star"].events.mouse.DOWN += (s, e) => {
				var r = d["rect815"];
				r.look.toggleVisibility();
			};
			//var t = d["path821"].view.RenderTransform;
			//d["path821"].view.RenderTransform = new MatrixTransform();

			//var tg = new TransformGroup();
			//tg.v
			//tg.Children.Add(new TranslateTransform());
			d["path821"].transform.pivot.toMassCenter();
			d["rect815"].events.mouse.DOWN += (s, e) => {

				//d["rect815"].transform.rotation += 30;
				//d["path821"].transform.rotation += 30;
				var o = d["path821"];
				o.transform.x += 30;
				if (o.transform.x > 300) o.transform.x = 0;

				o.transform.scale *= 1.5;
				//o.transform.scale = (0.5, 0.7);
				if (o.transform.scale > 3) o.transform.scale = 1;
				o.transform.rotation += 20;


				////d["path821"].look.fill = Brushes.Red;

				//var v = d["path821"].view;
				//var rt = v.RenderTransform;
				//rt.Value.Scale(0.5, 0.5);
				//v.RenderTransform = rt;
				//var m = rt as MatrixTransform;
				//m.Matrix = Matrix.Multiply(rt.Value,
				//	new RotateTransform(30).Value);
				//m.Matrix.
				////m.Matrix.Rotate(30);
				////rt.Value =  new ScaleTransform(0.5, 0.5);
				////v.RenderTransform = new ScaleTransform(0.5, 0.5);
			};
		}
	}

	public class SVGObjectDrawing : FrameworkElement {
		public string name { get; private set; }
		public Drawing drawing { get; private set; }
		private GeometryDrawing g;

		public SVGObjectDrawing(string name, Drawing d) {
			//DesiredSize = new Size(drawing.Bounds.Size);
			this.name = name;
			drawing = d;
			g = d as GeometryDrawing;
			Loaded += (s, e) => { InvalidateVisual(); };

		}

		public Brush brush {
			get => g?.Brush;
			set {
				if (g == null) return;
				g.Brush = value;
			}
		}

		protected override void OnRender(DrawingContext c) {
			c.DrawDrawing(drawing);
		}

		public override string ToString() => name;
	}

	public class MyElement : Grid {
		public Drawing drawing { get; private set; }
		public WpfDrawingDocument document { get; private set; }
		internal Dictionary<string, SVGObjectDrawing> objects
			= new Dictionary<string, SVGObjectDrawing>();

		public MyElement(Drawing drawings, WpfDrawingDocument dd) {
			drawing = drawings;
			document = dd;

			var c = 0;
			foreach (var dn in dd.DrawingNames) {
				if (dn == "layer1") continue;
				var d = dd.GetById(dn);
				var so = new SVGObjectDrawing(dn, d);
				objects.Add(dn, so);
				this.Children.Add(so);
			}
		}

		 public SVGObjectDrawing this[string name]
			=> objects[name];

	}
}
