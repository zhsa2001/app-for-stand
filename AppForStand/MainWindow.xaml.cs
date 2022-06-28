using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;
using System.Windows.Data;
using OxyPlot.Legends;

namespace AppForStand
{

    public partial class MainWindow : Window
    {
        private ObservableCollection<Device> Devices { get; set; }
        private List<string> PortsName;
        private List<string> _parameters;
        private List<LineSeries> LineSeries;
        
        



        public PlotModel Model { get; private set; } = new PlotModel
        {
            PlotAreaBorderColor = OxyColors.Gray,
            Axes =
    {
        new LinearAxis { Position = AxisPosition.Bottom },
        new LinearAxis { Position = AxisPosition.Left },
    },
        };

        public MainWindow()
        {
            InitializeComponent();
            initialize();
        }

        private void initialize()
        {
            PortsName = SerialPort.GetPortNames().ToList();           
            PortToSendMessage.ItemsSource = PortsName;
            Devices = new ObservableCollection<Device>();
            portList.ItemsSource = Devices;
            monitorsList.ItemsSource = Devices;
            _parameters = new List<string>();
            LineSeries = new List<LineSeries>();
            foreach (string port in PortsName)
            {
                Devices.Add(new Device { Port = port });
                Devices[Devices.Count - 1].StartReadingFromSerialPort();
                Devices[Devices.Count - 1].PropertyChanged += updateParametersInfo;
                LineSeries.Add(new LineSeries());
                LineSeries[LineSeries.Count - 1].Title = port;
                Model.Series.Add(LineSeries[LineSeries.Count - 1]);
            }
            Closing += Window_Closing;
            var l = new Legend
            {
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.RightTop,
                
            };
            Model.Legends.Add(l);
            graphics.Model = Model;
        }

        private void updateParametersInfo(object sender, PropertyChangedEventArgs e)
        {
            {
                var device = (Device)sender;

                try
                {
                    if (device.ArrayData.Length == 0)
                        return;
                    var _data = device.ArrayData[device.ArrayData.Length - 1];
                    JObject o1 = JObject.Parse(_data);
                    JsonTextReader reader = new JsonTextReader(new StringReader(_data));
                    while (reader.Read())
                    {
                        if (reader.Value != null && reader.TokenType == JsonToken.PropertyName)
                        {
                            if (!_parameters.Contains(reader.Value.ToString()))
                            {
                                _parameters.Add(reader.Value.ToString());
                                try {
                                    Dispatcher.Invoke(new Action(() => { ParametersBox.Items.Add(reader.Value.ToString()); }));
                                     }
                                catch (Exception ex) { 
                                    MessageBox.Show(ex.Message, "");
                                }
                            }
                        }
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (ParametersBox.SelectedIndex != -1)
                        {
                            if (o1[ParametersBox.SelectedValue.ToString()] != null)
                            {
                                var index = Devices.IndexOf(device);
                                if (LineSeries[index].Points.Count >= 30)
                                    LineSeries[index].Points.RemoveAt(0);
                                LineSeries[index].Points.Add(new DataPoint(LineSeries[index].Points.Count, (double)(o1.GetValue(ParametersBox.SelectedValue.ToString()))));
                                for (int i = 0; i < LineSeries[index].Points.Count; i++)
                                {
                                    LineSeries[index].Points[i] = new DataPoint(i, LineSeries[index].Points[i].Y);
                                }
                                Model.InvalidatePlot(true);
                            }
                        }
                    }));
                }
                catch { }
            };
        }
        private void Button_Set_Path_To_Hex(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "hex files (*.hex)|*.hex";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                HexPath.Text = openFileDialog.FileName;
            }      
        }

        private void Button_Update_Ports(object sender, RoutedEventArgs e)
        {
            _parameters = new List<string>();
            ParametersBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            if (ports.Equals(PortsName))
            {
                foreach (var dev in Devices)
                    if (!dev._continue)
                        dev.StartReadingFromSerialPort();
                return;
            }

            foreach (string port in ports)
                if (!PortsName.Contains(port))
                {
                    PortsName.Add(port);
                    Devices.Add(new Device { Port = port });
                    Devices[Devices.Count - 1].PropertyChanged += updateParametersInfo;
                    LineSeries.Add(new LineSeries());               
                    Model.Series.Add(LineSeries[LineSeries.Count - 1]);
                    LineSeries[LineSeries.Count - 1].Title = port;
                    Model.InvalidatePlot(true);
                } else if (!Devices[PortsName.IndexOf(port)]._continue)
            Devices[PortsName.IndexOf(port)].StartReadingFromSerialPort();
            
            for (int i = PortsName.Count - 1; i >= 0; i--)
            {
                if (!ports.Contains(PortsName[i]))
                {
                    PortsName.RemoveAt(i);
                    Devices[i].StopReadingFromSerialPort();
                    Devices.RemoveAt(i);
                    LineSeries.RemoveAt(i);
                    Model.Series.RemoveAt(i);
                    Model.InvalidatePlot(true);
                }

            }
            PortToSendMessage.ItemsSource = new List<string>();
            PortToSendMessage.ItemsSource = PortsName;
        }

        private void Button_Flash(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            if ((string)clickedButton.Content == "Прошить все порты")
                foreach (var device in Devices)
                {
                    device.Load(HexPath.Text);
                }
            else if ((string)clickedButton.Content == "Прошить выбранные порты")
            {
                if (portList.SelectedItem != null)
                {
                    for (int i = 0; i < portList.SelectedItems.Count; i++)
                    {
                        var tmp = (Device)portList.SelectedItems[i];
                        tmp.Load(HexPath.Text);
                    }
                } else MessageBox.Show("Не выбрано ни одного порта для загрузки на него прошивки","", MessageBoxButton.OK);
            }

        }
        private void Button_Cancel_Load(object sender, RoutedEventArgs e)
        {
            foreach (var device in Devices)
            {
                device.CancelLoad();
            }
        }

        private void Menu_Tab_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            switch ((string)btn.Content)
            {
                case "Загрузка прошивки":
                    Ports.Visibility = Visibility.Visible;
                    Monitors.Visibility = Visibility.Collapsed;
                    Graphics.Visibility = Visibility.Collapsed;
                    break;
                case "Мониторы портов":
                    Ports.Visibility = Visibility.Collapsed;
                    Monitors.Visibility= Visibility.Visible;
                    Graphics.Visibility = Visibility.Collapsed;
                    break;
                case "График":
                    Ports.Visibility = Visibility.Collapsed;
                    Monitors.Visibility = Visibility.Collapsed;
                    Graphics.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var device in Devices)
            {
                device.StopReadingFromSerialPort();
            }
        }

        private void Help_Button_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Show();
        }

        private void Button_Send_Message(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < Devices.Count; i++)
            {
                if (Devices[i].Port == (string)PortToSendMessage.SelectedValue)
                {
                    Devices[i].Send(MessageTextBox.Text);
                    MessageTextBox.Text = "";
                    return;
                }
            }
        }

        private void ParametersBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < LineSeries.Count; i++)
            {
                LineSeries[i].Points.Clear();
                Model.InvalidatePlot(true);
            }
        }
    }
}
