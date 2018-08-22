﻿using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;


// Look also on the tests on GitHub at a related project for SharpJet : https://github.com/gatzka/SharpJet/tree/master/SharpJetTests

namespace HBM.WT.API.WTX.Jet
{
    using Hbm.Devices.Jet;
    using HBM.WT.API;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Net.Security;
    using System.Threading;

    public enum Behavior
    {
        ConnectionFail,
        ConnectionSuccess,

        ReadFail,
        ReadSuccess,
    }

    public class TestJetbusConnection : INetConnection, IDisposable
    {
        private Behavior behavior;
        private List<string> messages;
        private bool connected;

        public event EventHandler BusActivityDetection;
        public event EventHandler<DataEvent> RaiseDataEvent;

        private int _mTimeoutMs;

        private Dictionary<string, JToken> _mTokenBuffer;

        private AutoResetEvent _mSuccessEvent = new AutoResetEvent(false);

        protected JetPeer MPeer;

        private bool JetConnected;
        private Exception _mException = null;

        private string IP;
        private int interval;

        // Constructor with all parameters possible from class 'JetbusConnection' - Without ssh certification.
        //public TestJetbusConnection(Behavior behavior, string ipAddr, string user, string passwd, RemoteCertificateValidationCallback certificationCallback, int timeoutMs = 5000) : base(ipAddr, user, passwd, certificationCallback, timeoutMs = 5000)

        public TestJetbusConnection(Behavior behavior, string ipAddr, string user, string passwd, RemoteCertificateValidationCallback certificationCallback, int timeoutMs = 5000)
        {
            this.JetConnected = false;
            this.behavior = behavior;
            this.messages = new List<string>();

            _mTokenBuffer = new Dictionary<string, JToken>();

            this._mTimeoutMs = 5000; // values of 5000 according to the initialization in class JetBusConnection. 

            //ConnectOnPeer(user, passwd, timeoutMs);
            //FetchAll();
        }

        public int SendingInterval
        {
            get
            {
                return this.interval;
            }
            set
            {
                this.interval = value;
            }
        }

        protected JToken ReadObj(object index)
        {
            
            switch (this.behavior)
            {
                case Behavior.ReadSuccess:
                    if (_mTokenBuffer.ContainsKey(index.ToString()))
                        return _mTokenBuffer[index.ToString()];
                    break;
                case Behavior.ReadFail:
                    //throw new InterfaceException(new KeyNotFoundException("Object does not exist in the object dictionary"), 0);
                    return _mTokenBuffer[""];
                    break;

                default:
                    break;

            }

            return 0;
        }

        public Dictionary<string, JToken> getTokenBuffer
        {
            get
            {
                return this._mTokenBuffer;
            }
        }

        public int NumofPoints { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsConnected
        {
            get
            {
                return this.JetConnected;
            }

            set
            {
                this.JetConnected = value;
            }
        }

        public string IpAddress
        {
            get
            {
                return this.IP;
            }

            set
            {
                this.IP = value;
            }
        }

        public virtual void ConnectOnPeer(string user, string passwd, int timeoutMs = 5000)   // before it was "protected". 
        {
            MPeer.Connect(delegate (bool connected) {
                if (connected)
                {

                    this.JetConnected = true;

                    MPeer.Authenticate(user, passwd, delegate (bool success, JToken token) {
                        if (!success)
                        {

                            this.JetConnected = false;
                            JetBusException exception = new JetBusException(token);
                            _mException = new InterfaceException(exception, (uint)exception.Error);
                        }
                        _mSuccessEvent.Set();
                    }, _mTimeoutMs);
                }
                else
                {
                    this.JetConnected = false;
                    _mException = new Exception("Connection failed");
                    _mSuccessEvent.Set();
                }
            }, timeoutMs);
            _mTimeoutMs = timeoutMs;
            WaitOne(2);
        }

        public void FetchAll()
        {
            this.ConnectOnPeer("wss://172.19.103.8:443/jet/canopen", "Administrator", 100);

            Matcher matcher = new Matcher();
            FetchId id;

            MPeer.Fetch(out id, matcher, OnFetchData, delegate (bool success, JToken token)
            {
                if (!success)
                {
                    this.JetConnected = false;
                    JetBusException exception = new JetBusException(token);
                    _mException = new InterfaceException(exception, (uint)exception.Error);
                   
                }
                //
                // Wake up the waiting thread where call the konstruktor to connect the session
                //
                _mSuccessEvent.Set();

                BusActivityDetection?.Invoke(this, new LogEvent("Fetch-All success: " + success + " - buffersize is " + _mTokenBuffer.Count));

                //BusActivityDetection?.Invoke(this, new NetConnectionEventArgs<string>(EventArgType.Message, "Fetch-All success: " + success + " - buffersize is: " + _mTokenBuffer.Count));

            }, _mTimeoutMs);
            WaitOne(3);
        }

        protected virtual void WaitOne(int timeoutMultiplier = 1)
        {
            if (!_mSuccessEvent.WaitOne(_mTimeoutMs * timeoutMultiplier))
            {

                this.JetConnected = false;
                //
                // Timeout-Exception
                //
                throw new InterfaceException(new TimeoutException("Interface Timeout - signal-handler will never reset"), 0x1);
            }
            if (_mException != null)
            {
                Exception exception = _mException;
                _mException = null;
                throw exception;
            }
        }

        /// <summary>
        /// Event with callend when raced a Fetch-Event by a other Peer.
        /// For testing it must be filled with pseudo data be tested in the UNIT tests. 
        /// </summary>
        /// <param name="data"></param>
        protected void OnFetchData(JToken data)
        {
            string path = data["path"].ToString();
            lock (_mTokenBuffer)
            {

                _mTokenBuffer.Add("6144/00", this.simulateFetchInstance()["value"]);

                //_mTokenBuffer.Add("6144 / 00", data["value"]);

                /*
                switch (data["event"].ToString())
                {
                    case "add": _mTokenBuffer.Add(path, data["value"]); break;
                    case "fetch": _mTokenBuffer[path] = data["value"]; break;
                    case "change":
                        _mTokenBuffer[path] = data["value"];
                        break;
                }
                */

                BusActivityDetection?.Invoke(this, new LogEvent(data.ToString()));
            }
        }


        public JToken simulateFetchInstance()
        {

            FetchData fetchInstance = new FetchData
            {
                path = "6144/00",
                Event = "gross",
                value = 12345,

            };

            return JToken.FromObject(fetchInstance);
        }


        public new void Connect()
        {
            switch (this.behavior)
            {
                case Behavior.ConnectionFail:
                    connected = false;
                    break;

                case Behavior.ConnectionSuccess:
                    connected = true;
                    break;

                default:
                    connected = true;
                    break;
            }
        }



        public bool isConnected()
        {
            return this.connected;
        }

        public new void Disconnect()
        {
            throw new NotImplementedException();
        }

        public int Read(object index)
        {
            _mTokenBuffer.Add("6144/00", this.simulateFetchInstance()["value"]);

            try
            {
                return Convert.ToInt32(ReadObj(index));

                //JToken token = ReadObj(index);
                //return token;

                //return (T)Convert.ChangeType(token, typeof(T));
            }
            catch (FormatException)
            {
                throw new InterfaceException(new FormatException("Invalid data format"), 0);
            }
        }


        public new void Write(object index, int data)
        {
            throw new NotImplementedException();
        }

        public new void WriteArray(ushort index, ushort[] data)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(string json)
        {
            messages.Add(json);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public class FetchData
        {
            public string path { get; set; }
            public string Event { get; set; }
            public int value { get; set; }
        }


    }
}