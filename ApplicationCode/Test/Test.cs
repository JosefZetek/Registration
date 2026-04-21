using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Test;
    
public class Test
{
    public Transform3D t;
    public double alpha;
    public Point3D pointMicro;
    public Point3D pointMacro;
    public double similarity;

    public Test(Transform3D t, double a)
    {
        this.t = t;
        this.alpha = a;
    }
    public Test(Transform3D t, double a, Point3D p)
    {
        this.t = t;
        this.alpha = a;
        this.pointMicro = p;
    }

    public Test(Transform3D t, double a, Point3D p, Point3D pM, double s)
    {
        this.t = t;
        this.alpha = a;
        this.pointMicro = p;
        this.pointMacro = pM;
        this.similarity = s;
    }
}