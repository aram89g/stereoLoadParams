//using System;
//using System.Linq;
//using System.Threading;
//using System.Net.Sockets;
//using Emgu.CV;

//namespace cxOF
//{
//    public class CxOF
//    {
//        private readonly string IP = "192.168.0.1"; // drone IP
//        private readonly int Port = 40000; // drone UDP port
//        private UdpClient udpClient = new UdpClient();
//        private byte[] headerNoHandshake = { 0x63, 0x63, 0x01, 0x00, 0x00, 0x00, 0x00 }; //first 7 bytes of every payload
//        private byte[] header = { 0x63, 0x63, 0x0a, 0x00, 0x00, 0x0a, 0x00, 0xcc }; // first byte of every payload
//        private readonly byte footer = 0x33; // last byte of every payload

//        private int right_left_mov = 128;
//        private int forward_back = 128;
//        private int rotate_left_right = 128;
//        private int up_down = 110; //Change it to set the drone altitude
//        private int und1 = 0;   //undefined bytes
//        private int und2 = 0;   //undefined bytes      
//        private int crc = 0;    //crc calculated from all bytes xor product
//        private int mode = 0;   // 0 = idle, 1 = takeoff, 2 = land

//        static int MIN = 0;
//        static int MAX = 255;
//        static int step = 5;

//        double delta = 0.1;
//        private byte[] message;

//        PIDControllers.PIDController pidX = new PIDControllers.PIDController();
//        PIDControllers.PIDController pidY = new PIDControllers.PIDController();
//        PIDControllers.PIDController pidZ = new PIDControllers.PIDController();

//        static double MinX = 128-10;
//        static double MaxX = 128+10;
//        static double MinY = 120-10;
//        static double MaxY = 120+20;
//        static double MinZ = 128-10;
//        static double MaxZ = 128+10;

//        static double min = -0.20; // [m\sec] maximum speed for pid output
//        static double max = 0.20; // [m\sec] minimum speed for pid output

//        double stateX;
//        double stateY;
//        double stateZ;



//        double Tix = 200;
//        double Tdx = 7;
//        double Kpx = (1.0 / 7)*3;
//        double Kix = 0;
//        double Kdx = 0;

//        double Tiy = 10;
//        double Tdy = 5;
//        double Kpy = (1.0 / 7) * 4;
//        double Kiy = 0;
//        double Kdy = 0;

//        double Tiz = 100;
//        double Tdz = 1;
//        double Kpz = (1.0 / 7) * 6;
//        double Kiz = 0;
//        double Kdz = 0;

//        //double Kp = 0.15;
//        //double Ki = 0.02;
//        //double Kd = 0.005;

//        public CxOF()
//        {
//            // connect to drone access point
//            udpClient.Connect(IP, Port);

//            // initialize PID constants
//            Kix = Kpx / Tix;
//            Kdx = Kpx * Tdx;
//            Kiy = Kpy / Tiy;
//            Kdy = Kpy * Tdy;
//            Kiz = Kpz / Tiz;
//            Kdz = Kpz * Tdz;
//            pidX.Kp = Kpx;
//            pidX.Ki = Kix;
//            pidX.Kd = Kdx;
//            pidX.MaxOutput = max;
//            pidX.MinOutput = min;
//            pidX.MaxInput = 0.4;
//            pidX.MinInput = -0.4;
//            //pidX.Tolerance = 0.1 * 100 / (pidX.MaxInput - pidX.MinInput);
//            pidX.Tolerance = 0;


//            pidY.Kp = Kpy;
//            pidY.Ki = Kiy;
//            pidY.Kd = Kdy;
//            pidY.MaxOutput = max + 0.05;
//            pidY.MinOutput = min - 0.05;
//            pidY.MaxInput = 0.5;
//            pidY.MinInput = -0.5;
//            //pidY.Tolerance = 0.1 * 100 / (pidY.MaxInput - pidY.MinInput);
//            pidY.Tolerance = 0;


//            pidZ.Kp = Kpz;
//            pidZ.Ki = Kiz;
//            pidZ.Kd = Kdz;
//            pidZ.MaxOutput = max;
//            pidZ.MinOutput = min;
//            pidZ.MaxInput = 1.8;
//            pidZ.MinInput = 0;
//            //pidZ.Tolerance = 0.1 * 100 / (pidZ.MaxInput - pidZ.MinInput);
//            pidZ.Tolerance = 0;

//        }


//        /*------------------------------------------------------------------
//        * CRC calculation
//        -------------------------------------------------------------------*/
//        public void CrcCalculate()
//        {
//            this.crc = right_left_mov ^ rotate_left_right ^ up_down ^ forward_back ^ mode ^ und1 ^ und2;
//        }

//        /*------------------------------------------------------------------
//        * Create the message from the actual values of the virtual remote, 
//        * must be called before each send
//        -------------------------------------------------------------------*/
//        public void CreateMessage()
//        {
//            CrcCalculate();
//            byte[] data = { (byte)right_left_mov, (byte)forward_back, (byte)up_down, (byte)rotate_left_right,
//                                (byte)mode, (byte)und1, (byte)und2, (byte)crc, footer};

//            message = header.Concat(data).ToArray();
//        }

//        /*------------------------------------------------------------------
//        * Validate the step function with constraints
//        -------------------------------------------------------------------*/
//        public int ValidStep(int value, int s, int min, int max)
//        {
//            return Math.Max(Math.Min(max, value + s), min);
//        }
        
//        /*------------------------------------------------------------------
//        * Send the first communication packet
//        -------------------------------------------------------------------*/
//        public void SendHandShake()
//        {
//            udpClient.Send(headerNoHandshake, headerNoHandshake.Length);
//            //Console.WriteLine(BitConverter.ToString(headerNoHandshake));
//            Console.WriteLine("Handshake\n");
//        }

//        /*------------------------------------------------------------------
//        * Reset drone parameters
//        -------------------------------------------------------------------*/
//        public void Reset()
//        {
//            mode = 128;
//            Thread.Sleep(1000);
//            mode = 0;
//            Console.WriteLine("--------------- Calibration--------------");
//        }

//        /*------------------------------------------------------------------
//        * Send the command to the drone
//        -------------------------------------------------------------------*/
//        public void Send()
//        {
//            CreateMessage();
//            udpClient.Send(message, message.Length);
//            //Console.WriteLine(BitConverter.ToString(message));
//        }

//        /*------------------------------------------------------------------
//        * Used to maintain communication with drone during main program
//        -------------------------------------------------------------------*/
//        public void ThreadProc()
//        {
//            while (true)
//            {
//                Send();
//            }

//        }

//        public void Takeoff()
//        {
//            Console.WriteLine("Takeoff\n");
//            mode = 1;
//        }
//        public void Land()
//        {
//            Console.WriteLine("Land\n");
//            mode = 2;
//        }
//        public void Space()
//        {
//            Console.WriteLine("space\n");
//            mode = 0;
//        }
//        public void Forward()
//        {
//            forward_back = ValidStep(forward_back, step, 0, 255);
//            Console.WriteLine("forward {0}", forward_back);
//        }
//        public void Back()
//        {
//            forward_back = ValidStep(forward_back, -step, 0, 255);
//            Console.WriteLine("back {0}", forward_back);
//        }
//        public void Right()
//        {
//            right_left_mov = ValidStep(right_left_mov, step, MIN, MAX);
//            Console.WriteLine("right {0}", right_left_mov);
//        }
//        public void Left()
//        {
//            right_left_mov = ValidStep(right_left_mov, -step, MIN, MAX);
//            Console.WriteLine("left {0}", right_left_mov);
//        }
//        public void Stop()
//        {
//            right_left_mov = 128;
//            rotate_left_right = 128;
//            //up_down = 128;
//            forward_back = 128;
//            mode = 0;
//            pidX.ResetState();
//            pidY.ResetState();
//            pidZ.ResetState();
//            Console.WriteLine("Stop");
//        }

//        /*------------------------------------------------------------------
//        * pre-programmed qubic flight plan for the drone
//        -------------------------------------------------------------------*/
//        public void Qubic()
//        {
//            Left();
//            CvInvoke.WaitKey(3000);
//            Stop();
//            Forward();
//            CvInvoke.WaitKey(3000);
//            Stop();
//            Right();
//            CvInvoke.WaitKey(3000);
//            Stop();
//            Back();
//            CvInvoke.WaitKey(3000);
//            Stop();
//            CvInvoke.WaitKey(3000);
//            Land();
//        }
//        /*------------------------------------------------------------------
//        * check if drone is on target
//-------------------------------------------------------------------*/
//        public bool OnTarget(double setpoint, double input, double delta)
//        {              
//            return Math.Abs(setpoint-input) < delta;
//        }
//        /*------------------------------------------------------------------
//         * inputs: (X,Y,Z) coordinates of the drone
//         * updates drone commands according to drone and target position.
//        -------------------------------------------------------------------*/
//        //public void InstructionCalculate(double X, double Y, double Z)
//        public void InstructionCalculate(StereoPoint3D drone, ref Point3D target)
//        {
//            double X = drone.GetX3D();
//            double Y = drone.GetY3D();
//            double Z = drone.GetZ3D();
//            bool xValid = false;
//            bool yValid = false;
//            bool zValid = false;

//            Console.WriteLine($"Drone Coordinates:  [X: {drone.GetX3D()}, Y: {drone.GetY3D()}, Z: {drone.GetZ3D()}]\n");
//            Console.WriteLine($"Target Coordinates:  [X: {target.GetX()}, Y: {target.GetY()}, Z: {target.GetZ()}]\n");

//            pidX.Input = drone.GetX3D();
//            pidX.Setpoint = target.GetX();

//            pidY.Input = drone.GetY3D();
//            pidY.Setpoint = target.GetY();

//            pidZ.Input = drone.GetZ3D();
//            pidZ.Setpoint = target.GetZ();
//            Console.WriteLine($"PID X:  [Kd: {pidX.Kd}, Ki: {pidX.Ki}, Kd: {pidX.Kd}]\n");
//            Console.WriteLine($"PID Y:  [Kd: {pidY.Kd}, Ki: {pidY.Ki}, Kd: {pidY.Kd}]\n");
//            Console.WriteLine($"PID Z:  [Kd: {pidZ.Kd}, Ki: {pidZ.Ki}, Kd: {pidZ.Kd}]\n");




//            xValid = OnTarget(pidX.Setpoint, pidX.Input, delta);
//            yValid = OnTarget(pidY.Setpoint, pidY.Input, delta);
//            zValid = OnTarget(pidZ.Setpoint, pidZ.Input, delta);
            
//            //if (xValid)
//            //{
//            //    right_left_mov = 128;
//            //    pidX.ResetState();
//            //}
//            //else
//            //{
//            //    stateX = pidX.PerformPID();
//            //}

//            //if (yValid)
//            //{
//            //    up_down = 120;
//            //    pidY.ResetState();
//            //}
//            //else
//            //{
//            //    stateY = pidY.PerformPID();
//            //}

//            //if (zValid)
//            //{
//            //    forward_back = 128;
//            //    pidZ.ResetState();
//            //}
//            //else
//            //{
//            //    stateZ = pidZ.PerformPID();
//            //}
            
//            stateX = pidX.PerformPID();
//            stateY = pidY.PerformPID();
//            stateZ = pidZ.PerformPID();
//            int key = CvInvoke.WaitKey(4);
//            if ((key == -1) && !double.IsNaN(stateX) && !double.IsNaN(stateY) && !double.IsNaN(stateZ))
//            {
//                if (!xValid)
//                    right_left_mov = 128 + (int)(stateX*100);
//                if (!yValid)
//                    up_down = 120 - (int)(stateY*100);
//                if (!zValid)
//                    forward_back = 128 + (int)(stateZ*100);

//                Console.WriteLine("PID values:\n");
//                Console.WriteLine($"right_left_mov = {right_left_mov}\n");
//                Console.WriteLine($"up_down = {up_down}\n");
//                Console.WriteLine($"forward_back = {forward_back}\n");
//                //if(pidX.IsOnTarget)
//                //    Console.WriteLine("X on Target\n");
//                //if (pidY.IsOnTarget)
//                //    Console.WriteLine("Y on Target\n");
//                //if (pidZ.IsOnTarget)
//                //    Console.WriteLine("Z on Target\n");

//                //if (pidX.IsOnTarget && pidY.IsOnTarget && pidZ.IsOnTarget)
//                //{
//                //    target.arrived = true;
//                //    Console.WriteLine("On Target, all axes\n");
//                //}


//                if (xValid && yValid && zValid)
//                {
//                    target.arrived = true;
//                    Console.WriteLine("On Target, all axes\n");
//                }
//            }
//            else if (key == 27)
//            {
//                Stop();
//                Console.WriteLine("Exiting..");
//                mode = 2;
//                CvInvoke.WaitKey(5000);
//                Environment.Exit(0);
//            }

//            /*
//            // Horizontal axis
//            if ((X < (target.GetX() + delta)) && (X > (target.GetX() - delta)))
//            {
//                right_left_mov = 128;
//                mode = 0;
//                xValid = true;
//            }
//            else if (X < target.GetX())
//            {
//                right_left_mov = ValidStep(128 + stepX, 0, 0, 255);
//                Console.WriteLine("goes right");
//            }
//            else
//            {
//                right_left_mov = ValidStep(128 - stepX, 0, 0, 255);
//                Console.WriteLine("goes left");
//            }
//            // Vertical axis
//            if ((Y < (target.GetY() + delta)) && (Y > (target.GetY() - delta)))
//            {
//                up_down = 120;
//                mode = 0;
//                yValid = true;
//            }
//            else if (Y < target.GetY())
//            {
//                up_down = ValidStep(120 - stepY, 0, 0, 255);
//                Console.WriteLine("goes down");
//            }
//            else
//            {
//                up_down = ValidStep(120 + stepY + 10, 0, 0, 255);
//                Console.WriteLine("goes up");
//            }
//            //Depth axis
//            if ((Z < (target.GetZ() + delta)) && (Z > (target.GetZ() - delta)))
//            {
//                forward_back = 128;
//                mode = 0;
//                zValid = true;
//            }
//            else if (Z < target.GetZ())
//            {
//                forward_back = ValidStep(128 + stepZ, 0, 0, 255);
//                Console.WriteLine("goes forward");
//            }
//            else
//            {
//                forward_back = ValidStep(128 - stepZ, 0, 0, 255);
//                Console.WriteLine("goes backwards");
//            }
//            if (xValid && yValid && zValid)
//            {
//                target.arrived = true;                         
//            }
//        }
//        else
//        {

//            Console.WriteLine("kEY = {0}", key);
//            if (key == 54) // 6->
//            {
//                right_left_mov = ValidStep(right_left_mov, step, MIN, MAX);
//                Console.WriteLine("Throttle {0}", right_left_mov);
//            }

//            if (key == 52) // 4<-
//            {
//                right_left_mov = ValidStep(right_left_mov, -step, 0, 255); //65-191
//                Console.WriteLine("Throttle {0}", right_left_mov);
//            }

//            if (key == 'A' || key == 'a') // a|A
//            {
//                rotate_left_right = ValidStep(rotate_left_right, -step, 0, 255);
//                Console.WriteLine("Rudder {0}", rotate_left_right);
//            }

//            if (key == 'D' || key == 'd') // d|D
//            {
//                rotate_left_right = ValidStep(rotate_left_right, step, 0, 255);
//                Console.WriteLine("Rudder {0}", rotate_left_right);
//            }

//            if (key == 56) // 8
//            {
//                forward_back = ValidStep(forward_back, step, 0, 255);
//                Console.WriteLine("forward_back {0}", forward_back);
//            }

//            if (key == 50) // 2
//            {
//                forward_back = ValidStep(forward_back, -step, 0, 255);
//                Console.WriteLine("forward_back {0}", forward_back);
//            }

//            if (key == 'S' || key == 's') // s|S
//            {
//                up_down = ValidStep(up_down, -step, 0, 255);
//                Console.WriteLine("up_down {0}", up_down);
//            }

//            if (key == 'W' || key == 'w') // w|W
//            {
//                up_down = ValidStep(up_down, step, 0, 255);
//                Console.WriteLine("up_down {0}", up_down);
//            }

//            if (key == 'T' || key == 't') // t|T
//            {
//                mode = 1;
//                Console.WriteLine("Mode {0} //takeoff", mode);
//            }

//            if (key == 'E' || key == 'e') // E|e
//            {
//                mode = 4;
//                Console.WriteLine("Mode {0} //Emergency", mode);
//            }

//            if (key == 'L' || key == 'l') // l|L
//            {
//                mode = 2;
//                Console.WriteLine("Mode {0} //land", mode);
//            }
//            //~~~~~~~TEST KEYS~~~~~~~~~~#                        
//            if (key == 'Z' || key == 'z') // z|Z
//            {
//                Reset();
//                Console.WriteLine("und1 {0} ", und1);
//            }

//            if (key == 'X' || key == 'x') // x|X
//            {
//                und1 = 2;
//                Console.WriteLine("und1 {0}", und1);
//            }

//            if (key == 'N' || key == 'n') // n|N
//            {
//                //square_filght();
//                Console.WriteLine("und2 {0} ", und2);
//            }

//            if (key == 'M' || key == 'm') // m|M
//            {
//                und2 = 2;
//                Console.WriteLine("und2 {0}", und2);
//            }

//            //~~~~~~~RESET AND ESC KEYS~~~~~~~~~~    
//            if (key == 32) // space
//            {
//                right_left_mov = 128;
//                rotate_left_right = 128;
//                up_down = 120;
//                forward_back = 128;
//                mode = 0;
//                Console.WriteLine("Reset commands");
//            }

//            if (key == 27) // Esc
//            {
//                Console.WriteLine("Exiting..");
//                mode = 2;
//                CvInvoke.WaitKey(5000);
//                Environment.Exit(0);
//            }
            
//        }
//        */

//            //}

//        }
//    }
//}
