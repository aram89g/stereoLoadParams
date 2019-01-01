/*---------------------------------------------------------------------------------------------------
 * This class represents the drone's target (X,Y,Z) coordinate.
 ---------------------------------------------------------------------------------------------------*/

namespace cxOF
{
    internal class TargetCoordinate
    {
        static public double X_target = 0;
        static public double Y_target = 0;
        static public double Z_target = 1.5;

        static double GetX()
        {
            return X_target;
        }
        static double GetY()
        {
            return Y_target;
        }
        static double GetZ()
        {
            return Z_target;
        }
    }
}