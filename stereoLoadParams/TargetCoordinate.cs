/*---------------------------------------------------------------------------------------------------
 * This class represents the drone's target (X,Y,Z) coordinate.
 ---------------------------------------------------------------------------------------------------*/

public class Point3D
{
    private double X_target;
    private double Y_target;
    private double Z_target;
    public bool arrived;

    public Point3D(double x, double y, double z)
    {
        X_target = x;
        Y_target = y;
        Z_target = z;
        arrived = false;
    }
    public Point3D(Point3D obj)
    {
        X_target = obj.GetX();
        Y_target = obj.GetY();
        Z_target = obj.GetZ();
        arrived = obj.arrived;
    }

    public double GetX()
    {
        return X_target;
    }
    public double GetY()
    {
        return Y_target;
    }
    public double GetZ()
    {
        return Z_target;
    }
    public void SetZ(double z)
    {
        Z_target = z;
    }
}
