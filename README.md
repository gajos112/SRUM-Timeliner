# SRUM-Timeliner

SRUM Timeliner was designed and created for all DFIR analysts who use SRUM database to discover potential data exfiltration and prove connections with C2 server. What I have observed during last few years is that analysts indeed use that source of information to better understand how much data was sent out and recevied by either malicious executable or LOLbins. It's quite simple to recognize if the malicious executable send something out, because all entires found in SRUM for the malicious EXE are suspicious, but what about LOLbins? How will you discover if the powershell.exe or wscript.exe was used to transfer data in the coorporate envirmoent where multiple scripts are active all the time and transfer data back and forth to cooporate servers? The answear can be pretty easy.... Let's look for a spike! It's a very good idea, but first we have to how some ilustrate a baseline for a suspected process. And this is a moment why my tool comes on the table. 

SRUM Timeliner does two things:
- builds a TIMELINE following the TLN format (https://forensicswiki.xyz/wiki/index.php?title=TLN, http://windowsir.blogspot.com/2009/02/timeline-analysis-pt-iii.html),
- builds a chart ilustrating a baseline for a sellected process.

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/1.png?raw=true)

It is a GUI tool written in C# .Net Framework 4.7.2. In order to access ESE database (the format used by SRUM) I decided to use ManagedEsent version 2.0.3 (older versions do not work properly). Link to it can be found here: https://github.com/microsoft/ManagedEsent. Going further, to make that executable portable I used Costura.Fody which merges assemblies as embedded resources, therfore you do not have to care about other dependencies. 

It was tested on:

- Windows 10.0.16299,
- Windows 10.0.17763,
- Windows 10.0.19042,

The tool accesses and parsses data from the table called **{973F5D5C-1D90-4944-BE8E-24B94231A174}**. More information about the SRUM strucutre can be found under these links:
- https://deepsec.net/docs/Slides/2019/Beyond_Windows_Forensics_with_Built-in_Microsoft_Tooling_Thomas_Fischer.pdf,
- https://velociraptor.velocidex.com/digging-into-the-system-resource-usage-monitor-srum-afbadb1a375.

To give you a quick overview of the database, I  listed few useful tables (fro DFIR analysts) below. 
- {DD6636C4-8929-4683-974E-22C046A43763} - Network Connectivity data
- {D10CA2FE-6FCF-4F6D-848E-B2E99266FA89} - Application Resource usage data
- {973F5D5C-1D90-4944-BE8E-24B94231A174} - Network usage data 
- {D10CA2FE-6FCF-4F6D-848E-B2E99266FA86} - Windows Push Notification data
- {FEE4E14F-02A9-4550-B5CE-5FA2DA202E37} - Energy usage data

# How does it work?
First you have to provide a path to a SRUM db that you want to parse. Then you also have to provide the path where you want to save the output (timeline in TLN format) and click "PARSE". 

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/2.png?raw=true)
![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/3.png?raw=true)

If the file ou provided is not a valid SRUM db, the tool will throw an error. If everything is okay it will try to:

1. Attach the database:

        wrn = Api.JetAttachDatabase(sesid, pathDB, AttachDatabaseGrbit.None);
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Attaching the database " + pathDB + "\r\n");
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

2. Open the database:

        wrn = Api.OpenDatabase(sesid, pathDB, out dbid, OpenDatabaseGrbit.None);
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Opening the database: " + pathDB + "\r\n");
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

3. Open the table:

        wrn = Api.OpenTable(sesid, dbid, nameTABLE, OpenTableGrbit.None, out tableid);
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Opening table: " + nameTABLE + "\r\n");
        LogTextBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Status: " + wrn + "\r\n");

4. Get information about the columns:

        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnAppId, out columndefAppId);
        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnTime, out columndefTime);
        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnUserID, out columndefUserID);
        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnBytesSent, out columndefBytesSent);
        Api.JetGetColumnInfo(sesid, dbid, nameTABLE, ColumnBytesRecvd, out columndefBytesRecvd);

5. Going further the tool loops through all rows in the table and get the value from each column:

        int AppId = (int)Api.RetrieveColumnAsInt32(sesid, tableid, columndefAppId.columnid);
        DateTime Time = (DateTime)Api.RetrieveColumnAsDateTime(sesid, tableid, columndefTime.columnid);
        Int64 BytesSent = (Int64)Api.RetrieveColumnAsInt64(sesid, tableid, columndefBytesSent.columnid);
        Int64 BytesRecvd = (Int64)Api.RetrieveColumnAsInt64(sesid, tableid, columndefBytesRecvd.columnid);
        string SRUM_ProcessName = GetName(instance, sesid, dbid, AppId);

As you could observe above, each value is stored using a different data type. It's quite important as you have to know which method you will choose to extract that data. I found one article that shows data types for all tables: 
- http://dfir.pro/index.php?link_id=92259,

What is more in the table you can not find the name of executables, only the application ID. Then there is another table called "SruDbIdMapTable", which stores the name for each ID. The name is an UTF-16 encoded string. Therefore I created a method that retrieves the name of the executable based on the Application ID. 

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

6. Create a TIMELINE (the two lines of the code below are out of context, but just wanted to show you the way it saves each row to the file):

        stringbuilder.Append(SRUM_Time + ",SRUM,,,[Network Connection] SRUM - Executable: " + SRUM_ProcessName + " -> Bytes Sent: " + BytesSent + " -> Bytes received: " + BytesRecvd + "\r\n");
        File.AppendAllText(CSVPath, stringbuilder.ToString());

7. Create a LIST of sent and received bytes for each process. 

Once you have the list of all process found in the database, you can choose the one you want to investigate and draw charts by pressing "Shows the baseline". 

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/4.PNG?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/5.PNG?raw=true)

You can find 4 charts:
- Bytes sent,
- Bytes sent (sum per day),
- Bytes received,
- Bytes received (sum per day).

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/9.PNG?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/10.PNG?raw=true)

You can also zoom in and zoom out the charts if you want to.

# Timeline

The timeline contains all entires extracted from the database. You can easily review them using BASH and simply GREP what you want analyze. 

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/12.png?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/13.png?raw=true)





![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/6.PNG?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/7.PNG?raw=true)
![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/8.PNG?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/11.PNG?raw=true)

![alt text](https://github.com/gajos112/SRUM-Timeliner/blob/main/Images/14.PNG?raw=true)
