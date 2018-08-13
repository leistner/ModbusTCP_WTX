﻿
namespace HBM.WT.API.WTX.Modbus
{
    using HBM.WT.API;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public enum Behavior
    {

         ConnectionFail, 
         ConnectionSuccess,

         DisconnectionFail,
         DisconnectionSuccess,
         
         ReadFail,
         ReadSuccess,

         WriteFail,
         WriteSuccess,

         WriteArrayFail,
         WriteArraySuccess,

         MeasureZeroFail,
         MeasureZeroSuccess,

         TareFail,
         TareSuccess,

    }

    public class TestModbusTCPConnection : ModbusTcpConnection
    {
        private Behavior behavior;
        private List<int> messages;

        private ushort arrayElement1;
        private ushort arrayElement2;

        private bool _connected;
        private ushort[] _data;
        public int command; 

        public event EventHandler BusActivityDetection;
        public override event EventHandler<DataEvent> RaiseDataEvent;

        public TestModbusTCPConnection(Behavior behavior,string ipAddress) : base(ipAddress)
        {
            _data = new ushort[38];

            this.behavior = behavior;
            this.messages = new List<int>();
        }

        public List<int> getMessages
        {
            get
            {
                return this.messages;
            }
        }

        public void Connect()
        {
            switch(this.behavior)
            {
                case Behavior.ConnectionFail:
                    _connected = false;
                    break;

                case Behavior.ConnectionSuccess:
                    _connected = true;
                    break;

                default:
                    _connected = true;
                    break; 
            }
    }

        public bool isConnected()
        {
            return this._connected;
        }

        public new void Disconnect()
        {
            switch (this.behavior)
            {
                case Behavior.DisconnectionFail:
                    _connected = true;
                    break;

                case Behavior.DisconnectionSuccess:
                    _connected = false;
                    break;

                default:
                    _connected = true;
                    break;
            }
        }

        public int Read(object index)
        {
            if (_connected)
                ReadRegisterPublishing(new DataEvent(_data));

            return 0;
        }

        public override void ReadRegisterPublishing(DataEvent e) // 25.4 Comment : 'virtual' machte hier probleme beim durchlaufen :o 
        {
            // Behavoir : Kann in Standard oder Filler Mode sein, kann unterschiedliche "NumInputs" haben. Dementsprechend abhängig
            // ist die Anzahl der eingelesenen Werte. Erstmal vom einfachen Fall ausgehen! 

            switch (this.behavior)
            {
                case Behavior.ReadFail:

                    // If there is a connection fail, all data attributes get 0 as value.

                    for (int index = 0; index < _data.Length; index++)
                    {
                        _data[index] = 0;
                    }
                    BusActivityDetection?.Invoke(this, new LogEvent("Read failed : Registers have not been read"));
                    break;

                case Behavior.ReadSuccess:

                    // The most important data attributes from the WTX120 device: 

                    _data[0] = 17000;       // Net value
                    _data[1] = 17000;       // Gross value
                    _data[2] = 0;           // General weight error
                    _data[3] = 0;           // Scale alarm triggered
                    _data[4] = 0;           // Limit status
                    _data[5] = 0;           // Weight moving
                    _data[6] = 1;           // Scale seal is open
                    _data[7] = 0;           // Manual tare
                    _data[8] = 0;           // Weight type
                    _data[9] = 0;           // Scale range
                    _data[10] = 0;          // Zero required/True zero
                    _data[11] = 0;          // Weight within center of zero 
                    _data[12] = 0;          // weight in zero range
                    _data[13] = 0;          // Application mode = 0
                    _data[14] = 4;          // Decimal Places
                    _data[15] = 2;          // Unit
                    _data[16] = 0;          // Handshake
                    _data[17] = 0;          // Status

                    BusActivityDetection?.Invoke(this, new LogEvent("Read successful: Registers have been read"));

                    break;

                default:
                    for (int index = 0; index < _data.Length; index++)
                    {
                        _data[index] = 0;
                    }
                    BusActivityDetection?.Invoke(this, new LogEvent("Read failed : Registers have not been read"));
                    break; 
            }

            //_data = e.Args;


            e.Args = _data;

            var handler = RaiseDataEvent;

            //If a subscriber exists: 
            if (handler != null) handler(this, e);
        }

        public int getCommand
        {
            get { return this.command; }
        }

        public void Write(object index, int data)
        {
            this.messages.Add(data);        // New : 10.8.18

            command = data; 


            /*
            switch (this.behavior)
            {
                case Behavior.WriteFail:
                    _data[16] = 0;

                    if (_data[17] == 0) // _data[17] = Do not invert the status bit.
                        _data[17] = 0;
                    if (_data[17] == 1)
                        _data[17] = 1;

                    break;

                case Behavior.WriteSuccess:
                    _data[16] = 1;      // _data[16] = Handshake.
                    Thread.Sleep(500);
                    _data[16] = 0;

                    if (_data[17] == 0) // _data[17] = Invert the status bit according to the Handshake protocol.
                        _data[17] = 1;
                    if (_data[17] == 1)
                        _data[17] = 0;

                    break;

                default:
                    break;

            }
            */
        }

        public new void WriteArray(ushort index, ushort[] data)
        {      
            switch(this.behavior)
            {
                case Behavior.WriteArrayFail:
                    arrayElement1 = 0;
                    arrayElement2 = 0; 

                    break;

                case Behavior.WriteArraySuccess:
                    arrayElement1 = data[0];
                    arrayElement2 = data[1];

                    break;

                default:
                    break; 
            }
        }

        public ushort getArrElement1
        {
            get
            {
                return this.arrayElement1;
            }
        }

        public ushort getArrElement2
        {
            get
            {
                return this.arrayElement2;
            }
        }
    }
}
