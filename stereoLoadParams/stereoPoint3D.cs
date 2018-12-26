/*---------------------------------------------------------------------------------------------------
 * This class represents a 3D point that it's (X,Y,Z) coordinates are calculated using Stereo Vision.
 ---------------------------------------------------------------------------------------------------*/
public class StereoPoint3D
{
        private static double T_3d = 0.12; // length between cameras[m]
        private static double f = 0.004; // Focal Length = 4mm for "LOGITECH HD WEBCAM C270"
        private static double X_3d = 0.0;
        private static double Y_3d = 0.0;
        private static double Z_3d = 0.0;
        private static double x_pixel_left_camera = 0.0;
        private static double x_pixel_right_camera = 0.0;
        private static double x_pixels_amount = 640.0; // image resulotion
        private static double y_pixels_amount = 480.0; // image resulotion
        private static double ox = x_pixels_amount / 2; // width = 1980 => x_pixel_center = 1980/2  assumes pixels start from 0
        private static double oy = y_pixels_amount / 2; // height = 1080 => y_pixel_center = 1080/2 assumes pixels start from 0
        private static double x_width_length = (2 / (1.732050808)) * f;
        private static double y_width_length = x_width_length;
        private static double sx = x_width_length / x_pixels_amount;
        private static double sy = y_width_length / y_pixels_amount;
        private static double x_1 = 0.0;
        private static double x_2 = 0.0;
        private static double y_1 = 0.0;

        public void CalculateCoordinate3D(double lX, double rX, double Y)
        {
            x_pixel_left_camera = rX;
            x_pixel_right_camera = lX;
            x_1 = (x_pixel_left_camera - ox) * sx;
            x_2 = (x_pixel_right_camera - ox) * sx;
            y_1 = (Y - oy) * sy;
            Z_3d = (T_3d * f) / (x_1 - x_2);
            X_3d = x_1 * (Z_3d / f);
            Y_3d = y_1 * (Z_3d / f);
        }

        public double GetX3D()
        {
            return X_3d;
        }
        public double GetY3D()
        {
            return Y_3d;
        }
        public double GetZ3D()
        {
            return Z_3d;
        }

    }
