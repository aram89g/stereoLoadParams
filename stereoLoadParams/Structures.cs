using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

struct Video_Device
{
    public string Device_Name;
    public int Device_ID;

    public Video_Device(int ID, string Name)
    {
        Device_ID = ID;
        Device_Name = Name;
    }

    /// <summary>
    /// Represent the Device as a String
    /// </summary>
    /// <returns>The string representation of this color</returns>
    public override string ToString()
    {
        return String.Format("[{0}] {1}", Device_ID, Device_Name);
    }
}