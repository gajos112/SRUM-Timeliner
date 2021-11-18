using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Isam.Esent.Interop;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;

namespace SRUM_GUI
{
    public partial class Form1 : Form
    {

        string pathToSrumdb;
        string pathToTimeline;

        FolderBrowserDialog openFolderBrowserDialog = new FolderBrowserDialog();
        OpenFileDialog openFileDialogPathToSrumdb = new OpenFileDialog();

        List<Tuple<string, DateTime, int, int>> Processes = new List<Tuple<string, DateTime, int, int>>();

        JET_INSTANCE instance;
        JET_SESID sesid;
        JET_DBID dbid;
        JET_TABLEID tableid;

        JET_wrn wrn;

        JET_COLUMNDEF columndefAppId = new JET_COLUMNDEF();
        JET_COLUMNDEF columndefTime = new JET_COLUMNDEF();
        JET_COLUMNDEF columndefUserID = new JET_COLUMNDEF();
        JET_COLUMNDEF columndefBytesSent = new JET_COLUMNDEF();
        JET_COLUMNDEF columndefBytesRecvd = new JET_COLUMNDEF();

        JET_COLUMNID columnid;

        string nameTABLE = "{973F5D5C-1D90-4944-BE8E-24B94231A174}";
        string ColumnAppId = "AppId";
        string ColumnTime = "TimeStamp";
        string ColumnUserID = "UserId";
        string ColumnBytesSent = "BytesSent";
        string ColumnBytesRecvd = "BytesRecvd";

        static string GetName(JET_INSTANCE instance, JET_SESID sesid, JET_DBID dbid, int AppId)
        {
            string AppName = "";

            string nameTABLE = "SruDbIdMapTable";
            string ColumnIdType = "IdType";
            string columnIdBlob = "IdBlob";
            string ColumIdIndex = "IdIndex";

            JET_COLUMNDEF columndefIdType = new JET_COLUMNDEF();
            JET_COLUMNDEF columndefIdBlob = new JET_COLUMNDEF();
            JET_COLUMNDEF columndefIdIndex = new JET_COLUMNDEF();

            JET_TABLEID tableid;

            Api.OpenTable(sesid, dbid, nameTABLE, OpenTableGrbit.None, out tableid);

            Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnIdType, out columndefIdType);
            Api.JetGetColumnInfo(sesid, dbid, nameTABLE, columnIdBlob, out columndefIdBlob);
            Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumIdIndex, out columndefIdIndex);

            Api.JetMove(sesid, tableid, AppId - 1, MoveGrbit.None);

            int IdIndex = (int)Api.RetrieveColumnAsInt32(sesid, tableid, columndefIdIndex.columnid);
            AppName = (string)Api.RetrieveColumnAsString(sesid, tableid, columndefIdBlob.columnid);
            Api.JetCloseTable(sesid, tableid);

            return AppName;
        }
        public void GetNamesOfAllExecutables(List<Tuple<string, DateTime, int, int>> Processes)
        {
            string tmp = "";
            Processes.Sort();

            foreach (var Process in Processes)
            {
                try
                {
                    string Name = Process.Item1;
                    if (!string.IsNullOrEmpty(Name))
                    {

                        if (!Name.ToLower().Equals(tmp.ToLower()))
                        {
                            tmp = Name;
                            ProcessesDataGridView.Rows.Add(Name);
                        }
                    }
                }
                catch { }
            }
        }
        public void CountPerDaySentBytes(List<Tuple<string, DateTime, int, int>> Processes, string ExecutableName)
        {
            chart3.Series["Bytes sent (sum per day)"].Points.Clear();

            int i = 0;
            Processes = Processes.OrderBy(t => t.Item2).ToList();
            Regex regexDay = new Regex(@"(?<day>\d{4}-\d{1,2}-\d{1,2}) ");
            string Day = "";
            string tmpDay = "";
            int SentSum = 0;
            foreach (var Process in Processes)
            {
                try
                {
                    string Name = Process.Item1;
                    if (!string.IsNullOrEmpty(Name))
                    {

                        if (Name.ToLower().Equals(ExecutableName.ToLower()))
                        {
                            string DayTime = Process.Item2.ToString("yyyy-MM-dd HH:mm:ss");
                            Match matchDays = regexDay.Match(DayTime);

                            if (matchDays.Groups["day"].Value.Length > 0)
                            {
                                Day = matchDays.Groups["day"].Value;

                                if (i == 0)
                                {
                                    i++; 
                                    tmpDay = Day;
                                }

                                if (Day.ToLower().Equals(tmpDay))
                                {
                                    SentSum += Process.Item3;
                                }
                                else
                                {
                                    ChartDataTextBox.AppendText("\r\nDay: " + tmpDay + ", sum of sent bytes: " + SentSum);
                                    
                                    chart3.Series["Bytes sent (sum per day)"].Points.AddXY(tmpDay, SentSum);
                                    tmpDay = Day;
                                    SentSum = Process.Item3;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            ChartDataTextBox.AppendText("\r\nDay: " + tmpDay + ", sum of sent bytes: " + SentSum);
            chart3.Series["Bytes sent (sum per day)"].Points.AddXY(tmpDay, SentSum);
        }
        public void CountPerDayReceivedBytes(List<Tuple<string, DateTime, int, int>> Processes, string ExecutableName)
        {
            chart4.Series["Bytes received (sum per day)"].Points.Clear();

            int i = 0;
            Processes = Processes.OrderBy(t => t.Item2).ToList();
            Regex regexDay = new Regex(@"(?<day>\d{4}-\d{1,2}-\d{1,2}) ");
            string Day = "";
            string tmpDay = "";
            int ReceivedSum = 0;
            foreach (var Process in Processes)
            {
                try
                {
                    string Name = Process.Item1;
                    if (!string.IsNullOrEmpty(Name))
                    {

                        if (Name.ToLower().Equals(ExecutableName.ToLower()))
                        {
                            string DayTime = Process.Item2.ToString("yyyy-MM-dd HH:mm:ss");
                            Match matchDays = regexDay.Match(DayTime);

                            if (matchDays.Groups["day"].Value.Length > 0)
                            {
                                Day = matchDays.Groups["day"].Value;

                                if (i == 0)
                                {
                                    i++;
                                    tmpDay = Day;
                                }

                                if (Day.ToLower().Equals(tmpDay))
                                {
                                    ReceivedSum += Process.Item4;
                                }
                                else
                                {
                                    ChartDataTextBox2.AppendText("\r\nDay: " + tmpDay + ", sum of received bytes: " + ReceivedSum);

                                    chart4.Series["Bytes received (sum per day)"].Points.AddXY(tmpDay, ReceivedSum);
                                    tmpDay = Day;
                                    ReceivedSum = Process.Item4;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            ChartDataTextBox2.AppendText("\r\nDay: " + tmpDay + ", sum of received bytes: " + ReceivedSum);
            chart4.Series["Bytes received (sum per day)"].Points.AddXY(tmpDay, ReceivedSum);
        }
        public void PrintChart(List<Tuple<string, DateTime, int, int>> Processes, string ExecutableName)
        {
            ChartDataTextBox.AppendText("\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Bulding the chart for " + ExecutableName + "\r\n");
            ChartDataTextBox2.AppendText("\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Bulding the chart for " + ExecutableName + "\r\n");

            chart1.Series["Bytes Sent"].Points.Clear();
            chart2.Series["Bytes received"].Points.Clear();

            int i = 0;

            Processes = Processes.OrderBy(t => t.Item2).ToList();
            ChartDataTextBox.AppendText("\r\n");
            ChartDataTextBox2.AppendText("\r\n");

            foreach (var Process in Processes)
            {
                try
                {
                    string Name = Process.Item1;
                    if (!string.IsNullOrEmpty(Name))
                    {
                        if (Name.ToLower().Equals(ExecutableName.ToLower()))
                        {
                            i++;

                            chart1.Series["Bytes Sent"].Points.AddXY(Process.Item2.ToString("yyyy-MM-dd HH:mm:ss"), (int)Process.Item3);
                            ChartDataTextBox.AppendText("\r\n" + Process.Item2.ToString("yyyy-MM-dd HH:mm:ss") + " sent "+ (int)Process.Item3 + " bytes");

                            chart2.Series["Bytes received"].Points.AddXY(Process.Item2.ToString("yyyy-MM-dd HH:mm:ss"), (int)Process.Item4);
                            ChartDataTextBox2.AppendText("\r\n" + Process.Item2.ToString("yyyy-MM-dd HH:mm:ss") + " received " + (int)Process.Item4 + " bytes");
                        }
                    }
                }
                catch { }
            }

            ChartDataTextBox.AppendText("\r\n" + "Found " + i + " entries.");
            ChartDataTextBox2.AppendText("\r\n" + "Found " + i + " entries.");

            chart1.ChartAreas["ChartArea1"].AxisX.Interval = 2;
            chart2.ChartAreas["ChartArea1"].AxisX.Interval = 2;

            ChartDataTextBox.AppendText("\r\n");
            ChartDataTextBox2.AppendText("\r\n");

        }

        public Form1()
        {
            InitializeComponent();
            chart1.MouseWheel += chart1_MouseWheel;
            chart2.MouseWheel += chart1_MouseWheel;
            chart3.MouseWheel += chart1_MouseWheel;
            chart4.MouseWheel += chart1_MouseWheel;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void buttonParse_Click(object sender, EventArgs e)
        {
            try {
                if (!String.IsNullOrEmpty(pathToSrumdb) && !String.IsNullOrEmpty(pathToTimeline))
                {
                    string CSVPath = pathToTimeline + @"\Timeline_SRUM.csv";

                    if (File.Exists(CSVPath))
                    {
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Removing the old TIMELINE...\r\n");
                        File.Delete(CSVPath);
                    }

                    string pathDB = pathToSrumdb;

                    if (instance.IsInvalid == true)
                    {
                        Api.JetCreateInstance(out instance, "instance");
                        Api.JetInit(ref instance);

                    }

                    Api.JetBeginSession(instance, out sesid, null, null);
                    try
                    {
                        wrn = Api.JetAttachDatabase(sesid, pathDB, AttachDatabaseGrbit.None);

                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Attaching the database " + pathDB + "\r\n");
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

                        wrn = Api.OpenDatabase(sesid, pathDB, out dbid, OpenDatabaseGrbit.None);
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Opening the database: " + pathDB + "\r\n");
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

                        wrn = Api.OpenTable(sesid, dbid, nameTABLE, OpenTableGrbit.None, out tableid);
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Opening table: " + nameTABLE + "\r\n");
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

                        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnAppId, out columndefAppId);
                        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnTime, out columndefTime);
                        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnUserID, out columndefUserID);
                        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnBytesSent, out columndefBytesSent);
                        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnBytesRecvd, out columndefBytesRecvd);

                        int i = 0;

                        StringBuilder stringbuilder = new StringBuilder();
                        string row = "";

                        do
                        {
                            int AppId = (int)Api.RetrieveColumnAsInt32(sesid, tableid, columndefAppId.columnid);
                            DateTime Time = (DateTime)Api.RetrieveColumnAsDateTime(sesid, tableid, columndefTime.columnid);
                            Int64 BytesSent = (Int64)Api.RetrieveColumnAsInt64(sesid, tableid, columndefBytesSent.columnid);
                            Int64 BytesRecvd = (Int64)Api.RetrieveColumnAsInt64(sesid, tableid, columndefBytesRecvd.columnid);

                            string SRUM_ProcessName = GetName(instance, sesid, dbid, AppId);
                            string SRUM_Time = Time.ToString("yyyy-MM-dd HH:mm:ss");

                            stringbuilder.Append(SRUM_Time + ",SRUM,,,[Network Connection] SRUM - Executable: " + SRUM_ProcessName + " -> Bytes Sent: " + BytesSent + " -> Bytes received: " + BytesRecvd + "\r\n");
                            Processes.Add(Tuple.Create(SRUM_ProcessName, Time, (int)BytesSent, (int)BytesRecvd));

                            i++;
                            if (i % 500 == 0)
                            {
                                LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Analyzed " + i + " entries.\r\n");
                            }
                        } while (Api.TryMoveNext(sesid, tableid));

                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Found " + i + " entries.\r\n");
                        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Saving the TIMELINE to " + CSVPath + "\r\n");

                        stringbuilder.Replace("\0", "");
                        File.AppendAllText(CSVPath, stringbuilder.ToString());

                        if (File.Exists(CSVPath))
                        {
                            LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Successfully created the TIMELINE\r\n");
                        }
                        else
                        {
                            LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " The TIMELINE was not created\r\n");
                        }

                        Api.JetCloseTable(sesid, tableid);
                        Api.JetEndSession(sesid, EndSessionGrbit.None);
                        Api.JetTerm(instance);


                        GetNamesOfAllExecutables(Processes);
                        button2.Enabled = true;
                        SearchForTextBox.Enabled = true;
                        buttonParse.Enabled = false;
                    }
                    catch (Microsoft.Isam.Esent.Interop.EsentDatabaseDirtyShutdownException)
                    {
                        MessageBox.Show("Could not open the DB, it was not shutdown cleanly! \r\n\r\nRun these commands:\r\n\t'esentutl.exe /r sru /i'\r\n\t'esentutl.exe /p SRUDB.dat'", "DB was not shutdown cleanly", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please provide path to SRUM database and to the TIMELINE!");

                    Api.JetCloseTable(sesid, tableid);
                    Api.JetEndSession(sesid, EndSessionGrbit.None);
                    Api.JetTerm(instance);
                }
            }
            catch(Exception ex) 
            {
                MessageBox.Show("Something went wrong! Are you sure you are trying to open the SRUM database?\r\nFile you are trying to open: " + pathToSrumdb + "\r\n\r\nRrror: " + ex.ToString(),"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialogPathToSrumdb.InitialDirectory = @"C:\";
            openFileDialogPathToSrumdb.Filter = "dat files (*.dat)|*.dat|All files (*.*)|*.*";

            if (openFileDialogPathToSrumdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pathToSrumTextBox.Text = openFileDialogPathToSrumdb.FileName;
                pathToSrumdb = pathToSrumTextBox.Text;
            }            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pathToTimelineTextBox.Text = openFolderBrowserDialog.SelectedPath;
                pathToTimeline = pathToTimelineTextBox.Text;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "It is the fully free software created by Krzysztof Gajewski.\r\n\r\nIcons made by Pixel perfect from www.flaticon.com";
            string title = "SRUM - Timeliner";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ProcessesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(ProcessesDataGridView.SelectedCells[0].Value as String))
                {
                    label4.Text = ProcessesDataGridView.SelectedCells[0].Value.ToString();
                }
            }
            catch { }
        }

        private void ProcessesDataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Up))
            {
                int tmp = ProcessesDataGridView.CurrentCell.RowIndex;
                if ((tmp - 1) >= 0)
                {
                    label4.Text = ProcessesDataGridView.Rows[tmp - 1].Cells[0].Value.ToString();
                }
            }

            if (e.KeyData == (Keys.Down))
            {
                int tmp = ProcessesDataGridView.CurrentCell.RowIndex;
                if ((tmp + 1) < ProcessesDataGridView.Rows.Count)
                {
                    label4.Text = ProcessesDataGridView.Rows[tmp + 1].Cells[0].Value.ToString();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string ExecutableName = "";
            ExecutableName = ProcessesDataGridView.SelectedCells[0].Value.ToString();
            PrintChart(Processes, ExecutableName);
            CountPerDaySentBytes(Processes, ExecutableName);
            CountPerDayReceivedBytes(Processes, ExecutableName);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            {
                int i = 0;
                string lookFor = SearchForTextBox.Text;

                if (lookFor.Length == 0)
                {
                    i = 0;
                    foreach (var row in ProcessesDataGridView.Rows)
                    {
                        ProcessesDataGridView.Rows[i].Visible = true;
                        i++;
                    }
                }

                else
                {
                    foreach (var row in ProcessesDataGridView.Rows)
                    {
                        if (!String.IsNullOrEmpty(ProcessesDataGridView.Rows[i].Cells[0].Value as String))
                        {
                            string tmpSearchFor = (ProcessesDataGridView.Rows[i].Cells[0].Value).ToString();

                            if (!tmpSearchFor.ToLower().Contains(lookFor.ToLower()))
                            {
                                ProcessesDataGridView.Rows[i].Visible = false;
                            }
                            else
                            {
                                ProcessesDataGridView.Rows[i].Visible = true;
                            }
                            i++;
                        }
                    }
                }
            }
        }
        private void SearchForTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
        private void tableLayoutPanel4_Paint(object sender, PaintEventArgs e)
        {

        }
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void chart1_MouseWheel(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;

            try
            {
                if (e.Delta < 0) // Scrolled down.
                {
                    xAxis.ScaleView.ZoomReset();
                    yAxis.ScaleView.ZoomReset();
                }
                else if (e.Delta > 0) // Scrolled up.
                {
                    var xMin = xAxis.ScaleView.ViewMinimum;
                    var xMax = xAxis.ScaleView.ViewMaximum;
                    var yMin = yAxis.ScaleView.ViewMinimum;
                    var yMax = yAxis.ScaleView.ViewMaximum;

                    var posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                    var posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;
                    var posYStart = yAxis.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / 4;
                    var posYFinish = yAxis.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / 4;

                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                }
            }
            catch { }
        }
     
        private void chart2_Click(object sender, EventArgs e)
        {

        }
        private void checkBox1_CheckStateChanged(object sender, EventArgs e)
        {
        }
        private void checkBoxInterval1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInterval1.Checked == true)
            {
                checkBoxInterval2.Checked = false;
                checkBoxInterval5.Checked = false;
                checkBoxInterval10.Checked = false;

                chart1.ChartAreas["ChartArea1"].AxisX.Interval = 1;
                chart2.ChartAreas["ChartArea1"].AxisX.Interval = 1;
                chart3.ChartAreas["ChartArea1"].AxisX.Interval = 1;
                chart4.ChartAreas["ChartArea1"].AxisX.Interval = 1;

            }
        }
        private void checkBoxInterval2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInterval2.Checked == true)
            {
                checkBoxInterval1.Checked = false;
                checkBoxInterval5.Checked = false;
                checkBoxInterval10.Checked = false;

                chart1.ChartAreas["ChartArea1"].AxisX.Interval = 2;
                chart2.ChartAreas["ChartArea1"].AxisX.Interval = 2;
                chart3.ChartAreas["ChartArea1"].AxisX.Interval = 2;
                chart4.ChartAreas["ChartArea1"].AxisX.Interval = 2;
            }
        }
        private void checkBoxInterval5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInterval5.Checked == true)
            {
                checkBoxInterval1.Checked = false;
                checkBoxInterval2.Checked = false;
                checkBoxInterval10.Checked = false;

                chart1.ChartAreas["ChartArea1"].AxisX.Interval = 5;
                chart2.ChartAreas["ChartArea1"].AxisX.Interval = 5;
                chart3.ChartAreas["ChartArea1"].AxisX.Interval = 5;
                chart4.ChartAreas["ChartArea1"].AxisX.Interval = 5;
            }
        }
        private void checkBoxInterval10_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInterval10.Checked == true)
            {
                checkBoxInterval1.Checked = false;
                checkBoxInterval2.Checked = false;
                checkBoxInterval5.Checked = false;

                chart1.ChartAreas["ChartArea1"].AxisX.Interval = 10;
                chart2.ChartAreas["ChartArea1"].AxisX.Interval = 10;
                chart3.ChartAreas["ChartArea1"].AxisX.Interval = 10;
                chart4.ChartAreas["ChartArea1"].AxisX.Interval = 10;
            }
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
