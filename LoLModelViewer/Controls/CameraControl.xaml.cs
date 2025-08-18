using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using LoLModelViewer.Models;
using LoLModelViewer.Utils;

namespace LoLModelViewer.Controls
{
    public partial class CameraControl : UserControl
    {
        private readonly ModelVisual3D _lights;
        private readonly PointLight _pointLight;
        private readonly ModelVisual3D _sunVisual;

        public static readonly DependencyProperty AmbientBrightnessProperty =
            DependencyProperty.Register("AmbientBrightness", typeof(double), typeof(CameraControl), new PropertyMetadata(0.5, OnAmbientBrightnessChanged));

        public double AmbientBrightness
        {
            get { return (double)GetValue(AmbientBrightnessProperty); }
            set { SetValue(AmbientBrightnessProperty, value); }
        }

        private static void OnAmbientBrightnessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CameraControl)d;
            var group = (Model3DGroup)control._lights.Content;
            var light = (AmbientLight)group.Children[0];
            var color = new ColorConverter().ConvertFrom("#FFFFFF") as Color? ?? Colors.White;
            light.Color = Color.FromArgb(color.A, (byte)(color.R * (double)e.NewValue), (byte)(color.G * (double)e.NewValue), (byte)(color.B * (double)e.NewValue));
        }

        public static readonly DependencyProperty ModelVisualsProperty =
            DependencyProperty.Register("ModelVisuals", typeof(ObservableCollection<ModelVisual3D>), typeof(CameraControl), new PropertyMetadata(null, OnModelVisualsChanged));

        public ObservableCollection<ModelVisual3D> ModelVisuals
        {
            get { return (ObservableCollection<ModelVisual3D>)GetValue(ModelVisualsProperty); }
            set { SetValue(ModelVisualsProperty, value); }
        }

        public static readonly DependencyProperty SelectedModelProperty =
            DependencyProperty.Register("SelectedModel", typeof(SceneModel), typeof(CameraControl), new PropertyMetadata(null));

        public SceneModel SelectedModel
        {
            get { return (SceneModel)GetValue(SelectedModelProperty); }
            set { SetValue(SelectedModelProperty, value); }
        }

        public static readonly DependencyProperty SunXProperty = DependencyProperty.Register("SunX", typeof(double), typeof(CameraControl), new PropertyMetadata(0.0, OnSunPositionChanged));
        public static readonly DependencyProperty SunYProperty = DependencyProperty.Register("SunY", typeof(double), typeof(CameraControl), new PropertyMetadata(200.0, OnSunPositionChanged));
        public static readonly DependencyProperty SunZProperty = DependencyProperty.Register("SunZ", typeof(double), typeof(CameraControl), new PropertyMetadata(300.0, OnSunPositionChanged));

        public double SunX
        {
            get { return (double)GetValue(SunXProperty); }
            set { SetValue(SunXProperty, value); }
        }

        public double SunY
        {
            get { return (double)GetValue(SunYProperty); }
            set { SetValue(SunYProperty, value); }
        }

        public double SunZ
        {
            get { return (double)GetValue(SunZProperty); }
            set { SetValue(SunZProperty, value); }
        }

        private static void OnSunPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CameraControl)d).UpdateSunPosition();
        }

        private void UpdateSunPosition()
        {
            var position = new Point3D(SunX, SunY, SunZ);
            _pointLight.Position = position;
            _sunVisual.Transform = new TranslateTransform3D(position.X, position.Y, position.Z);
        }


        // Internal camera state
        private double _rotationX = -30;
        private double _rotationY = 0;
        private double _cameraDistance = 350; // Initial distance
        private Point3D _cameraTarget = new Point3D(0, 100, 0); // Point camera orbits around
        private double _cameraTranslateX; // For panning
        private double _cameraTranslateY; // For panning

        // Mouse state
        private Point _lastMousePosition;
        private Point _lastPanPosition;
        private Point _lastModelMovePosition; // This might need to be handled differently if model movement is separate

        public CameraControl()
        {
            InitializeComponent();

            var group = new Model3DGroup();
            group.Children.Add(new AmbientLight(Colors.White));
            _pointLight = new PointLight(Colors.White, new Point3D(0, 200, 300));
            group.Children.Add(_pointLight);
            _lights = new ModelVisual3D { Content = group };

            var sphere = new MeshGeometry3D();
            var builder = new MeshBuilder(false, false);
            builder.AddSphere(new Point3D(0,0,0), 10, 12, 12);
            sphere.Positions = builder.Positions;
            sphere.TriangleIndices = builder.TriangleIndices;
            _sunVisual = new ModelVisual3D { Content = new GeometryModel3D(sphere, new EmissiveMaterial(Brushes.White)) };

            MainViewport.Children.Add(_lights);
            MainViewport.Children.Add(_sunVisual);
            UpdateSunPosition();
            RecalculateCamera(); // Initial camera calculation
        }

        private void RecalculateCamera()
        {
            // Calculate camera position based on rotation, distance, and target
            Vector3D initialPositionVector = new Vector3D(0, 0, _cameraDistance);

            // Apply rotations
            Matrix3D rotationMatrix = new Matrix3D();
            rotationMatrix.Rotate(new Quaternion(new Vector3D(1, 0, 0), _rotationX)); // Rotate around X-axis
            rotationMatrix.Rotate(new Quaternion(new Vector3D(0, 1, 0), _rotationY)); // Rotate around Y-axis

            Vector3D rotatedPositionVector = rotationMatrix.Transform(initialPositionVector);

            // Add translation for panning
            Point3D currentTarget = new Point3D(_cameraTarget.X + _cameraTranslateX, _cameraTarget.Y + _cameraTranslateY, _cameraTarget.Z);

            MainCamera.Position = currentTarget + rotatedPositionVector;
            MainCamera.LookDirection = currentTarget - MainCamera.Position;
        }

        private void CameraControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _lastMousePosition = e.GetPosition(this);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _lastPanPosition = e.GetPosition(this);
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _lastModelMovePosition = e.GetPosition(this);
            }
        }

        private void CameraControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMousePosition = e.GetPosition(this);
                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;

                _rotationY += deltaX * 0.2;
                _rotationX -= deltaY * 0.2;

                _lastMousePosition = currentMousePosition;
                RecalculateCamera();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                Point currentPanPosition = e.GetPosition(this);
                double deltaX = currentPanPosition.X - _lastPanPosition.X;
                double deltaY = currentPanPosition.Y - _lastPanPosition.Y;

                // Pan the camera target
                _cameraTranslateX += deltaX * 0.2;
                _cameraTranslateY -= deltaY * 0.2;

                _lastPanPosition = currentPanPosition;
                RecalculateCamera();
            }
            else if (e.MiddleButton == MouseButtonState.Pressed && SelectedModel != null)
            {
                Point currentModelMovePosition = e.GetPosition(this);
                double deltaX = currentModelMovePosition.X - _lastModelMovePosition.X;
                double deltaY = currentModelMovePosition.Y - _lastModelMovePosition.Y;

                var transform = SelectedModel.RootVisual.Transform as TranslateTransform3D ?? new TranslateTransform3D();
                transform.OffsetX += deltaX * 0.2;
                transform.OffsetY -= deltaY * 0.2;
                SelectedModel.RootVisual.Transform = transform;

                _lastModelMovePosition = currentModelMovePosition;
            }
        }

        private void CameraControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _cameraDistance -= e.Delta * 0.1;
            RecalculateCamera();
        }

        private static void OnModelVisualsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CameraControl)d;
            if (e.OldValue is ObservableCollection<ModelVisual3D> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnModelVisualsCollectionChanged;
                control.ClearModelVisuals();
            }
            if (e.NewValue is ObservableCollection<ModelVisual3D> newCollection)
            {
                newCollection.CollectionChanged += control.OnModelVisualsCollectionChanged;
                control.AddModelVisuals(newCollection);
            }
        }

        private void OnModelVisualsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (MainViewport.Children.Contains(item as ModelVisual3D)) continue;
                    MainViewport.Children.Add((ModelVisual3D)item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    MainViewport.Children.Remove((ModelVisual3D)item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearModelVisuals();
            }
        }

        private void ClearModelVisuals()
        {
            for (int i = MainViewport.Children.Count - 1; i >= 0; i--)
            {
                if (MainViewport.Children[i] != _lights && MainViewport.Children[i] != _sunVisual)
                {
                    MainViewport.Children.RemoveAt(i);
                }
            }
        }

        private void AddModelVisuals(ObservableCollection<ModelVisual3D> visuals)
        {
            foreach (var visual in visuals)
            {
                if (MainViewport.Children.Contains(visual)) continue;
                MainViewport.Children.Add(visual);
            }
        }
    }
}