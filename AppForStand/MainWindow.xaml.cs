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

namespace AppForStand
{

    public partial class MainWindow : Window
    {
        private ObservableCollection<Device> Devices { get; set; }
        private List<string> PortsName;
        private List<string> _parameters;
        
        



        public PlotModel Model { get; private set; } = new PlotModel
        {
            //Title = "Hello WinUI 3",
            PlotAreaBorderColor = OxyColors.Gray,
            Axes =
    {
        new LinearAxis { Position = AxisPosition.Bottom },
        new LinearAxis { Position = AxisPosition.Left },
    },
            /*Series =
    {
        new LineSeries
        {
            Title = "LineSeries",
            MarkerType = MarkerType.None,
            Color = OxyColors.Automatic,
            Points =
            {
                new DataPoint(0, 0),
                new DataPoint(10, 18),
                new DataPoint(20, 12),
                new DataPoint(30, 8),
                new DataPoint(40, 15),
            }
        },
        new LineSeries
        {
            Title = "LineSeries",
            MarkerType = MarkerType.None,
            Color = OxyColors.Automatic,
            Points =
            {
                new DataPoint(10, 0),
                new DataPoint(20, 18),
                new DataPoint(30, 12),
                new DataPoint(40, 8),
                new DataPoint(50, 15),
            }
        },
        new LineSeries
        {
            Title = "LineSeries",
            MarkerType = MarkerType.None,
            Color = OxyColors.Automatic,
            Points =
            {
                new DataPoint(20, 0),
                new DataPoint(30, 18),
                new DataPoint(40, 12),
                new DataPoint(50, 8),
                new DataPoint(60, 15),
            }
        },
        new LineSeries
        {
            Title = "LineSeries",
            MarkerType = MarkerType.None,
            Color = OxyColors.Automatic,
            Points =
            {
                new DataPoint(30, 0),
                new DataPoint(40, 18),
                new DataPoint(50, 12),
                new DataPoint(60, 8),
                new DataPoint(70, 15),
            }
        },
        
        new LineSeries
        {
            Title = "LineSeries",
            MarkerType = MarkerType.None,
            Color = OxyColors.Automatic,
            Points =
            {
                new DataPoint(50, 0),
                new DataPoint(60, 18),
                new DataPoint(70, 12),
                new DataPoint(80, 8),
                new DataPoint(90, 15),
            }
        },
    } */
        };

        public MainWindow()
        {
            InitializeComponent();
            initialize();
        }

        private void initialize()
        {
            PortsName = SerialPort.GetPortNames().ToList();
            
            //ParameterCounters = new List<int>();

            PortToSendMessage.ItemsSource = PortsName;
            Devices = new ObservableCollection<Device>();
            portList.ItemsSource = Devices;
            monitorsList.ItemsSource = Devices;

            _parameters = new List<string>();


            //comboBox1.DisplayMember = "Name";


            //ParametersBox.Items.Add("kot");
            //ParametersBox.Items.Add("krot");




            foreach (string port in PortsName)
            {
                Devices.Add(new Device { Port = port });
                Devices[Devices.Count - 1].StartReadingFromSerialPort();
                Devices[Devices.Count - 1].PropertyChanged += updateParametersInfo;
            }
            //Devices[0].StartReadingFromSerialPort();


            Closing += Window_Closing;
            var ls2 = new LineSeries();
            //ls2.Points.
            ls2.Points.Add(new DataPoint(20, 20));
            ls2.Points.Add(new DataPoint(30, 20));

            Model.Series.Add(ls2);
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
                    //MessageBox.Show(_data);
                    while (reader.Read())
                    {
                        //MessageBox.Show("зашли",reader.TokenType.ToString());
                        if (reader.Value != null && reader.TokenType == JsonToken.PropertyName)
                        {
                            //MessageBox.Show(reader.Value.ToString(), "1");
                            if (!_parameters.Contains(reader.Value.ToString()))
                            {
                                _parameters.Add(reader.Value.ToString());
                                //MessageBox.Show(reader.Value.ToString(), "2");
                                //ParametersBox.ItemsSource = new List<string>();

                                try {
                                    Dispatcher.Invoke(new Action(() => { ParametersBox.Items.Add(reader.Value.ToString()); }));
                                     }
                                catch (Exception ex) { MessageBox.Show("Nu bliiiin", "");
                                    MessageBox.Show(ex.Message, "");
                                }
                                //MessageBox.Show(reader.Value.ToString(), "3");
                            }
                        }

                       
                        //if (reader.Value != null)
                        //{
                        //Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                        //  MessageBox.Show("Token: " + reader.TokenType + ", Value: " + reader.Value, "");
                        //}
                        //else
                        //{
                        //  Console.WriteLine("Token: {0}", reader.TokenType);
                        //}
                        //if (o1["CPU"] != null)
                        //  MessageBox.Show("ok", "");
                        //else MessageBox.Show("not ok", "");
                    }
                    //box.ItemsSource = Parameters.ToArray();
                    /*for (int i = 0; i < ParameterCounters.Count; i++)
                    {
                        ParameterCounters[i]--;
                        if (ParameterCounters[i] == 0)
                        {
                            Parameters.RemoveAt(i);
                            ParameterCounters.RemoveAt(i);
                            foreach (var parameter in Parameters)
                                MessageBox.Show(parameter.ToString() + " " + Parameters.Count.ToString());


                        }
                    }*/

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
            string rrr = "";
            for (int i = 0; i < _parameters.Count; i++)
                rrr += _parameters[i];
            MessageBox.Show(rrr);
            //ParametersBox.ItemsSource = new List<string>() { };
            //Parameters.RemoveAll(str => true);
            //ParametersBox.ItemsSource = null;
            //foreach (string port in PortsName)
            //{

            //    Devices[Devices.Count - 1].PropertyChanged += updateParametersInfo;
            //}

            //ParametersBox.Items.Clear();
            _parameters = new List<string>();
            ParametersBox.Items.Clear();

             var ls2 = new LineSeries();
                        //ls2.Points.
                        ls2.Points.Add(new DataPoint(10, 10));
                        ls2.Points.Add(new DataPoint(30, 20));
                        Model.Series.Clear();
                        Model.Series.Add(ls2);
                        Model.InvalidatePlot(true);
                        //graphics.
                        //graphics.Model = Model;
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
                            } else if (!Devices[PortsName.IndexOf(port)]._continue)
                                Devices[PortsName.IndexOf(port)].StartReadingFromSerialPort();
                        for (int i = 0; i < PortsName.Count; i++)
                        {
                            if (!ports.Contains(PortsName[i]))
                            {
                                PortsName.RemoveAt(i);
                                Devices[i].StopReadingFromSerialPort();
                                Devices.RemoveAt(i);
                            }

                        }
                        PortToSendMessage.ItemsSource = new List<string>();
                        PortToSendMessage.ItemsSource = PortsName;

            //Parameters = 
            //ParameterCounters = new List<int>();
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
                    //timer.Stop();
                    break;
                case "Мониторы портов":
                    Ports.Visibility = Visibility.Collapsed;
                    Monitors.Visibility= Visibility.Visible;
                    Graphics.Visibility = Visibility.Collapsed;
                    //timer.Stop();
                    break;
                case "График":
                    Ports.Visibility = Visibility.Collapsed;
                    Monitors.Visibility = Visibility.Collapsed;
                    Graphics.Visibility = Visibility.Visible;
                    //timer.Start();
                    break;
            }
        }

        private void Draw() { }

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
    }
}
