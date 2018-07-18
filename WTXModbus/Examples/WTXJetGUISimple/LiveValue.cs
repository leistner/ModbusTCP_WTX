﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Reflection;
using System.Timers;

using HBM.WT.API;
using HBM.WT.API.WTX;
using WTXJetGUISimple.Properties;
using HBM.WT.API.WTX.Jet;

namespace WTXGUISimple
{
    public partial class LiveValue : Form
    {
        const string DEFAULT_IP_ADDRESS = "172.19.103.8";

        private static WtxJet _wtxObj;

        private static System.Timers.Timer _aTimer;
        private static String _ipAddr;
        private static int _timerInterval;

        static JetBusConnection _sConnection;

        static string _menuRequest = "folow instructions: \n \r"
    + "<read> <parameter> \n"
    + "<write> <parameter> <value> \n"
    + "<test> \n"
    + "<show> <filter> \n"
    + "<quit> \n";

        delegate int SelectFunktion(string[] args);
        static Dictionary<string, SelectFunktion> _funktonSelect = new Dictionary<string, SelectFunktion> {
            { "read", ReadParameter },
            { "write", WriteParameter },
            { "show" , ShowProperties },
            { "test" , TestDeviceLayer },
        };


        public LiveValue(string[] args)
        {
            InitializeComponent();

            textBox1.Text = DEFAULT_IP_ADDRESS;

            pictureBox1.Image = WTXJetGUISimple.Properties.Resources.NE107_DiagnosisPassive;

            // Setting the connection for Modbus: 

            /*
             
            s_Connection = new ModbusTCPConnection(ipAddr);
            WTXObj = new Hbm.Wt.WTXInterface.WTX120_Modbus.WTX120_Jet(s_Connection);     // WTX120_Jet umändern 
            WTXObj.getConnection.Connect();
            timerInterval = 200; 
            WTXObj.getConnection.Sending_interval = timerInterval;
            initializeTimerModbus(timerInterval);

            */

            // Setting the connection for jetbus: 

            if (args.Length > 0)
            {
                if (args[0] == "modbus" || args[0] == "Modbus")
                    toolStripStatusLabel2.Text = "Modbus";

                if (args[0] == "jet" || args[0] == "Jet")
                    toolStripStatusLabel2.Text = "Jetbus";
            }

            _timerInterval = 200;

            _ipAddr = "wss://" + args[1];

            Console.Write("Initialize Jet-Peer to address " + _ipAddr + "...");

            _sConnection = new JetBusConnection(_ipAddr, "Administrator", "wtx", delegate { return true; });

            //s_Connection.BusActivityDetection += S_Connection_BusActivityDetection;

            Console.WriteLine("Parameter are fetching: ");

            Console.Write((_sConnection as JetBusConnection).BufferToString());

            _wtxObj = new HBM.WT.API.WTX.WtxJet(_sConnection);
            
            pictureBox1.Image = WTXJetGUISimple.Properties.Resources.NE107_DiagnosisActive;  // Check, ob der Verbindungsaufbau erfolgreich war? 

            InitializeTimerJetbus(_timerInterval);
        }

        // This method initializes the with the timer interval as a parameter: 
        private void InitializeTimerJetbus(int timerInterval)
        {
            // Create a timer with an interval of 500ms. 
            _aTimer = new System.Timers.Timer(timerInterval);

            // Connect the elapsed event for the timer. 
            _aTimer.Elapsed += JetbusOnTimedEvent;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
        }

        // Event method, which will be triggered after a interval of the timer is elapsed- 
        // After triggering (after 500ms) the register is read. 
        private void JetbusOnTimedEvent(Object source, ElapsedEventArgs e)
        {
            int taraValue = _wtxObj.GrossValue - _wtxObj.NetValue;

            textBox2.Invoke(new Action(() =>
            {
                textBox2.Text = "Net:" + _wtxObj.NetGrossValueStringComment(_wtxObj.NetValue, _wtxObj.Decimals) + _wtxObj.UnitStringComment() + Environment.NewLine
                + "Gross:" + _wtxObj.NetGrossValueStringComment(_wtxObj.GrossValue, _wtxObj.Decimals) + _wtxObj.UnitStringComment() + Environment.NewLine
                + "Tara:" + _wtxObj.NetGrossValueStringComment(taraValue, _wtxObj.Decimals) + _wtxObj.UnitStringComment();
                textBox2.TextAlign = HorizontalAlignment.Right;

                pictureBox1.Image = WTXJetGUISimple.Properties.Resources.NE107_DiagnosisActive;
            }));
        }


        // This method initializes the with the timer interval as a parameter: 
        private void InitializeTimerModbus(int timerInterval)
        {
            // Create a timer with an interval of 500ms. 
            _aTimer = new System.Timers.Timer(_timerInterval);

            // Connect the elapsed event for the timer. 
            _aTimer.Elapsed += ModbusOnTimedEventModbus;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
        }

        // Event method, which will be triggered after a interval of the timer is elapsed- 
        // After triggering (after 500ms) the register is read. 
        private void ModbusOnTimedEventModbus(Object source, ElapsedEventArgs e)
        {

            // ++++ !!! Only for Modbus so far !!! +++ :

            //WTXObj.Async_Call(0x00, Read_DataReceived);
        }

        // This is a callback-method, which will be called after the reading of the register is finished. 
        public void Read_DataReceived(IDeviceData deviceValues)
        {
            int taraValue = _wtxObj.GrossValue - _wtxObj.NetValue;

            textBox2.Invoke(new Action(() =>
            {
                textBox2.Text = "Net value : " + _wtxObj.NetValue + " t" + Environment.NewLine + "Gross value : " + _wtxObj.GrossValue + " t" + Environment.NewLine + "Tara value : " + taraValue + " t";

                pictureBox1.Image = WTXJetGUISimple.Properties.Resources.NE107_DiagnosisActive;

            }));


            /*
            textBox2.Invoke(new Action(() =>
            {
                textBox2.Text = "Net:" + WTXObj.netGrossValueStringComment(WTXObj.NetValue, WTXObj.decimals) + WTXObj.unitStringComment() + Environment.NewLine
                + "Gross:" + WTXObj.netGrossValueStringComment(WTXObj.GrossValue, WTXObj.decimals) + WTXObj.unitStringComment() + Environment.NewLine
                + "Tara:" + WTXObj.netGrossValueStringComment(taraValue, WTXObj.decimals) + WTXObj.unitStringComment();
                textBox2.TextAlign = HorizontalAlignment.Right;

                //pictureBox1.Image = Properties.Resources.NE107_DiagnosisActive;
                
            }));
            */
        }

        private void CalculateCalibrationToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void CalibrationWithWeightToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void calculateCalibrationToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

        }

        private void calibrationWithWeightToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private static int ReadParameter(string[] args)
        {
            Console.Write(args[0] + "Read... ");
            if (args.Length < 2) return -1;
            int intValue = _sConnection.Read<int>(args[1]);

            Console.WriteLine(intValue);
            return 0;
        }

        private static int WriteParameter(string[] args)
        {
            Console.Write(args[0] + "Write... ");
            if (args.Length < 3) return -1;

            int value = Convert.ToInt32(args[2]);
            _sConnection.Write<int>(args[1], value);
            Console.WriteLine("OK");

            return 0;
        }

        private static int ShowProperties(string[] args)
        {
            BaseWtDevice parameter = new HBM.WT.API.WTX.WtxJet(_sConnection);

            //HBM.WT.API.COMMON.BaseWTDevice parameter = new Hbm.Wt.WTXInterface.WTX120_Jet.WTX120_Jet(s_Connection, 100);
            // Before : //Hbm.Wt.WTXInterface.DeviceAbstract parameter = new Hbm.Wt.WTXInterface.WTX120_Jet.WTX120_Jet(s_Connection,100);

            Type type = parameter.GetType();

            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                Console.WriteLine(prop.ToString());
            }
            return 0;
        }

        private static uint StringToId(string arg)
        {
            if (arg.Contains("0x"))
            {
                return Convert.ToUInt32(arg, 16);
            }
            else
            {
                return Convert.ToUInt32(arg, 10);
            }
        }

        private static int TestDeviceLayer(string[] arg)
        {
            BaseWtDevice parameter = new HBM.WT.API.WTX.WtxJet(_sConnection);

            if (true)
            {
                parameter = new HBM.WT.API.WTX.WtxJet(_sConnection);
                

            }
            else
            {
                //parameter = new HBM.WT.API.WTX.WTXJet(s_Connection);
            }


            /*
            Console.Write("Read Measure... ");
            int value = parameter.MeasureValue;
            Console.WriteLine(value);
            //int statusValue = parameter.MeasureValueType;

            Console.Write("Write DPT... ");
            parameter.DecimalPonit = 4;
            Console.WriteLine("OK");

            Console.WriteLine("Read Parameter success");
            */

            return 0;

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void LiveValue_Load(object sender, EventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {

        }
    }
}
