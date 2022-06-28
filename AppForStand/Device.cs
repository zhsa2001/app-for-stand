using ArduinoUploader;
using ArduinoUploader.Hardware;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AppForStand
{
    internal class Device : INotifyPropertyChanged
    {
        Task t2;
        private SerialPort _serialPort;
        public bool _continue { get; private set; } = false;
        public string Port { get; set; }
        public string Model { get; set; }
        private string _baudRate;

        public string BaudRate
        {
            get => _baudRate;
            set
            {
                _baudRate = value;
                StopReadingFromSerialPort();
                StartReadingFromSerialPort();
            }
        }
        private Queue<string> _receivedData = new Queue<string>();
        
        public string ReceivedData 
        {
            get
            {
                string res = "";
                lock (_receivedData)
                {
                    foreach (string _data in _receivedData)
                    {
                        if (_data == null)
                            continue;
                        res += _data;
                        res += "\n";
                    }

                    return res;
                }
            }
            set { } 
        }

        private CancellationTokenSource _tokenSourceForLoad;
        private CancellationTokenSource _tokenSourceForRead;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public string[] ArrayData => _receivedData.ToArray(); 
        public void CancelLoad()
        {
            if (_tokenSourceForLoad != null)
                _tokenSourceForLoad.Cancel();
        }
        public void Load(string firmwareName)
        {
            if (!firmwareName.EndsWith(".hex") || !Port.StartsWith("COM"))
            {
                MessageBox.Show("Проверьте выбранный файл прошивки и порт " + Port, "", MessageBoxButton.OK);
                return;
            }
                
            int model = 0;
            switch (Model)
            {
                case "NanoR3(OldBootloader)":
                    model = (int)ArduinoModel.NanoR3;
                    break;
                case "NanoR3":
                case "UnoR3":
                    model = (int)ArduinoModel.UnoR3;
                    break;
                case "NanoR2":
                    model = (int)ArduinoModel.NanoR2;
                    break;
                case "Micro":
                    model = (int)ArduinoModel.Micro;
                    break;
                case "Mega2560":
                    model = (int)ArduinoModel.Mega2560;
                    break;
                case "Mega1284":
                    model = (int)ArduinoModel.Mega1284;
                    break;
                case "Leonardo":
                    model = (int)ArduinoModel.Leonardo;
                    break;
                default:
                    MessageBox.Show("Проверьте выбранный тип устройства на порту " + Port, "", MessageBoxButton.OK);
                    return;
            }
            
            _tokenSourceForLoad = new CancellationTokenSource();
            StopReadingFromSerialPort();
            //if (t2 != null)
            //{
            //    _tokenSourceForRead.Cancel();
            //}
            //while (t2.IsCompleted) { }

            var uploader = new ArduinoSketchUploader(
                    new ArduinoSketchUploaderOptions()
                    {
                        FileName = firmwareName,
                        PortName = Port,
                        ArduinoModel = (ArduinoModel)model
                    });

            Task task_Load = new Task(() => {
                var t = Thread.CurrentThread;
                using (_tokenSourceForLoad.Token.Register(t.Abort))
                {
                    uploader.UploadSketch();
                }
            }, _tokenSourceForLoad.Token);
            
            Task t3 = Task.Run(() => {
                bool isOk = true;
                try
                {
                    task_Load.Start();
                    task_Load.Wait(-1);
                }
                catch(Exception ex)
                {
                    Trace.WriteLine("Message"+ex.Message);
                    isOk = false;
                    MessageBox.Show("Не удалось загрузить прошивку на порт " + Port, "", MessageBoxButton.OK);
                } 
                if (isOk)
                    MessageBox.Show("Прошивка загружена на порт " + Port, "", MessageBoxButton.OK);
                StartReadingFromSerialPort();
            });
            
        }

        public void StartReadingFromSerialPort()
        {
            _tokenSourceForRead = new CancellationTokenSource();
             t2 = Task.Run(() =>
            {
                var t = Thread.CurrentThread;
                using (_tokenSourceForRead.Token.Register(t.Abort))
                {
                    //if (_serialPort == null || !_serialPort.IsOpen)
                    //{
                    _serialPort = new SerialPort();
                    _serialPort.PortName = Port;
                    _serialPort.BaudRate = int.Parse(_baudRate == null ? "9600" : _baudRate);
                    _serialPort.ReadTimeout = 1000;
                    _serialPort.WriteTimeout = 1000;
                    _serialPort.Encoding = new UTF8Encoding();
                    try { _serialPort.Open(); }
                    catch(UnauthorizedAccessException ex)
                    {
                        MessageBox.Show("Ooops");
                    }
                    //System.Threading.Thread.Sleep(1000);
                    _continue = true;
                    Thread readThread = new Thread(Read);
                    readThread.Start();
                    //readThread.Join();
                    //_serialPort.Close();
                    //return;
                    //}
                    //else if (_serialPort.IsOpen)
                    //{
                    //    _serialPort.Close();
                    //    StartReadingFromSerialPort();
                    //}
                }
                
            });
        }

        public void StopReadingFromSerialPort()
        {
            _continue = false;
            if (_serialPort.IsOpen)
            {
                if (_tokenSourceForRead != null)
                    _tokenSourceForRead.Cancel();
                _serialPort.Close();
            }
            
        }

        public void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    _receivedData.Enqueue(message);
                    if (_receivedData.Count > 30)
                    {
                        _receivedData.Dequeue();
                    }
                }
                catch (TimeoutException) 
                {
                    _receivedData.Enqueue(null);
                }
                catch (Exception) 
                {
                    StopReadingFromSerialPort();

                }
                
                OnPropertyChanged("ReceivedData");
            }
        }
        public void Send(string msg)
        {
            if (msg == null)
                return;
            try
            {
                _serialPort.WriteLine(msg);
            }
            catch (InvalidOperationException){
                MessageBox.Show("Порт " + Port + " закрыт. Отправка данных не выполнена.");
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Не удалось выполнить отправку данных на порт " + Port, "" );
            }
            
        }

        ~Device()
        {
            StopReadingFromSerialPort();
        }
    }
}
