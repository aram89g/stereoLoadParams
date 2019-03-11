/* --------------------------------------------------------------------------------------------------------------
 * Description: This is a tello remote control that is used to communicate with the tello drone over UDP.
 *              It has control variables that can be used to send the drone RC commands in all channels, 
 *              and it can send all the commands from the Tello SDK 1.3.
 *              Logging is done through two files:
 *                  - "Desktop/state.txt":  drone statistics.
 *                  - "Desktop/response.txt": drone responses to commands.
 *              It throws a TimeoutException if the drone hasn't responded to a sent command.
 * Created by:  Aram Gasparian.
 * Date:        Feb 2019.
 --------------------------------------------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Emgu.CV;
using PIDControllers;

public class Tello
{
    string tello_host = "192.168.10.1";
    string local_host = "0.0.0.0";
    int tello_port = 8889;
    int local_state_port = 8890;
    int tello_video_port = 11111;
    int timeout = 7000; // msec

    IPEndPoint local_endpoint;
    IPEndPoint tello_endpoint;

    // Response thread
    Thread response_thread;
    Socket socket;

    // Video thread
    Mat frame = new Mat();
    bool is_new_frame_to_process;
    VideoCapture cap;
    Thread video_receive_thread;

    // Controls
    public int left_right = 0;
    public int down_up = 0;
    public int back_forward = 0;
    public int yaw = 0;

    // State thread
    IPEndPoint state_endpoint;
    Socket state_socket;
    Thread state_receive_thread;

    // Log files
    static string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    StreamWriter stateFile = new StreamWriter(desktop + @"\state.txt");
    StreamWriter responseFile = new StreamWriter(desktop + @"\response.txt");

    bool respFlag;
    bool abortFlag;

    // PID's
    PIDController pidX = new PIDController();
    PIDController pidY = new PIDController();
    PIDController pidZ = new PIDController();
    
    // PID output limitations
    static double min = -0.20; // [m\sec] maximum speed for pid output
    static double max = 0.20; // [m\sec] minimum speed for pid output

    // PID outputs
    double stateX;
    double stateY;
    double stateZ;

    // Error tolerance
    double delta = 0.1;

    // Kp,Kd,Ki variables
    double Tix = 100;
    double Tdx = 0.5;
    double Kpx = (1.0 / 7) * 3;
    double Kix;
    double Kdx;

    double Tiy = 100;
    double Tdy = 0.5;
    double Kpy = (1.0 / 7) * 3;
    double Kiy;
    double Kdy;

    double Tiz = 100;
    double Tdz = 0.05;
    double Kpz = (1.0 / 7) * 3;
    double Kiz;
    double Kdz;

    /**********************************************************
     * send a command to the drone and busy wait for it's response,
     * throws TimeoutException if timeout passsed.
     **********************************************************/
    public void SendCommand(string command)
    {
        abortFlag = false;
        byte[] cmd = Encoding.UTF8.GetBytes(command);
        socket.SendTo(cmd, cmd.Length, SocketFlags.None, tello_endpoint);
        //Timer timer = new Timer(SetAbortFlag, null, timeout, Timeout.Infinite);
        //while (respFlag == false)
        //{
        //    if (abortFlag == true)
        //    {
        //        socket.SendTo(Encoding.UTF8.GetBytes("land"), Encoding.UTF8.GetBytes("land").Length, SocketFlags.None, tello_endpoint);
        //        throw new TimeoutException($"Command: {command} didn't have a response");                
        //    }
        //}
        //timer.Dispose();
        //respFlag = false;
    }

    /**********************************************************
     * initialize PID parameters.
     **********************************************************/
    public void InitializePIDs()
    {
        // initialize PID constants
        Kix = Kpx / Tix;
        Kdx = Kpx * Tdx;
        Kiy = Kpy / Tiy;
        Kdy = Kpy * Tdy;
        Kiz = Kpz / Tiz;
        Kdz = Kpz * Tdz;

        // Left-Right control PID
        pidX.Kp = Kpx;
        pidX.Ki = Kix;
        pidX.Kd = Kdx;
        pidX.MaxOutput = max;
        pidX.MinOutput = min;
        pidX.MaxInput = 0.4;
        pidX.MinInput = -0.4;
        pidX.Tolerance = delta * 100 / (pidX.MaxInput - pidX.MinInput);

        // Up-Down control PID
        pidY.Kp = Kpy;
        pidY.Ki = Kiy;
        pidY.Kd = Kdy;
        pidY.MaxOutput = max;
        pidY.MinOutput = min;
        pidY.MaxInput = 0.5;
        pidY.MinInput = -0.5;
        pidY.Tolerance = delta * 100 / (pidY.MaxInput - pidY.MinInput);

        // Forward-Back control PID
        pidZ.Kp = Kpz;
        pidZ.Ki = Kiz;
        pidZ.Kd = Kdz;
        pidZ.MaxOutput = max;
        pidZ.MinOutput = min;
        pidZ.MaxInput = 3;
        pidZ.MinInput = 0;
        pidZ.Tolerance = delta * 100 / (pidZ.MaxInput - pidZ.MinInput);
    }

    /**********************************************************
     * TELLO constructor, creates sockects and threads.
     * enables drone SDK mode.
     **********************************************************/
    public Tello()
    {
        InitializePIDs();

        tello_endpoint = new IPEndPoint(IPAddress.Parse(tello_host), tello_port);
        local_endpoint = new IPEndPoint(IPAddress.Parse(local_host), tello_port);
        state_endpoint = new IPEndPoint(IPAddress.Parse(local_host), local_state_port);

        // socket for sending commands and recieve responses
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(local_endpoint);
        response_thread = new Thread(new ThreadStart(ResponseProc));
        response_thread.Start();

        // socket for recieving tello drone state
        state_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        state_socket.Bind(state_endpoint);
        state_receive_thread = new Thread(new ThreadStart(StateReceiveProc));
        state_receive_thread.Start();

        // initiate drone SDK mode
        SendCommand("command");
        Thread.Sleep(500);

        // initiate drone SDK mode
        SendCommand("streamon");
        Thread.Sleep(2000);

        // recieve video from tello drone
        cap = new VideoCapture(@"udp://" + tello_host + $":{tello_video_port}");
        if (!cap.IsOpened)
        {
            Console.WriteLine("Stream is not opened");
        }
        video_receive_thread = new Thread(new ThreadStart(VideoReceiveProc));
        video_receive_thread.Start();
        
    }

    /**********************************************************
     * drone video stream thread, takes frame from stream and if 
     * there is a valid frame raises flag "is_new_frame_to_process".
     **********************************************************/
    public void VideoReceiveProc()
    {
        while (true)
        {
            frame = cap.QueryFrame();
            if (!frame.IsEmpty)
            {
                is_new_frame_to_process = true;
            }
        }
    }

    /**********************************************************
     * return true if there is a new frame to process.
     **********************************************************/
    public bool IsNewFrameReady()
    {
        return is_new_frame_to_process;
    }

    /**********************************************************
     * Returns the last frame recieved from drone.
     **********************************************************/
    public Mat GetLastFrame()
    {
        is_new_frame_to_process = false;
        return frame;
    }

    /********************************************************** 
     * recieve drone statistics about it's battery, location, 
     * speed, battery, barometer, temperature, 
     * pitch, roll, yaw, time of flight.
     * (statistics are seperated by a semicolon(;)
     * Save it to a file.
     **********************************************************/
    public void StateReceiveProc()
    {
        int buffer_size = 1518;
        byte[] buff = new byte[buffer_size];
        while (true)
        {
            state_socket.Receive(buff);
            stateFile.WriteLine(Encoding.UTF8.GetString(buff));
        }
    }

    /**********************************************************
     * recieve responses to sent commands, when a response is 
     * recieved raises flag to alert that the resonse has 
     * recieved and writes it to a file.
     **********************************************************/
    public void ResponseProc()
    {
        int buffer_size = 1518;
        byte[] buff = new byte[buffer_size];
        while (true)
        {
            socket.Receive(buff);
            respFlag = true;
            Console.WriteLine(Encoding.UTF8.GetString(buff));
            responseFile.WriteLine(Encoding.UTF8.GetString(buff));
        }
    }

    /**********************************************************
    * raise abort flag if drone didn't send response,
    * which means problem with communication
    **********************************************************/
    public void SetAbortFlag(Object state)
    {
        abortFlag = true;
    }

    /**********************************************************
     * Basic drone command shorcuts
     **********************************************************/

    // Send a RC command (all channels)
    public void SendRcControl()
    {
        SendCommand($"rc {left_right} {back_forward} {down_up} {yaw}");
    }
    // drone takeoff from ground
    public void Takeoff()
    {
        SendCommand("takeoff");
    }
    // drone land
    public void Land()
    {
        SendCommand("land");
    }
    // enable video stream
    public void SetVideoStreamOn()
    {
        SendCommand("streamon");
    }
    // disable video stream
    public void SetVideoStreamOff()
    {
        SendCommand("streamoff");
    }

    /**********************************************************
     * operetions to do when exisiting the program
     **********************************************************/
    public void Exit()
    {
        // close files
        responseFile.Close();
        stateFile.Close();
    }

    /***********************************************************
    * check if drone is on target
    ***********************************************************/
    public bool OnTarget(double setpoint, double input, double delta)
    {
        return Math.Abs(setpoint - input) < delta;
    }

    /***********************************************************
    * inputs: (X,Y,Z) coordinates of the drone
    * updates drone commands according to drone and target position.
    ***********************************************************/
    public void InstructionCalculate(StereoPoint3D drone, ref Point3D target)
    {
        double delta = 0.1;
        //int stepX = 20;
        //int stepY = 20;
        //int stepZ = 20;

        double X = drone.GetX3D();
        double Y = drone.GetY3D();
        double Z = drone.GetZ3D();

        bool xValid = false;
        bool yValid = false;
        bool zValid = false;

        Console.WriteLine($"Drone Coordinates:  [X: {drone.GetX3D()}, Y: {drone.GetY3D()}, Z: {drone.GetZ3D()}]\n");
        Console.WriteLine($"Target Coordinates:  [X: {target.GetX()}, Y: {target.GetY()}, Z: {target.GetZ()}]\n");
        
        // set PID input and reference
        pidX.Input = drone.GetX3D();
        pidX.Setpoint = target.GetX();

        pidY.Input = drone.GetY3D();
        pidY.Setpoint = target.GetY();

        pidZ.Input = drone.GetZ3D();
        pidZ.Setpoint = target.GetZ();

        Console.WriteLine($"PID X:  [Kp: {pidX.Kp}, Ki: {pidX.Ki}, Kd: {pidX.Kd}]\n");
        Console.WriteLine($"PID Y:  [Kp: {pidY.Kp}, Ki: {pidY.Ki}, Kd: {pidY.Kd}]\n");
        Console.WriteLine($"PID Z:  [Kp: {pidZ.Kp}, Ki: {pidZ.Ki}, Kd: {pidZ.Kd}]\n");

        // check if drone is at target
        xValid  = OnTarget(pidX.Setpoint, pidX.Input, delta);
        yValid = OnTarget(pidY.Setpoint, pidY.Input, delta);
        zValid = OnTarget(pidZ.Setpoint, pidZ.Input, delta);

        // calculate PID output
        stateX = pidX.PerformPID();
        stateY = pidY.PerformPID();
        stateZ = pidZ.PerformPID();

        if (!double.IsNaN(stateX) && !double.IsNaN(stateY) && !double.IsNaN(stateZ))
        {
            if (!xValid)
                left_right = (int)(stateX * 100);
            if (!yValid)
                down_up = -1*(int)(stateY * 100);
            if (!zValid)
                back_forward = (int)(stateZ * 100);

            Console.WriteLine("PID values:\n");
            Console.WriteLine($"right_left_mov = {left_right}\n");
            Console.WriteLine($"up_down = {down_up}\n");
            Console.WriteLine($"forward_back = {back_forward}\n");

            if (xValid && yValid && zValid)
            {
                target.arrived = true;
                Console.WriteLine("On Target, all axes\n");
            }

            // Send the RC command to the drone
            SendRcControl();
        }


        
        /*
        
        // Horizontal axis
        if ((X < (target.GetX() + delta)) && (X > (target.GetX() - delta)))
        {
            left_right = 0;
            xValid = true;
        }
        else if (X < target.GetX())
        {
            left_right = stepX;
            Console.WriteLine("goes right");
        }
        else
        {
            left_right = -stepX;
            Console.WriteLine("goes left");
        }
        // Vertical axis
        if ((Y < (target.GetY() + delta)) && (Y > (target.GetY() - delta)))
        {
            down_up = 0;
            yValid = true;
        }
        else if (Y < target.GetY())
        {
            down_up = -stepY;
            Console.WriteLine("goes down");
        }
        else
        {
            down_up = stepY;
            Console.WriteLine("goes up");
        }
        //Depth axis
        if ((Z < (target.GetZ() + delta)) && (Z > (target.GetZ() - delta)))
        {
            back_forward = 0;
            zValid = true;
        }
        else if (Z < target.GetZ())
        {
            back_forward = stepZ;
            Console.WriteLine("goes forward");
        }
        else
        {
            back_forward = -stepZ;
            Console.WriteLine("goes backwards");
        }
        if (xValid && yValid && zValid)
        {
            target.arrived = true;
        }
        
        // Send the RC command to the drone
        SendRcControl();
        */
    }

}