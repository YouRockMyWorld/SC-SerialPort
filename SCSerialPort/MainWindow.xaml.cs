using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;

namespace SCSerialPort
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort serialPort = new SerialPort();
        private System.Windows.Threading.DispatcherTimer timer;
        private byte[] command_angle;
        private byte[] command_accel;
        private bool is_collect_angel = true;
        private bool is_collect_accel = false;
        private bool is_begin = false;
        private int data_count = 0;
        private int data_size = 0;
        private BitmapImage img_begin;
        private BitmapImage img_end;
        private int mode = 1;
        public MainWindow()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            //System.Diagnostics.Debug.WriteLine(SerialPort.GetPortNames().ToList().Count);
            cbb_ComName.ItemsSource = SerialPort.GetPortNames();
            timer = new System.Windows.Threading.DispatcherTimer();
            img_begin = new BitmapImage(new Uri("pack://application:,,,/image/Begin.png"));
            img_end = new BitmapImage(new Uri("pack://application:,,,/image/End.png"));
            btn_image.Source = img_begin;
            timer.Tick += new EventHandler(SendData);
            if (cbb_ComName.Items.Count > 0) cbb_ComName.SelectedIndex = 0;

            cbb_BaudRate.Items.Add(4800);
            cbb_BaudRate.Items.Add(9600);
            cbb_BaudRate.Items.Add(19200);
            cbb_BaudRate.Items.Add(38400);
            cbb_BaudRate.Items.Add(56000);
            cbb_BaudRate.Items.Add(115200);
            if (cbb_BaudRate.Items.Count > 0) cbb_BaudRate.SelectedIndex = 1;

            cbb_items.Items.Add("三轴角度");
            cbb_items.Items.Add("三轴加速度");
            cbb_items.Items.Add("角度和加速度");
            if (cbb_items.Items.Count > 0) cbb_items.SelectedIndex = 0;

            cbb_frequency.Items.Add(1);
            cbb_frequency.Items.Add(5);
            cbb_frequency.Items.Add(10);
            cbb_frequency.Items.Add(20);
            cbb_frequency.Items.Add(50);
            if (cbb_frequency.Items.Count > 0) cbb_frequency.SelectedIndex = 3;

            rb_frequency.IsChecked = true;

            serialPort.DataReceived += new SerialDataReceivedEventHandler(com_DataReceived);
            command_angle = str2HexByte("7704000408");
            command_accel = str2HexByte("7704005458");
        }

        private void MouseEnterExitArea(object sender, MouseEventArgs e)
        {
            status_bar_text.Text = "退出程序";
        }

        private void MouseLeaveArea(object sender, MouseEventArgs e)
        {
            status_bar_text.Text = "Ready";
        }

        private void Click_File_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MouseEnterAutoLinefeed(object sender, MouseEventArgs e)
        {
            status_bar_text.Text = "自动换行";
        }

        private void Click_Edit_AutoLinefeed(object sender, RoutedEventArgs e)
        {
            if (Auto_Linefeed_Menu.IsChecked)
            {
                //origin_data.TextWrapping = TextWrapping.Wrap;
                //result_data.TextWrapping = TextWrapping.Wrap;
                //Translation_Box.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                //origin_data.TextWrapping = TextWrapping.NoWrap;
                //result_data.TextWrapping = TextWrapping.NoWrap;
                //Translation_Box.TextWrapping = TextWrapping.NoWrap;
            }

        }

        private void MouseEnterAboutArea(object sender, MouseEventArgs e)
        {
            status_bar_text.Text = "关于";
        }

        private void Click_Help_About(object sender, RoutedEventArgs e)
        {
            About ab = new About();
            ab.Owner = this;
            ab.ShowDialog();
        }

        private void Click_rb_f(object sender, RoutedEventArgs e)
        {
            rb_interval.IsChecked = false;
            cbb_frequency.IsEnabled = true;
            tb_interval.IsEnabled = false;
        }

        private void Click_rb_i(object sender, RoutedEventArgs e)
        {
            rb_frequency.IsChecked = false;
            cbb_frequency.IsEnabled = false;
            tb_interval.IsEnabled = true;
        }

        private void tb_interval_keydown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.NumPad0 && e.Key != Key.NumPad1 && e.Key != Key.NumPad2 &&
                e.Key != Key.NumPad3 && e.Key != Key.NumPad4 && e.Key != Key.NumPad5 && e.Key != Key.NumPad6 &&
                e.Key != Key.NumPad7 && e.Key != Key.NumPad8 && e.Key != Key.NumPad9 && e.Key != Key.Back)
            {
                e.Handled = true;
            }
        }

        private void com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                Thread.Sleep(3);
                byte[] array = new byte[serialPort.BytesToRead];
                serialPort.Read(array, 0, array.Length);
                if (mode == 0)
                {
                    Dispatcher.Invoke(new Action<byte[]>(mode_1_Execute), array);
                }
                System.Diagnostics.Debug.WriteLine(BitConverter.ToString(array) + " ---- " + array.Length);
                if (array.Length < 14) return;
                Dispatcher.Invoke(new Action<byte[]>(Add_data), array);
                //Add_data(array);
            }
            catch (Exception ee)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(this, ee.Message, "Error");
                }));
            }
        }

        private void Add_data(byte[] data)
        {

            StringBuilder sb = new StringBuilder();
            switch (cbb_items.SelectedIndex)
            {
                case 0:
                    {
                        for (int i = 0; i < data.Length; ++i)
                        {
                            sb.AppendFormat("{0:X2}", data[i]);
                        }
                        if (sb[0] != '7' || sb[1] != '7') return;
                        leftbox_data.Text += sb.ToString() + "\r\n";
                        string str = ParserAngle(sb.ToString());
                        rightbox_data.Text += str;
                        tb_x_angle.Text = str.Substring(0, 7);
                        tb_y_angle.Text = str.Substring(10, 7);
                        tb_z_angle.Text = str.Substring(20, 7);
                        data_count++;
                        data_size += data.Length;
                        tb_data_count.Text = data_count.ToString();
                        tb_data_size.Text = data_size.ToString();
                        leftbox_data.ScrollToEnd();
                        rightbox_data.ScrollToEnd();
                        sb.Clear();
                        break;
                    }
                case 1:
                    {
                        for (int i = 0; i < data.Length; ++i)
                        {
                            sb.AppendFormat("{0:X2}", data[i]);
                        }
                        if (sb[0] != '7' || sb[1] != '7') return;
                        leftbox_data.Text += sb.ToString() + "\r\n";
                        string str = ParserAcceleration(sb.ToString());
                        rightbox_data.Text += str;
                        tb_x_accel.Text = str.Substring(0, 7);
                        tb_y_accel.Text = str.Substring(10, 7);
                        tb_z_accel.Text = str.Substring(20, 7);
                        data_count++;
                        data_size += data.Length;
                        tb_data_count.Text = data_count.ToString();
                        tb_data_size.Text = data_size.ToString();
                        leftbox_data.ScrollToEnd();
                        rightbox_data.ScrollToEnd();
                        sb.Clear();
                        break;
                    }
                case 2:
                    {
                        for (int i = 0; i < data.Length; ++i)
                        {
                            sb.AppendFormat("{0:X2}", data[i]);
                        }
                        if (sb[0] != '7' || sb[1] != '7') return;

                        if (sb[6] == '8')
                        {
                            string str = ParserAngle(sb.ToString());
                            leftbox_data.Text += str;
                            tb_x_angle.Text = str.Substring(0, 7);
                            tb_y_angle.Text = str.Substring(10, 7);
                            tb_z_angle.Text = str.Substring(20, 7);
                        }
                        else if (sb[6] == '5')
                        {
                            string str = ParserAcceleration(sb.ToString());
                            rightbox_data.Text += str;
                            tb_x_accel.Text = str.Substring(0, 7);
                            tb_y_accel.Text = str.Substring(10, 7);
                            tb_z_accel.Text = str.Substring(20, 7);

                        }
                        data_count++;
                        data_size += data.Length;
                        tb_data_count.Text = data_count.ToString();
                        tb_data_size.Text = data_size.ToString();
                        leftbox_data.ScrollToEnd();
                        rightbox_data.ScrollToEnd();
                        sb.Clear();
                        break;
                    }
            }
        }

        private void mode_1_Execute(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; ++i)
            {
                sb.AppendFormat("{0:X2}", data[i]);
            }
            if(tb_get_command.Text.Replace(" ", "") != "7704000408" && tb_get_command.Text.Replace(" ", "") != "7704005458")
            leftbox_data.Text += sb.ToString() + "\r\n";
            if (tb_get_command.Text.Replace(" ", "") != "7705000C0011" && tb_get_command.Text.Replace(" ", "").Substring(0, 8) == "7705000C" && sb.ToString().Substring(0, 10) == "7705008C00")
            {
                mode = 1;
                cbb_items.SelectedIndex = 0;
                cbb_items.IsEnabled = false;
                btn_begin.IsEnabled = false;
            }
            if (tb_get_command.Text.Replace(" ", "") == "7705000C0011" && sb.ToString().Substring(0, 10) == "7705008C00")
            {
                cbb_items.IsEnabled = true;
                btn_begin.IsEnabled = true;
            }
        }

        private string ParserAngle(string data)
        {
            StringBuilder sb = new StringBuilder();
            if (data[8] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(9, 3));
                sb.Append(".");
                sb.Append(data.Substring(12, 2));
            }
            else if (data[8] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(9, 3));
                sb.Append(".");
                sb.Append(data.Substring(12, 2));
            }
            sb.Append("   ");

            if (data[14] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(15, 3));
                sb.Append(".");
                sb.Append(data.Substring(18, 2));
            }
            else if (data[14] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(15, 3));
                sb.Append(".");
                sb.Append(data.Substring(18, 2));
            }
            sb.Append("   ");

            if (data[20] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(21, 3));
                sb.Append(".");
                sb.Append(data.Substring(24, 2));
            }
            else if (data[20] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(21, 3));
                sb.Append(".");
                sb.Append(data.Substring(24, 2));
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        private string ParserAcceleration(string data)
        {
            StringBuilder sb = new StringBuilder();
            if (data[8] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(9, 1));
                sb.Append(".");
                sb.Append(data.Substring(10, 4));
            }
            else if (data[8] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(9, 1));
                sb.Append(".");
                sb.Append(data.Substring(10, 4));
            }
            sb.Append("   ");

            if (data[14] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(15, 1));
                sb.Append(".");
                sb.Append(data.Substring(16, 4));
            }
            else if (data[14] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(15, 1));
                sb.Append(".");
                sb.Append(data.Substring(16, 4));
            }
            sb.Append("   ");

            if (data[20] == '0')
            {
                sb.Append(" ");
                sb.Append(data.Substring(21, 1));
                sb.Append(".");
                sb.Append(data.Substring(22, 4));
            }
            else if (data[20] == '1')
            {
                sb.Append("-");
                sb.Append(data.Substring(21, 1));
                sb.Append(".");
                sb.Append(data.Substring(22, 4));
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        private void cbb_items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbb_items.SelectedIndex != -1)
            {
                switch (cbb_items.SelectedIndex)
                {
                    case 0:
                        {
                            sp_angle.Visibility = Visibility.Visible;
                            sp_accel.Visibility = Visibility.Collapsed;
                            is_collect_angel = true;
                            is_collect_accel = false;
                            tb_left_text.Text = "原始数据";
                            tb_right_text.Text = "角度数据";
                            break;
                        }
                    case 1:
                        {
                            sp_angle.Visibility = Visibility.Collapsed;
                            sp_accel.Visibility = Visibility.Visible;
                            is_collect_angel = false;
                            is_collect_accel = true;
                            tb_left_text.Text = "原始数据";
                            tb_right_text.Text = "加速度数据";
                            break;
                        }
                    case 2:
                        {
                            sp_angle.Visibility = Visibility.Visible;
                            sp_accel.Visibility = Visibility.Visible;
                            is_collect_angel = true;
                            is_collect_accel = true;
                            tb_left_text.Text = "角度数据";
                            tb_right_text.Text = "加速度数据";
                            break;
                        }
                }
            }
        }


        private void Com_Button_Click(object sender, RoutedEventArgs e)
        {
            if (cbb_ComName.Items.Count == 0)
            {
                MessageBox.Show(this, "没有发现串口！", "Error");
            }
            else
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.PortName = cbb_ComName.SelectedItem.ToString();
                    serialPort.BaudRate = int.Parse(cbb_BaudRate.SelectedItem.ToString());
                    serialPort.RtsEnable = true;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;
                    System.Diagnostics.Debug.WriteLine(cbb_ComName.SelectedItem.ToString() + " " + int.Parse(cbb_BaudRate.SelectedItem.ToString()));
                    try
                    {
                        serialPort.Open();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Error");
                        return;
                    }
                    btn_open.Content = "关闭串口";
                    btn_image.Source = img_end;
                }
                else
                {
                    try
                    {
                        if (is_begin)
                        {
                            timer.Stop();
                            btn_begin.Content = "开始";
                            is_begin = false;
                        }
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();
                        serialPort.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Error");
                    }
                    btn_open.Content = "打开串口";
                    btn_image.Source = img_begin;
                }
                cbb_ComName.IsEnabled = !serialPort.IsOpen;
                cbb_BaudRate.IsEnabled = !serialPort.IsOpen;
            }
        }

        private void SendData(object sender, EventArgs e)
        {
            try
            {
                if (is_collect_angel)
                {
                    serialPort.Write(command_angle, 0, command_angle.Length);
                    //Thread.Sleep(10);
                }
                if (is_collect_accel)
                {
                    serialPort.Write(command_accel, 0, command_accel.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error");
            }
        }

        private void btn_begin_click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    if (!is_begin)
                    {
                        mode = 1;
                        if (rb_frequency.IsChecked == true)
                        {
                            int i = 1000 / int.Parse(cbb_frequency.SelectedItem.ToString());
                            timer.Interval = new TimeSpan(0, 0, 0, 0, i);
                        }
                        if (rb_interval.IsChecked == true)
                        {
                            int i = int.Parse(tb_interval.Text);
                            timer.Interval = new TimeSpan(0, 0, 0, 0, i);
                        }
                        btn_begin.Content = "关闭";
                        is_begin = true;
                        timer.Start();
                    }
                    else
                    {
                        timer.Stop();
                        btn_begin.Content = "开始";
                        is_begin = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error");
                }

            }
            else
            {
                MessageBox.Show(this, "请先打开串口！", "Error");
            }

        }

        private byte[] str2HexByte(string str)
        {
            str = str.Replace(" ", "");
            if (str.Length % 2 != 0)
            {
                str += " ";
            }
            byte[] mybytes = new byte[str.Length / 2];
            for (int i = 0; i < mybytes.Length; ++i)
            {
                mybytes[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return mybytes;
        }

        private void tb_get_command_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.NumPad0 && e.Key != Key.NumPad1 && e.Key != Key.NumPad2 && e.Key != Key.Space &&
                e.Key != Key.NumPad3 && e.Key != Key.NumPad4 && e.Key != Key.NumPad5 && e.Key != Key.NumPad6 &&
                e.Key != Key.NumPad7 && e.Key != Key.NumPad8 && e.Key != Key.NumPad9 && e.Key != Key.Back && 
                e.Key != Key.A && e.Key != Key.B && e.Key != Key.C && e.Key != Key.D && e.Key != Key.E && e.Key != Key.F &&
                e.Key != Key.G && e.Key != Key.Left && e.Key != Key.Right)
            {
                e.Handled = true;
            }
        }

        private void tb_clear_click(object sender, RoutedEventArgs e)
        {
            tb_data_count.Text = "0";
            data_count = 0;
            tb_data_size.Text = "0";
            data_size = 0;
            leftbox_data.Text = "";
            rightbox_data.Text = "";
        }

        private void btn_send_click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    mode = 0;
                    byte[] command = str2HexByte(tb_get_command.Text.Replace(" ", ""));
                    serialPort.Write(command, 0, command.Length);
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                MessageBox.Show(this, "请先打开串口！", "Error");
            }
        }

        private void tb_export_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string time = DateTime.Now.TimeOfDay.ToString();
                time = time.Split('.')[0];
                time = time.Replace(":", "_");
                string desktoppath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                switch (cbb_items.SelectedIndex)
                {
                    case 0:
                        {
                            if (rightbox_data.Text != "")
                            {
                                string path = System.IO.Path.Combine(desktoppath, time + "_Angle.txt");
                                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                                {
                                    sw.Write(rightbox_data.Text);
                                }
                                MessageBox.Show(this, "成功导出文件至桌面", "导出成功");
                            }
                            else
                            {
                                MessageBox.Show(this, "角度信息未导出，内容为空", "Error");
                            }
                            break;
                        }
                    case 1:
                        {
                            if (rightbox_data.Text != "")
                            {
                                string path = System.IO.Path.Combine(desktoppath, time + "_Acceleration.txt");
                                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                                {
                                    sw.Write(rightbox_data.Text);
                                }
                                MessageBox.Show(this, "成功导出文件至桌面", "导出成功");
                            }
                            else
                            {
                                MessageBox.Show(this, "加速度信息未导出，内容为空", "Error");
                            }
                            break;
                        }
                    case 2:
                        {
                            if (leftbox_data.Text != "")
                            {
                                string path = System.IO.Path.Combine(desktoppath, time + "_Angle.txt");
                                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                                {
                                    sw.Write(leftbox_data.Text);
                                }
                                MessageBox.Show(this, "成功导出文件至桌面", "导出成功");
                            }
                            else
                            {
                                MessageBox.Show(this, "角度信息未导出，内容为空", "Error");
                            }
                            if (rightbox_data.Text != "")
                            {
                                string path = System.IO.Path.Combine(desktoppath, time + "_Acceleration.txt");
                                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                                {
                                    sw.Write(rightbox_data.Text);
                                }
                                MessageBox.Show(this, "成功导出文件至桌面", "导出成功");
                            }
                            else
                            {
                                MessageBox.Show(this, "加速度信息未导出，内容为空", "Error");
                            }
                            break;
                        }
                }
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message, "错误");
            }
        }
    }
}
