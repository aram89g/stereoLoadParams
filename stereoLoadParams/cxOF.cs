﻿using System;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using Emgu.CV;

namespace cxOF
{
    public class CxOF
    {

        private string IP = "192.168.0.1"; // drone IP
        private int Port = 40000; // drone UDP port
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

        /*------------------------------------------------------------------
        * CRC calculation
        -------------------------------------------------------------------*/
        public void CrcCalculate()
        {
            this.crc = right_left_mov ^ rotate_left_right ^ up_down ^ forward_back ^ mode ^ und1 ^ und2;
        }

        /*------------------------------------------------------------------
        * Create the message from the actual values of the virtual remote, 
        * must be called before each send
        -------------------------------------------------------------------*/
        public void CreateMessage()
        {
            CrcCalculate();
            byte[] data = { (byte)right_left_mov, (byte)forward_back, (byte)up_down, (byte)rotate_left_right,
                                (byte)mode, (byte)und1, (byte)und2, (byte)crc, footer};

            message = header.Concat(data).ToArray();
        }

        /*------------------------------------------------------------------
        * Validate the step function with constraints
        -------------------------------------------------------------------*/
        public int ValidStep(int value, int step, int min, int max)
        {
            return Math.Max(Math.Min(max, value + step), min);
        }

        /*------------------------------------------------------------------
        * Send the first communication packet
        -------------------------------------------------------------------*/
        public void SendHandShake()
        {
            udpClient.Send(headerNoHandshake, headerNoHandshake.Length);
            Console.WriteLine(BitConverter.ToString(headerNoHandshake));
        }

        /*------------------------------------------------------------------
        * Reset drone parameters
        -------------------------------------------------------------------*/
        public void Reset()
        {
            mode = 128;
            Thread.Sleep(1000);
            mode = 0;
            Console.WriteLine("--------------- Calibration--------------");
        }
        
        /*------------------------------------------------------------------
        * Send the command to the drone
        -------------------------------------------------------------------*/
        public void Send()
        {
            CreateMessage();
            udpClient.Send(message, message.Length);
            Console.WriteLine(BitConverter.ToString(message));
        }

        /*------------------------------------------------------------------
        * Used to maintain communication with drone during main program
        -------------------------------------------------------------------*/
        public void ThreadProc()
        {
            while (true)
            {
                Send();
            }

        }

        public void Takeoff()
        {
            Console.WriteLine("Takeoff\n");
            mode = 1;
        }
        public void Land()
        {
            Console.WriteLine("Land\n");
            mode = 2;
        }
        public void Space()
        {
            Console.WriteLine("space\n");
            mode = 0;
        }
        public void Forward()
        {
            forward_back = ValidStep(forward_back, step, 0, 255);
            Console.WriteLine("forward {0}", forward_back);
        }
        public void Back()
        {
            forward_back = ValidStep(forward_back, -step, 0, 255);
            Console.WriteLine("back {0}", forward_back);
        }
        public void Right()
        {
            right_left_mov = ValidStep(right_left_mov, step, MIN, MAX);
            Console.WriteLine("right {0}", right_left_mov);
        }
        public void Left()
        {
            right_left_mov = ValidStep(right_left_mov, -step, MIN, MAX);
            Console.WriteLine("left {0}", right_left_mov);
        }
        public void Stop()
        {
            right_left_mov = 128;
            rotate_left_right = 128;
            up_down = 120;
            forward_back = 128;
            mode = 0;
            Console.WriteLine("Stop");
        }

        /*------------------------------------------------------------------
        * pre-programmed qubic flight plan for the drone
        -------------------------------------------------------------------*/
        public void Qubic()
        {
            Left();
            CvInvoke.WaitKey(3000);
            Stop();
            Forward();
            CvInvoke.WaitKey(3000);
            Stop();
            Right();
            CvInvoke.WaitKey(3000);
            Stop();
            Back();
            CvInvoke.WaitKey(3000);
            Stop();
            CvInvoke.WaitKey(3000);
            Land();
        }

        /*------------------------------------------------------------------
         * inputs: (X,Y,Z) coordinates of the drone
         * updates drone commands according to drone and target position.
        -------------------------------------------------------------------*/
        public void InstructionCalculate(double X, double Y, double Z)
        {
            //while (true)
            //{
                int key = CvInvoke.WaitKey(50);
                if (key == -1)
                {
                    // Horizontal axis
                    if (X < TargetCoordinate.X_target)
                    {
                        right_left_mov = ValidStep(138, 0, 0, 255);
                        Console.WriteLine("goes right");
                    }
                    else
                    {
                        right_left_mov = ValidStep(118, 0, 0, 255);
                        Console.WriteLine("goes left");
                    }
                    // Vertical axis
                    if (Y < TargetCoordinate.Y_target)
                    {
                        up_down = ValidStep(118, 0, 0, 255);
                        Console.WriteLine("goes down");
                    }
                    else
                    {
                        up_down = ValidStep(138, 0, 0, 255);
                        Console.WriteLine("goes up");
                    }
                    // Depth axis
                    if (Z < TargetCoordinate.Z_target)
                    {
                        forward_back = ValidStep(138, 0, 0, 255);
                        Console.WriteLine("goes forward");
                    }
                    else
                    {
                        forward_back = ValidStep(118, 0, 0, 255);
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
            //}

        }
    }
}