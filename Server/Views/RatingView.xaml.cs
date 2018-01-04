using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for RatingView.xaml
    /// </summary>
    public partial class RatingView : UserControl
    {
        public RatingView()
        {
            InitializeComponent();
            
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            value.Width = Value * ActualWidth;
            base.OnRenderSizeChanged(sizeInfo);    
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            value.Width = Value * ActualWidth;
            base.OnRender(drawingContext);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(RatingView), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged));

        private static void ValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var view = dependencyObject as RatingView;
            if (view == null) return;
            view.value.Width = view.ActualWidth * view.Value;
        }

        public double Value
        {
            get { return (double) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty StarBackgroundProperty = DependencyProperty.Register(
            "StarBackground", typeof(Brush), typeof(RatingView), new PropertyMetadata(default(Brush)));

        public Brush StarBackground
        {
            get { return (SolidColorBrush) GetValue(StarBackgroundProperty); }
            set { SetValue(StarBackgroundProperty, value); }
        }
    }
}
