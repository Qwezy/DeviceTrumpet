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
        private bool _isDragging;
        private int _moveLogCount;

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

                // Every mouse-driven position update (thumb or track) is driven by this
                // class via _isDragging, not by Thumb's own built-in drag handling. Only
                // skip the initial snap-to-click-point when starting exactly on the thumb,
                // so grabbing it doesn't cause a tiny unwanted jump before the drag starts.
                _isDragging = true;
                if (!_thumb.IsMouseOver)
                {
                    SetPositionByControlPoint(_lastMousePosition);
                }

                bool captured = CaptureMouse();
                e.Handled = true;

                _moveLogCount = 0;
                System.Diagnostics.Debug.WriteLine($"[VolumeSlider] DOWN onThumb={_thumb.IsMouseOver} captureCallReturned={captured} isMouseCaptured={IsMouseCaptured} Mouse.Captured={Mouse.Captured?.GetType().Name ?? "null"}");
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
            System.Diagnostics.Debug.WriteLine($"[VolumeSlider] UP isDragging={_isDragging} isMouseCaptured={IsMouseCaptured} Mouse.Captured={Mouse.Captured?.GetType().Name ?? "null"}");

            if (_isDragging || IsMouseCaptured)
            {
                // If the point is outside of the control, clear the hover state.
                Rect rcSlider = new Rect(0, 0, ActualWidth, ActualHeight);
                if (!rcSlider.Contains(e.GetPosition(this)))
                {
                    VisualStateManager.GoToState((FrameworkElement)sender, "Normal", true);
                }

                _isDragging = false;
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
            if (_moveLogCount < 8)
            {
                _moveLogCount++;
                System.Diagnostics.Debug.WriteLine($"[VolumeSlider] MOVE isDragging={_isDragging} leftButton={e.LeftButton} isMouseCaptured={IsMouseCaptured} Mouse.Captured={Mouse.Captured?.GetType().Name ?? "null"} pos={e.GetPosition(this)}");
            }

            // Gated on our own _isDragging flag and the physical button state rather than
            // IsMouseCaptured: WPF's routed events still bubble MouseMove up through this
            // element (as an ancestor) even when some descendant control ends up holding
            // the actual mouse capture, so this doesn't need to own capture to keep
            // receiving move events -- it just needs to know a drag is in progress.
            if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
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
            var newValue = Bound((Maximum - Minimum) * percent);

            if (_moveLogCount <= 8)
            {
                System.Diagnostics.Debug.WriteLine($"[VolumeSlider] SETPOS point.X={point.X} ActualWidth={ActualWidth} thumbWidth={thumbWidth} usableWidth={usableWidth} percent={percent} oldValue={Value} newValue={newValue}");
            }

            Value = newValue;

            if (_moveLogCount <= 8)
            {
                double thumbX = -1;
                try { thumbX = _thumb.TransformToAncestor(this).Transform(new Point(0, 0)).X; } catch { }
                System.Diagnostics.Debug.WriteLine($"[VolumeSlider] SETPOS afterSet Value={Value} thumbVisualX={thumbX} thumbActualWidth={_thumb?.ActualWidth}");
            }
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
