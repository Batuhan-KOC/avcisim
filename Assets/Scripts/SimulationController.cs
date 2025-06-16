using System;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public delegate void simulation_signal();
    public static event simulation_signal simulation_started;
    public static event simulation_signal simulation_stopped;
    public static event simulation_signal simulation_initialized;

    private bool simulation_initialized_sent = false;

    void OnEnable()
    {
        CommunicationController.start_environment += start_environment_signal_received;
        CommunicationController.stop_environment += stop_environment_signal_received;
        CommunicationController.drone_position_updated += drone_position_updated_signal_received;
        CommunicationController.simulation_started_signal_received += simulation_started_signal_received;
        CommunicationController.simulation_stopped_signal_received += simulation_stopped_signal_received;
        CommunicationController.simulation_initialized_signal_received += simulation_initialized_signal_received;
    }

    void OnDisable()
    {
        CommunicationController.start_environment -= start_environment_signal_received;
        CommunicationController.stop_environment -= stop_environment_signal_received;
        CommunicationController.drone_position_updated -= drone_position_updated_signal_received;
        CommunicationController.simulation_started_signal_received -= simulation_started_signal_received;
        CommunicationController.simulation_stopped_signal_received -= simulation_stopped_signal_received;
        CommunicationController.simulation_initialized_signal_received -= simulation_initialized_signal_received;
    }

    void Start()
    {
        if (!simulation_initialized_sent)
        {
            send_simulation_initialized();
            simulation_initialized_sent = true;
        }
    }

    public void start_environment_signal_received()
    {
        // Handle start environment signal
    }

    public void stop_environment_signal_received()
    {
        // Handle stop environment signal
    }

    public void send_simulation_started()
    {
        simulation_started?.Invoke();
    }

    public void send_simulation_stopped()
    {
        simulation_stopped?.Invoke();
    }

    public void send_simulation_initialized()
    {
        simulation_initialized?.Invoke();
    }

    public void drone_position_updated_signal_received(float lat, float lon, float alt, float roll, float pitch, float yaw)
    {
        // Handle drone position update here
    }
}
