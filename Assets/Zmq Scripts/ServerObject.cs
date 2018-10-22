using System.Diagnostics;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System;

public class NetMqPublisher
{
    private readonly Thread _listenerWorker;

    private bool _listenerCancelled;

    public delegate string MessageDelegate(string message);

    private readonly MessageDelegate _messageDelegate;

    private readonly Stopwatch _contactWatch;

    private const long ContactThreshold = 1000;

    public bool Connected;
    public GameObject cylinder;
    private float response;
    public int thread_sleep_time = 1000;
    public bool debug_angle = true;
    

    private void ListenerWork()
    {
        UnityEngine.Debug.Log("ListenerWork ");
        AsyncIO.ForceDotNet.Force();
        using (var server = new ResponseSocket())
        {
            server.Bind("tcp://*:12346");

            while (!_listenerCancelled)
            {
                UnityEngine.Debug.Log("!ListenerCancelled");
                Connected = _contactWatch.ElapsedMilliseconds < ContactThreshold;
                string message;
                if (!server.TryReceiveFrameString(out message)) continue;
                _contactWatch.Restart();
                var response = _messageDelegate(message);
                server.SendFrame(response);
                //Thread.Sleep(thread_sleep_time);
            }
        }
        NetMQConfig.Cleanup();
    }

    public NetMqPublisher(MessageDelegate messageDelegate)
    {
        UnityEngine.Debug.Log("Messagedelegate");
        _messageDelegate = messageDelegate;
        _contactWatch = new Stopwatch();
        _contactWatch.Start();
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        _listenerCancelled = false;
        _listenerWorker.Start();
    }

    public void Stop()
    {
        _listenerCancelled = true;
        _listenerWorker.Join();
    }
}


public class ServerObject : MonoBehaviour
{
    public bool Connected;
    private NetMqPublisher _netMqPublisher;
    private string _response; 
    public GameObject cylinder;
    private int ind = 1;
    
    List<string> listA = new List<string>();

    private void Start()
    {
        _netMqPublisher = new NetMqPublisher(HandleMessage);
        _netMqPublisher.Start();


        using (var reader = new StreamReader(@"vive_wheel_data_from_1862245.0000_to_1938044.0000_Dataforwardchickenbrian.csv"))
        {
            
        
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                listA.Add(values[12]);
            }
        }
    }

    private void Update()
    {
        var length = listA.Count;
        _response = listA[ind];
        UnityEngine.Debug.Log(_response + " " + ind);
        ind += 1;
        if (ind == length)
        {
            ind = 1;
        }

    }

    private string HandleMessage(string message)
    {
        // Not on main thread
        return _response;
    }

    private void OnDestroy()
    {
        _netMqPublisher.Stop();
    }
}
