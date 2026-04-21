namespace Registration.ApplicationCode.Other;

public class CutResolution
{
    private int width;
    private int height;

    public CutResolution()
    {
        width = 0;
        height = 0;
    }

    public CutResolution(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public int Width { get => width; set => width = (value > 0) ? value : width; }
    public int Height { get => height; set => height = (value > 0) ? value : height; }
}
