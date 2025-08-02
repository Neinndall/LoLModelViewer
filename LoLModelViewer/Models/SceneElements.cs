using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.Models
{
    public static class SceneElements
    {
        public static ModelVisual3D CreateSidePlanes(Func<string, BitmapSource?> loadTextureFunc, Action<string> logErrorFunc)
        {
            Model3DGroup finalGroup = new Model3DGroup();
            double size = 10000; // A large size for the skybox planes

            // 1. Load the shared texture and create the material
            string sidesTexturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sky", "sides.dds");
            BitmapSource? sidesTexture = loadTextureFunc(sidesTexturePath);
            Material sidesMaterial;
            if (sidesTexture != null)
            {
                sidesMaterial = new DiffuseMaterial(new ImageBrush(sidesTexture));
            }
            else
            {
                sidesMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Gray));
                logErrorFunc($"Failed to load sides texture from {sidesTexturePath}. Using solid color fallback.");
            }

            // Load sky_up texture
            string skyUpTexturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sky", "sky_up.dds");
            BitmapSource? skyUpTexture = loadTextureFunc(skyUpTexturePath);
            Material skyUpMaterial;
            if (skyUpTexture != null)
            {
                skyUpMaterial = new DiffuseMaterial(new ImageBrush(skyUpTexture));
            }
            else
            {
                skyUpMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.LightBlue)); // Fallback color
                logErrorFunc($"Failed to load sky_up texture from {skyUpTexturePath}. Using solid color fallback.");
            }

            // 2. Create a single, canonical plane geometry. By default, its front face points towards +Z.
            var planeMesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection()
                {
                    new Point3D(-size, -size, 0), // Bottom-left
                    new Point3D(size, -size, 0),  // Bottom-right
                    new Point3D(size, size, 0),   // Top-right
                    new Point3D(-size, size, 0)    // Top-left
                },
                TriangleIndices = new Int32Collection() { 0, 1, 2, 0, 2, 3 },
                TextureCoordinates = new PointCollection()
                {
                    new System.Windows.Point(0, 1),
                    new System.Windows.Point(1, 1),
                    new System.Windows.Point(1, 0),
                    new System.Windows.Point(0, 0)
                }
            };

            var planeModel = new GeometryModel3D(planeMesh, sidesMaterial);

            // Create a plane model for the top with its specific material
            var topPlaneModel = new GeometryModel3D(planeMesh, skyUpMaterial);

            // 4. Create, transform, and add each plane to the group

            // Back Plane (at z=size, needs to face origin at -Z)
            var backTransform = new Transform3DGroup();
            backTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
            backTransform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, size)));
            var backPlane = planeModel.Clone();
            backPlane.Transform = backTransform;
            finalGroup.Children.Add(backPlane);

            // Front Plane (at z=-size, needs to face origin at +Z)
            var frontPlane = planeModel.Clone();
            frontPlane.Transform = new TranslateTransform3D(0, 0, -size);
            finalGroup.Children.Add(frontPlane);

            // Left Plane (at x=-size, needs to face origin at +X)
            var leftTransform = new Transform3DGroup();
            leftTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            leftTransform.Children.Add(new TranslateTransform3D(new Vector3D(-size, 0, 0)));
            var leftPlane = planeModel.Clone();
            leftPlane.Transform = leftTransform;
            finalGroup.Children.Add(leftPlane);

            // Right Plane (at x=size, needs to face origin at -X)
            var rightTransform = new Transform3DGroup();
            rightTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
            rightTransform.Children.Add(new TranslateTransform3D(new Vector3D(size, 0, 0)));
            var rightPlane = planeModel.Clone();
            rightPlane.Transform = rightTransform;
            finalGroup.Children.Add(rightPlane);

            // Top Plane (at y=size, needs to face origin at -Y)
            var topTransform = new Transform3DGroup();
            topTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90))); // Rotate to face down
            topTransform.Children.Add(new TranslateTransform3D(new Vector3D(0, size, 0))); // Move to top
            var topPlane = topPlaneModel.Clone();
            topPlane.Transform = topTransform;
            finalGroup.Children.Add(topPlane);

            return new ModelVisual3D { Content = finalGroup };
        }

        public static ModelVisual3D CreateGroundPlane(Func<string, BitmapSource?> loadTextureFunc, Action<string> logErrorFunc)
        {
            MeshGeometry3D groundMesh = new MeshGeometry3D();

            // Define vertices for a large square plane (e.g., 800x800 units)
            // Y-coordinate is 0 to place it at the base of the model
            groundMesh.Positions = new Point3DCollection()
            {
                new Point3D(-1600, 0, -1600), // Bottom-left
                new Point3D(1600, 0, -1600),  // Bottom-right
                new Point3D(1600, 0, 1600),   // Top-right
                new Point3D(-1600, 0, 1600)   // Top-left
            };

            // Define triangle indices (two triangles for a square)
            groundMesh.TriangleIndices = new Int32Collection() { 0, 3, 2, 0, 2, 1 };

            // Define texture coordinates (simple mapping for a solid color)
            groundMesh.TextureCoordinates = new PointCollection()
            {
                new System.Windows.Point(0, 1),
                new System.Windows.Point(1, 1),
                new System.Windows.Point(1, 0),
                new System.Windows.Point(0, 0)
            };

            // Load the ground texture
            string groundTexturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Floor", "ground_rift.dds"); // Assuming ground_rift.dds is in the app directory
            BitmapSource? groundTexture = loadTextureFunc(groundTexturePath);

            Material groundMaterial;
            if (groundTexture != null)
            {
                groundMaterial = new DiffuseMaterial(new ImageBrush(groundTexture));
            }
            else
            {
                // Fallback to a solid color if texture loading fails
                groundMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 120, 80))); // Earthy color
                logErrorFunc($"Failed to load ground texture from {groundTexturePath}. Using solid color fallback.");
            }

            // Create the GeometryModel3D and ModelVisual3D
            GeometryModel3D groundModel = new GeometryModel3D(groundMesh, groundMaterial);
            return new ModelVisual3D { Content = groundModel };
        }
    }
}
