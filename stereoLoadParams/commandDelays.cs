using System.Collections.Generic;
namespace TelloController
{
    public partial class Program
    {
        static Dictionary<string, int> cmdDelays = new Dictionary<string, int>()
        {
            { "command", 500 },
            { "takeoff", 5000 },
            { "land", 5000 },
            { "up", 7000 },
            { "down", 7000 },
            { "left", 5000 },
            { "go", 7000 },
            { "right", 5000 },
            { "forward", 5000 },
            { "back", 5000 },
            { "cw", 5000 },
            { "ccw", 5000 },
            { "flip", 3000 },
            { "speed", 3000 },
            { "battery?", 500 },
            { "speed?", 500 },
            { "time?", 500 },
            { "rc", 5000 }
        };

        public static Dictionary<string, int> commandDelays { get => cmdDelays; set => cmdDelays = value; }
    }
}