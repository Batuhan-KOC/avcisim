using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class CommunicationController : MonoBehaviour
{
    public delegate void EnvironmentSignal();
    public static event EnvironmentSignal start_environment;
    public static event EnvironmentSignal stop_environment;

    public delegate void SimulationSignal();
    // Removed duplicate event declarations for simulation_started_signal_received, simulation_stopped_signal_received, simulation_initialized_signal_received

    public delegate void drone_position_updated_signal(float lat, float lon, float alt, float roll, float pitch, float yaw);
    public static event drone_position_updated_signal drone_position_updated;

    private readonly int rx_port_10003 = 10003;
    private readonly int tx_port_10003 = 10006;
    private readonly int rx_port_10004 = 10004;
    private readonly string local_ip = "127.0.0.1";
    private UdpClient udp_client_10003;
    private UdpClient udp_client_10004;
    private UdpClient udpSender;
    private Thread receiveThread;
    private bool running = false;
    private ConcurrentQueue<byte> messageQueue = new ConcurrentQueue<byte>();
    private ConcurrentQueue<Action> simulationSignalQueue = new ConcurrentQueue<Action>();
    private ConcurrentQueue<(float, float, float, float, float, float)> drone_position_queue = new ConcurrentQueue<(float, float, float, float, float, float)>();

    // Singleton instance for static method access
    public static CommunicationController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        running = true;
        udp_client_10003 = new UdpClient(new IPEndPoint(IPAddress.Parse(local_ip), rx_port_10003));
        udp_client_10003.Client.Blocking = false;
        udp_client_10004 = new UdpClient(new IPEndPoint(IPAddress.Parse(local_ip), rx_port_10004));
        udp_client_10004.Client.Blocking = false;
        udpSender = new UdpClient();
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Subscribe to SimulationController signals
        SimulationController.simulation_started += () => EnqueueSimulationSignal(simulation_started_signal_received);
        SimulationController.simulation_stopped += () => EnqueueSimulationSignal(simulation_stopped_signal_received);
        SimulationController.simulation_initialized += () => EnqueueSimulationSignal(simulation_initialized_signal_received);
    }

    private void EnqueueSimulationSignal(Action signalAction)
    {
        simulationSignalQueue.Enqueue(signalAction);
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out byte msg))
        {
            if (msg == 0b00000001)
            {
                start_environment?.Invoke();
            }
            else if (msg == 0b00000010)
            {
                stop_environment?.Invoke();
            }
        }

        // Handle simulation signal queue
        while (simulationSignalQueue.TryDequeue(out var action))
        {
            action?.Invoke();
        }

        // Handle drone position queue
        while (drone_position_queue.TryDequeue(out var pos))
        {
            drone_position_updated?.Invoke(pos.Item1, pos.Item2, pos.Item3, pos.Item4, pos.Item5, pos.Item6);
        }
    }

    public static void simulation_started_signal_received()
    {
        Instance?.SendSimulationSignal(0b00000001);
    }
    public static void simulation_stopped_signal_received()
    {
        Instance?.SendSimulationSignal(0b00000010);
    }
    public static void simulation_initialized_signal_received()
    {
        Instance?.SendSimulationSignal(0b00000100);
    }

    private void SendSimulationSignal(byte signal)
    {
        try
        {
            udpSender.Send(new byte[] { signal }, 1, new IPEndPoint(IPAddress.Parse(local_ip), tx_port_10003));
        }
        catch (Exception ex)
        {
            Debug.LogError($"UDP Send Error: {ex.Message}");
        }
    }

    private void ReceiveData()
    {
        try
        {
            while (running)
            {
                ReadPort10003();
                ReadPort10004();
                Thread.Sleep(1); // Prevent tight loop
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"UDP Receive Error: {ex.Message}");
        }
    }

    private void ReadPort10003()
    {
        IPEndPoint remoteEndPoint10003 = new IPEndPoint(System.Net.IPAddress.Any, rx_port_10003);
        if (udp_client_10003.Available > 0)
        {
            byte[] data = udp_client_10003.Receive(ref remoteEndPoint10003);
            if (data != null && data.Length > 0)
            {
                messageQueue.Enqueue(data[0]);
            }
        }
    }

    private void ReadPort10004()
    {
        IPEndPoint remoteEndPoint10004 = new IPEndPoint(System.Net.IPAddress.Any, rx_port_10004);
        if (udp_client_10004.Available > 0)
        {
            byte[] data = udp_client_10004.Receive(ref remoteEndPoint10004);
            if (data != null && data.Length == 24)
            {
                float lat = System.BitConverter.ToSingle(data, 0);
                float lon = System.BitConverter.ToSingle(data, 4);
                float alt = System.BitConverter.ToSingle(data, 8);
                float roll = System.BitConverter.ToSingle(data, 12);
                float pitch = System.BitConverter.ToSingle(data, 16);
                float yaw = System.BitConverter.ToSingle(data, 20);
                drone_position_queue.Enqueue((lat, lon, alt, roll, pitch, yaw));
            }
        }
    }

    void OnDestroy()
    {
        running = false;
        if (udp_client_10003 != null)
        {
            udp_client_10003.Close();
        }
        if (udp_client_10004 != null)
        {
            udp_client_10004.Close();
        }
        if (udpSender != null)
        {
            udpSender.Close();
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
    }
}
