﻿// <copyright file="LiveValue.cs" company="Hottinger Baldwin Messtechnik GmbH">
//
// WTXGUIsimple, a demo application for HBM Weighing-API  
//
// The MIT License (MIT)
//
// Copyright (C) Hottinger Baldwin Messtechnik GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// </copyright>
using HBM.WT.API;
using HBM.WT.API.WTX;
using HBM.WT.API.WTX.Jet;
using HBM.WT.API.WTX.Modbus;
using System;
using System.Windows.Forms;
using WTXModbusGUIsimple;

/*
 * This application example enables communication and a connection to the WTX120 device via 
 * Modbus and Jetbus.
 */
namespace WTXGUIsimple
{
    public partial class LiveValue : Form
    {
        #region Locales
        private const string DEFAULT_IP_ADDRESS = "192.168.100.88";

        private const string MESSAGE_CONNECTION_FAILED = "Connection failed!";
        private const string MESSAGE_CONNECTING = "Connecting...";

        private string _ipAddress = DEFAULT_IP_ADDRESS;

        private int _timerInterval = 200;
        
        private static BaseWtDevice _wtxDevice;

        private AdjustmentCalculator _adjustmentCalculator;
        private AdjustmentWeigher _adjustmentWeigher;
        #endregion
        

        #region Constructor
        public LiveValue()
        {
            InitializeComponent();

            txtInfo.Text = "To Connect to the WTX device, please enter an IP address, select 'Jet' or 'Modbus/TCP' and press 'connect'.";

            txtIPAddress.Text = _ipAddress;
        }
        
        public LiveValue(string[] args)
        {
            InitializeComponent();

            txtInfo.Text = "To Connect to the WTX device, please enter an IP address, select 'Jet' or 'Modbus/TCP' and press 'connect'.";

            EvaluateCommandLine(args);      

            txtIPAddress.Text = _ipAddress;
        }
        #endregion

        
        #region Command line
        private void EvaluateCommandLine(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "modbus")
                {              
                    picConnectionType.Image = WTXGUIsimple.Properties.Resources.modbus_symbol;
                    rbtConnectionModbus.Checked = true;
                }
                if (args[0].ToLower() == "jet")
                {
                    picConnectionType.Image = WTXGUIsimple.Properties.Resources.jet_symbol;
                    rbtConnectionJet.Checked = true;
                }
            }

            if (args.Length > 1)
                _ipAddress = args[1];

            if (args.Length > 2)
                this._timerInterval = Convert.ToInt32(args[2]);
        }
        #endregion


        #region Connection
        // This method connects to the given IP address
        private void Connect()
        {
            picNE107.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisPassive;
            picConnectionType.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisPassive;
            txtInfo.Text = "Connecting...";
            this._ipAddress = txtIPAddress.Text;

            if (this.rbtConnectionModbus.Checked )
            {
                ModbusTcpConnection _modbusConection = new ModbusTcpConnection(this._ipAddress);

                _wtxDevice = new WtxModbus(_modbusConection, this._timerInterval);

                _wtxDevice.getConnection.NumofPoints = 6;

                try
                {
                    _wtxDevice.Connect();
                    //_wtxDevice.getConnection.Connect();
                }
                catch(Exception)
                {
                    txtInfo.Text = MESSAGE_CONNECTION_FAILED;
                }

                if (_wtxDevice.isConnected == true)
                {
                    picConnectionType.Image = WTXGUIsimple.Properties.Resources.modbus_symbol;
                    picNE107.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisActive;
                    _wtxDevice.DataUpdateEvent += Update;
                }
                else
                {
                    picNE107.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisPassive;
                    txtInfo.Text = MESSAGE_CONNECTION_FAILED;
                }

            }
            else
                if(this.rbtConnectionJet.Checked)
                {
                    JetBusConnection _jetConnection = new JetBusConnection(_ipAddress, "Administrator", "wtx");

                    _wtxDevice = new WtxJet(_jetConnection);

                    _wtxDevice.Connect();

                try
                {
                    _wtxDevice.Connect();
                }
                catch (Exception)
                {
                    txtInfo.Text = MESSAGE_CONNECTION_FAILED;
                }

                if (_wtxDevice.isConnected == true)
                    {
                        _wtxDevice.DataUpdateEvent += Update;
                        this.Update(this,null);
                        picConnectionType.Image = WTXGUIsimple.Properties.Resources.jet_symbol;
                        picNE107.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisActive;

                    }
                    else
                    {
                        picNE107.Image = WTXGUIsimple.Properties.Resources.NE107_DiagnosisPassive;
                        txtInfo.Text = MESSAGE_CONNECTION_FAILED;
                    }                    
                }
        }
      
        //Callback for automatically receiving event based data from the device
        private void Update(object sender, DataEvent e)
        {         
            txtInfo.Invoke(new Action(() =>
            {
                int taraValue = _wtxDevice.NetValue - _wtxDevice.GrossValue;

                txtInfo.Text = "Net:" + _wtxDevice.NetGrossValueStringComment(_wtxDevice.NetValue, _wtxDevice.Decimals) + _wtxDevice.UnitStringComment() + Environment.NewLine
                + "Gross:" + _wtxDevice.NetGrossValueStringComment(_wtxDevice.GrossValue, _wtxDevice.Decimals) + _wtxDevice.UnitStringComment() + Environment.NewLine
                + "Tara:" + _wtxDevice.NetGrossValueStringComment(taraValue, _wtxDevice.Decimals) + _wtxDevice.UnitStringComment();
                txtInfo.TextAlign = HorizontalAlignment.Right;
            }));

        }

        private void WriteDataCompleted(IDeviceData obj)
        {
        }
        #endregion


        #region Button clicks
        //Connect device
        private void cmdConnect_Click(object sender, EventArgs e)
        {
            this.Connect();
        }


        // button click event for switching to gross or net value. 
        private void cmdGrossNet_Click(object sender, EventArgs e)
        {
                _wtxDevice.gross(WriteDataCompleted);
        }

        // button click event for zeroing
        private void cmdZero_Click(object sender, EventArgs e)
        {
                _wtxDevice.zeroing(WriteDataCompleted);
        }

        // button click event for taring 
        private void cmdTare_Click(object sender, EventArgs e)
        {
            _wtxDevice.taring(WriteDataCompleted);
        }

        //Method for calculate adjustment with dead load and span: 
        private void calibrationWithWeightToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            _adjustmentCalculator = new AdjustmentCalculator(_wtxDevice);            
            DialogResult res = _adjustmentCalculator.ShowDialog();
        }


        //Method for adjustment with weight: 
        private void calibrationToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            _adjustmentWeigher = new AdjustmentWeigher(_wtxDevice);
            DialogResult res = _adjustmentWeigher.ShowDialog();
        }
        #endregion

        private void picConnectionType_Click(object sender, EventArgs e)
        {

        }

        private void rbtConnectionModbus_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void LiveValue_Load(object sender, EventArgs e)
        {

        }
    }
}
