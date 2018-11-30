using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Emgu.CV;
using Emgu.CV.CvEnum;
using stereoLoadParams;

namespace cxOF
{
    public class CxOF
    {

        private string IP = "192.168.0.1";
        private int Port = 40000;
        private UdpClient udpClient = new UdpClient();
        private byte[] headerNoHandshake = { 0x63, 0x63, 0x01, 0x00, 0x00, 0x00, 0x00 }; //first 7 bytes of every payload
        private byte[] header = { 0x63, 0x63, 0x0a, 0x00, 0x00, 0x0a, 0x00, 0xcc }; // first byte of every payload
        private byte footer = 0x33; // last byte of every payload

        private int right_left_mov = 128;
        private int forward_back = 128;
        private int rotate_left_right = 128;
        private int up_down = 120; //Change it to set the drone altitude
        private int und1 = 0;   //undefined bytes
        private int und2 = 0;   //undefined bytes      
        private int crc = 0;    //crc calculated from all bytes xor product
        private int mode = 0;   // 0 = idle, 1 = takeoff, 2 = land

        static int MIN = 0;
        static int MAX = 255;
        static int step = 15;
        private byte[] message;

        public CxOF()
        {
            udpClient.Connect(IP, Port);
        }

        // crc calculation
        public void CrcCalculate()
        {
            this.crc = right_left_mov ^ rotate_left_right ^ up_down ^ forward_back ^ mode ^ und1 ^ und2;
        }

        // create the message from the actual values of the virtual remote, must be called before each send
        public void CreateMessage()
        {
            CrcCalculate();
            //   string data = right_left_mov.ToString("X2") + forward_back.ToString("X2") + up_down.ToString("X2") +
            //                 rotate_left_right.ToString("X2") + mode.ToString("X2") + und1.ToString("X2") +
            //                  und2.ToString("X2") + crc.ToString("X2");

            byte[] data = { (byte)right_left_mov, (byte)forward_back, (byte)up_down, (byte)rotate_left_right,
                                (byte)mode, (byte)und1, (byte)und2, (byte)crc, footer};

            message = header.Concat(data).ToArray();


            //message = BitConverter.ToString(header).Replace("-", "") + data + BitConverter.ToString(footer).Replace("-", "");
        }

        // validate the step function with constraints
        public int ValidStep(int value, int step, int min, int max)
        {
            return Math.Max(Math.Min(max, value + step), min);
        }

        //Sends the first communication packet
        public void SendHandShake()
        {
            udpClient.Send(headerNoHandshake, headerNoHandshake.Length);
            Console.WriteLine(BitConverter.ToString(headerNoHandshake));
        }

        // Resets the parameters
        public void Reset()
        {
            mode = 128;
            Thread.Sleep(1000);
            mode = 0;
            Console.WriteLine("--------------- Calibration--------------");
        }

        // Send the command to the drone
        public void Send()
        {
            CreateMessage();
            //byte[] msg = Encoding.ASCII.GetBytes(message);
            udpClient.Send(message, message.Length);
            Console.WriteLine(BitConverter.ToString(message));

        }

        public void TimerDelegate(Object stateInfo)
        {
            Send();
        }

        public void Takeoff()
        {
            mode = 1;
            Send();
        }

        public void InstructionCalculate(double X, double Y, double Z)
        {
            int key = CvInvoke.WaitKey(10);
            if (key == -1)
            {
                // Horizontal axis
                if (X < TargetCoordinate.X_target)
                {
                    right_left_mov = ValidStep(155, 0, 0, 255);
                    Console.WriteLine("goes right");
                }
                else
                {
                    right_left_mov = ValidStep(85, 0, 0, 255);
                    Console.WriteLine("goes left");
                }
                // Vertical axis
                if (Y < TargetCoordinate.Y_target)
                {
                    up_down = ValidStep(85, 0, 0, 255);
                    Console.WriteLine("goes down");
                }
                else
                {
                    up_down = ValidStep(155, 0, 0, 255);
                    Console.WriteLine("goes up");
                }
                // Depth axis
                if (Z < TargetCoordinate.Z_target)
                {
                    forward_back = ValidStep(155, 0, 0, 255);
                    Console.WriteLine("goes forward");
                }
                else
                {
                    forward_back = ValidStep(85, 0, 0, 255);
                    Console.WriteLine("goes backwards");
                }
            }
            else
            {

                Console.WriteLine("kEY = {0}", key);
                if (key == 54) // 6->
                {
                    right_left_mov = ValidStep(right_left_mov, step, MIN, MAX);
                    Console.WriteLine("Throttle {0}", right_left_mov);
                }

                if (key == 52) // 4<-
                {
                    right_left_mov = ValidStep(right_left_mov, -step, 0, 255); //65-191
                    Console.WriteLine("Throttle {0}", right_left_mov);
                }

                if (key == 'A' || key == 'a') // a|A
                {
                    rotate_left_right = ValidStep(rotate_left_right, -step, 0, 255);
                    Console.WriteLine("Rudder {0}", rotate_left_right);
                }

                if (key == 'D' || key == 'd') // d|D
                {
                    rotate_left_right = ValidStep(rotate_left_right, step, 0, 255);
                    Console.WriteLine("Rudder {0}", rotate_left_right);
                }

                if (key == 56) // 8
                {
                    forward_back = ValidStep(forward_back, step, 0, 255);
                    Console.WriteLine("forward_back {0}", forward_back);
                }

                if (key == 50) // 2
                {
                    forward_back = ValidStep(forward_back, -step, 0, 255);
                    Console.WriteLine("forward_back {0}", forward_back);
                }

                if (key == 'S' || key == 's') // s|S
                {
                    up_down = ValidStep(up_down, -step, 0, 255);
                    Console.WriteLine("up_down {0}", up_down);
                }

                if (key == 'W' || key == 'w') // w|W
                {
                    up_down = ValidStep(up_down, step, 0, 255);
                    Console.WriteLine("up_down {0}", up_down);
                }

                if (key == 'T' || key == 't') // t|T
                {
                    mode = 1;
                    Console.WriteLine("Mode {0} //takeoff", mode);
                }

                if (key == 'E' || key == 'e') // E|e
                {
                    mode = 4;
                    Console.WriteLine("Mode {0} //Emergency", mode);
                }

                if (key == 'L' || key == 'l') // l|L
                {
                    mode = 2;
                    Console.WriteLine("Mode {0} //land", mode);
                }
                //~~~~~~~TEST KEYS~~~~~~~~~~#                        
                if (key == 'Z' || key == 'z') // z|Z
                {
                    Reset();
                    Console.WriteLine("und1 {0} ", und1);
                }

                if (key == 'X' || key == 'x') // x|X
                {
                    und1 = 2;
                    Console.WriteLine("und1 {0}", und1);
                }

                if (key == 'N' || key == 'n') // n|N
                {
                    //square_filght();
                    Console.WriteLine("und2 {0} ", und2);
                }

                if (key == 'M' || key == 'm') // m|M
                {
                    und2 = 2;
                    Console.WriteLine("und2 {0}", und2);
                }

                //~~~~~~~RESET AND ESC KEYS~~~~~~~~~~    
                if (key == 32) // space
                {
                    right_left_mov = 128;
                    rotate_left_right = 128;
                    up_down = 120;
                    forward_back = 128;
                    mode = 0;
                    Console.WriteLine("Reset commands");
                }

                if (key == 27) // Esc
                {
                    Console.WriteLine("Exiting..");
                    mode = 2;
                    System.Environment.Exit(0);
                }
            }

        }
    }
    //static void Main(string[] args)
    //{
    //    CxOF rmt = new CxOF();
    //    rmt.SendHandShake();
    //    TimerCallback tmCallback = rmt.TimerDelegate;
    //    Timer timer = new Timer(tmCallback, null, 1000, 100);
    //    rmt.Send();
    //    //mt.takeoff_drone();s
    //    //rmt.square_filght();

    //    CvInvoke.Imshow("Press Here", Frame);
    //    rmt.Loop();
    //}

    //}
}
