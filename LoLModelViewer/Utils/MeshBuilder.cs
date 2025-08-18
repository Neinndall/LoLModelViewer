using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.Utils
{
    public class MeshBuilder
    {
        private readonly Point3DCollection positions = new Point3DCollection();
        private readonly Int32Collection triangleIndices = new Int32Collection();

        public Point3DCollection Positions { get { return positions; } }
        public Int32Collection TriangleIndices { get { return triangleIndices; } }

        public MeshBuilder(bool createNormals, bool createTextureCoordinates)
        {
        }

        public void AddSphere(Point3D center, double radius, int slices, int stacks)
        {
            for (int stack = 0; stack <= stacks; stack++)
            {
                double phi = Math.PI / 2 - stack * Math.PI / stacks;
                double y = radius * Math.Sin(phi);
                double scale = -radius * Math.Cos(phi);

                for (int slice = 0; slice <= slices; slice++)
                {
                    double theta = slice * 2 * Math.PI / slices;
                    double x = scale * Math.Sin(theta);
                    double z = scale * Math.Cos(theta);

                    positions.Add(new Point3D(x + center.X, y + center.Y, z + center.Z));
                }
            }

            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int i0 = stack * (slices + 1) + slice;
                    int i1 = (stack + 1) * (slices + 1) + slice;
                    int i2 = (stack + 1) * (slices + 1) + (slice + 1);
                    int i3 = stack * (slices + 1) + (slice + 1);

                    triangleIndices.Add(i0);
                    triangleIndices.Add(i1);
                    triangleIndices.Add(i3);

                    triangleIndices.Add(i1);
                    triangleIndices.Add(i2);
                    triangleIndices.Add(i3);
                }
            }
        }
    }
}