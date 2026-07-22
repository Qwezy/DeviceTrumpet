using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace EarTrumpet.UI.Controls
{
    public class VolumeSlider : Slider
    {
        public float PeakValue1
        {
            get { return (float)this.GetValue(PeakValue1Property); }
            set { this.SetValue(PeakValue1Property, value); }
        }
        public static readonly DependencyProperty PeakValue1Property = DependencyProperty.Register(
          "PeakValue1", typeof(float), typeof(VolumeSlider), new PropertyMetadata(0f, new PropertyChangedCallback(PeakValueChanged)));

        public float PeakValue2
        {
            get { return (float)this.GetValue(PeakValue2Property); }
            set { this.SetValue(PeakValue2Property, value); }
        }
        public static readonly DependencyProperty PeakValue2Property = DependencyProperty.Register(
          "PeakValue2", typeof(float), typeof(VolumeSlider), new PropertyMetadata(0f, new PropertyChangedCallback(PeakValueChanged)));

        private Border _peakMeter1;
        private Border _peakMeter2;
        private Thumb _thumb;
        private Point _lastMousePosition;
        private bool _isTrackDragging;

        public VolumeSlider() : base()
        {
            PreviewTouchDown += OnTouchDown;
            PreviewMouseDown += OnMouseDown;
            TouchUp += OnTouchUp;
            MouseUp += OnMouseUp;
            TouchMove += OnTouchMove;
            MouseMove += OnMouseMove;
            MouseWheel += OnMouseWheel;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _thumb = (Thumb)GetTemplateChild("SliderThumb");
            _peakMeter1 = (Border)GetTemplateChild("PeakMeter1");
            _peakMeter2 = (Border)GetTemplateChild("PeakMeter2");
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var ret = base.ArrangeOverride(arrangeBounds);
            SizeOrVolumeOrPeakValueChanged();
            return ret;
        }

        private static void PeakValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VolumeSlider)d).SizeOrVolumeOrPeakValueChanged();
        }

        private void SizeOrVolumeOrPeakValueChanged()
        {
            if (_peakMeter1 != null)
            {
                _peakMeter1.Width = Math.Max(0, (ActualWidth - _thumb.ActualWidth) * PeakValue1 * (Value / 100f));
            }

            if (_peakMeter2 != null)
            {
                _peakMeter2.Width = Math.Max(0, (ActualWidth - _thumb.ActualWidth) * PeakValue2 * (Value / 100f));
            }
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            VisualStateManager.GoToState((FrameworkElement)sender, "Pressed", true);

            SetPositionByControlPoint(e.GetTouchPoint(this).Position);
            CaptureTouch(e.TouchDevice);

            e.Handled = true;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _lastMousePosition = e.GetPosition(this);
                VisualStateManager.GoToState((FrameworkElement)sender, "Pressed", true);

                // A click on the track (not the thumb) is tracked and driven entirely by
                // this class. A click on the thumb is left to Thumb's own built-in drag
                // handling, which WPF gives capture priority over us regardless of our own
                // CaptureMouse() call below, since Thumb's class handlers run with
                // handledEventsToo. _isTrackDragging (not IsMouseCaptured) is what OnMouseMove
                // keys off of, so a track drag keeps moving even if WPF hands mouse capture
                // to some other element mid-gesture.
                _isTrackDragging = !_thumb.IsMouseOver;
                if (_isTrackDragging)
                {
                    SetPositionByControlPoint(_lastMousePosition);
                }

                CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            VisualStateManager.GoToState((FrameworkElement)sender, "Normal", true);

            ReleaseTouchCapture(e.TouchDevice);
            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isTrackDragging || IsMouseCaptured)
            {
                // If the point is outside of the control, clear the hover state.
                Rect rcSlider = new Rect(0, 0, ActualWidth, ActualHeight);
                if (!rcSlider.Contains(e.GetPosition(this)))
                {
                    VisualStateManager.GoToState((FrameworkElement)sender, "Normal", true);
                }

                _isTrackDragging = false;
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
                e.Handled = true;
            }
        }

        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            if (AreAnyTouchesCaptured)
            {
                SetPositionByControlPoint(e.GetTouchPoint(this).Position);
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Gated on our own _isTrackDragging flag and the physical button state rather
            // than IsMouseCaptured: WPF's routed events still bubble MouseMove up through
            // this element (as an ancestor) even when some descendant control ends up
            // holding the actual mouse capture, so this doesn't need to own capture to keep
            // receiving move events -- it just needs to know a track drag is in progress.
            if (!_isTrackDragging || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var mousePosition = e.GetPosition(this);
            if (mousePosition != _lastMousePosition)
            {
                _lastMousePosition = mousePosition;
                SetPositionByControlPoint(mousePosition);
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var amount = Math.Sign(e.Delta) * 2.0;
            ChangePositionByAmount(amount);
            e.Handled = true;
        }

        public void SetPositionByControlPoint(Point point)
        {
            // The thumb's center can only travel from thumbWidth/2 to ActualWidth - thumbWidth/2,
            // the same range Track uses -- treating the point as a fraction of the full control
            // width (ignoring the thumb's own width) puts the thumb's center up to thumbWidth/2
            // away from the actual cursor position, worse near the track's edges. That mismatch is
            // what made the thumb appear to "jump" relative to the click/drag point.
            var thumbWidth = _thumb?.ActualWidth ?? 0;
            var usableWidth = ActualWidth - thumbWidth;
            var percent = usableWidth > 0 ? (point.X - thumbWidth / 2) / usableWidth : 0;
            Value = Bound((Maximum - Minimum) * percent);
        }

        public void ChangePositionByAmount(double amount)
        {
            Value = Bound(Value + amount);
        }

        public double Bound(double val)
        {
            return Math.Max(Minimum, Math.Min(Maximum, val));
        }
    }
}
