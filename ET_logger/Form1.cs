using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace ET_logger
{
	public partial class Form1 : Form
	{
		//ポート関係の宣言
		SerialPort myPort;
		static string PortName;
		int BaudRate;
		Parity Parity;
		int DataBits;
		StopBits StopBits;

		//表示する値のコマンド
		int cmd;

		//読み込むbufferr
		string buf = "";
		string buf2 = "";
		int find = 0;

		//csv用のファイル
		System.IO.StreamWriter fs;

		//各変数の宣言
		int time = 0, light = 0, gyro = 0, motor_R = 0, motor_L = 0;

		//Thread
		System.Threading.Thread Datalogging_Thread;
		delegate void MyDelegate(string target);

		public Form1()
		{
			InitializeComponent();

			//ポートの設定
			BaudRate = 38400;
			Parity = Parity.None;
			DataBits = 8;
			StopBits = StopBits.One;

			//接続可能なCOMポートを検出、コンボボックスに入れる
			string[] portNames = SerialPort.GetPortNames();

			// 取得したシリアル・ポート名をコンボボックスにセット
			comboBox2.Items.Clear();
			foreach (string port in portNames)
			{
				this.comboBox2.Items.Add(port);
			}

			// コンボボックスのデフォルト選択をセット
			this.comboBox2.SelectedIndex = 0;
			this.comboBox1.SelectedIndex = 0;

			//ファイルを作成
			string FileName = System.DateTime.Now.ToString();	//時間を取得
			FileName = FileName.Replace('/', '_');				// 2013/07/13を2013_07_13という形に整形
			FileName = FileName.Replace(":", "_");				//18:23:30を18_23_30という形に整形
			FileName = FileName.Replace(" ", "_");				//␣を＿に置き換え
			FileName += ".csv";
			//MyDocumentsにJ2_logがあったらそこに保存
			if (System.IO.Directory.Exists(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\J2_log"))
			{
				fs = new System.IO.StreamWriter(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\J2_log\\"+FileName);
			}
			else
			{
				fs = new System.IO.StreamWriter(FileName);
			}
			MessageBox.Show(FileName, "ファイル作成完了");
		}

		private void button3_Click(object sender, EventArgs e)
		{
			//コンボボックスからCOMポート名を取得
			//取得したCOMポート名でポートを開く
			PortName = comboBox2.Text;
			if (myPort == null && PortName.Length >= 4 && PortName.Length != 0)
			{
				//ポートがnullでポート名が大丈夫そうならポートを開く
				myPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
				try
				{
					myPort.Open();
				}
				catch
				{
					MessageBox.Show("error", "error");
				}
			}
			if (myPort != null && myPort.IsOpen)
			{
				//接続に成功したらメッセージを出す
				MessageBox.Show("接続成功");
				//接続に成功したらコンボボックスをロックする
				comboBox2.Enabled = false;
			}
			else
			{
				MessageBox.Show("error", "error");
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			//'R'を送信する
			if (myPort != null && myPort.IsOpen)
			{
				myPort.Write("R");
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			//'S'を送信する
			if (myPort != null && myPort.IsOpen)
			{
				myPort.Write("S");
				myPort.Close();
			}
		}
		private void button4_Click(object sender, EventArgs e)
		{
			//値(id)を取得
			switch (comboBox1.SelectedIndex)
			{
				case 0: cmd = 0;
					break;
				case 1: cmd = 1;
					break;
				case 2: cmd = 2;
					break;
				case 3: cmd = 3;
					break;
				case 4: cmd = 4;
					break;
				default: MessageBox.Show("error", "error");
					break;
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			//フォームが閉じるときにポートも閉じる
			if (myPort != null && myPort.IsOpen)
			{
				myPort.Close();
			}

		}

		private void button5_Click(object sender, EventArgs e)
		{
			button5.Enabled = false;
			this.Datalogging_Thread =
			new Thread(new ThreadStart(this.datalogging));
			this.Datalogging_Thread.Start();
			button5.Enabled = true;

		}

		public void datalogging()
		{
			if (myPort != null && myPort.IsOpen)
			{
				//無限ループ
				while (true)
				{
					if (myPort.IsOpen == false)
					{
						break;
					}
					//bufferの取得
					try
					{
						buf = myPort.ReadLine();
					}
					catch
					{
						MessageBox.Show("error", "error");
					}
					//NULL文字を消す
					buf = buf.TrimStart('\0');

					//誤作動予防
					if (buf.Length > 30 || buf.Length < 5)
					{
						continue;
					}
					if (buf.Length > 30 || buf.Length < 5)
					{
						continue;
					}

					//CSVへの書き込み
					fs.WriteLine(buf);

					//各変数の取得

					//timeの値取得
					find = buf.IndexOf(',');
					if (find <= 0)
					{
						continue;
					}
					buf2 = buf.Substring(0, find);
					if (int.TryParse(buf2, out time) == false)
					{
						continue;
					}
					if (cmd == 0)
					{
						label1.Invoke(new MyDelegate(SetText), new object[] { Convert.ToString(time) });
					}

					//lightの値取得
					buf = buf.Remove(0, find + 1);
					find = buf.IndexOf(',');
					if (find <= 0)
					{
						continue;
					}
					buf2 = buf.Substring(0, find);
					if (int.TryParse(buf2, out light) == false)
					{
						continue;
					}
					if (cmd == 1)
					{
						label1.Invoke(new MyDelegate(SetText), new object[] { Convert.ToString(light) });
					}

					//gyroの値取得
					buf = buf.Remove(0, find + 1);
					find = buf.IndexOf(',');
					if (find <= 0)
					{
						continue;
					}
					buf2 = buf.Substring(0, find);
					if (int.TryParse(buf2, out gyro) == false)
					{
						continue;
					}
					gyro = Convert.ToInt32(buf2);
					if (cmd == 2)
					{
						label1.Invoke(new MyDelegate(SetText), new object[] { Convert.ToString(gyro) });
					}

					//moter_Rの値取得
					buf = buf.Remove(0, find + 1);
					find = buf.IndexOf(',');
					if (find <= 0)
					{
						continue;
					}
					buf2 = buf.Substring(0, find);
					if (int.TryParse(buf2, out motor_R) == false)
					{
						continue;
					}
					if (cmd == 3)
					{
						label1.Invoke(new MyDelegate(SetText), new object[] { Convert.ToString(motor_R) });
					}
					//moter_Lの値取得
					buf = buf.Remove(0, find + 1);
					buf2 = buf;
					if (int.TryParse(buf2, out motor_L) == false)
					{
						continue;
					}
					if (cmd == 4)
					{
						label1.Invoke(new MyDelegate(SetText), new object[] { Convert.ToString(motor_L) });
					}
				}
			}
			else
			{
				MessageBox.Show("error", "error");
			}
		}
		void SetText(string target)
		{
			label1.Text = target;
		}
	}

}
