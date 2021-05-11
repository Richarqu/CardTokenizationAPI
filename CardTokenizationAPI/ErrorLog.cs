using System;
using System.Configuration;
using System.IO;

namespace CardTokenizationAPI
{
    public class ErrorLog
    {
        private StreamWriter _log;
        private string _fileName;

        public ErrorLog(Exception error)
        {
            String date = DateTime.Now.ToShortDateString();
            date = date.Replace('/', '_');
            FileName = ConfigurationManager.AppSettings["ErrorLogPath"] + date + ".txt";
            //FileName = "E:\\App\\acadepjob2\\MfbReports\\Log\\" + date + ".txt";

            Log = !File.Exists(FileName) ? new StreamWriter(FileName) : File.AppendText(FileName);
            Log.WriteLine(DateTime.Now);
            Log.WriteLine(error.ToString());
            Log.WriteLine();
            Log.Close();
            //Console.WriteLine(error);
        }

        public ErrorLog(string error)
        {
            String date = DateTime.Now.ToShortDateString();
            date = date.Replace('/', '_');
            FileName = ConfigurationManager.AppSettings["ErrorLogPath"] + date + ".txt";
            //FileName = "E:\\App\\acadepjob2\\MfbReports\\Log\\" + date + ".txt";

            Log = !File.Exists(FileName) ? new StreamWriter(FileName) : File.AppendText(FileName);
            Log.WriteLine(DateTime.Now);
            Log.WriteLine(error.ToString());
            Log.WriteLine();
            Log.Close();
            //Console.WriteLine(error);
        }

        private string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        private StreamWriter Log
        {
            get { return _log; }
            set { _log = value; }
        }
    }
}
