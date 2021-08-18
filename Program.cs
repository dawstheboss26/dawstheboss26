using System;
using System.IO;
using System.Net;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace RWIS_Project
{
    class Program
    {


        static void Main()
        {
            deleteOld(ConfigurationManager.AppSettings.Get("deleteWave"));

            // Gets a list of sites from an SQL table
            List<string> sites = siteList();

            //Passes the list to a function that checks for the files and downloads them
            ftpcheck(sites);

           
                   
            return;
        }


        /* siteList
         * input: none
         * OUTPUT: List<string> of cameras to get the new pictures from
         */
        static List<string> siteList()
        {
            // SQL-formatted query string from config file
            string queryString = ConfigurationManager.AppSettings.Get("queryString");

            // SQL server credentials from config file
            string connectionString = ConfigurationManager.AppSettings.Get("sqlConnectionString");

            // List of requested sites
            List<string> columnData = new List<string>();
            ErrorLog oErrorLog = new ErrorLog();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(queryString, connection))
                    {
                        try
                        {
                            SqlDataReader sqlDataReader = command.ExecuteReader();
                            using SqlDataReader reader = sqlDataReader;
                            Console.WriteLine("Requested Sites:");
                            while (reader.Read())
                            {
                                Console.WriteLine(reader.GetInt32(0));

                                //Add to list from SQL reader
                                columnData.Add(Convert.ToString(reader.GetInt32(0)));
                            }
                            Console.WriteLine("");
                            return columnData;
                        }
                        catch (SqlException obdcEx)
                        {
                            oErrorLog.WriteErrorLog(obdcEx.ToString());
                            return null;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                oErrorLog.WriteErrorLog(Ex.ToString());
                return null;
            }
            
        }

        private static void ftpcheck(List<string> siteNums)
        { // function takes all the numbers from the given SQL tables and finds the relavant picture paths to be downloaded. 
          // Input: siteNums-- list of site numbers to check


            String RemoteFtpPath = ConfigurationManager.AppSettings.Get("ftpDir"); //target to search
            String Username = ConfigurationManager.AppSettings.Get("ftpUsr");  // Credentials
            String Password = ConfigurationManager.AppSettings.Get("ftpPwd");
            Boolean UseBinary = true; // since we have images, not text
            Boolean UsePassive = false;



            // Ftp "List Directory" method will return a string of a filename with each call of the stream reader
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(RemoteFtpPath);

            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.KeepAlive = true;
            request.UsePassive = UsePassive;
            request.UseBinary = UseBinary;
            request.Credentials = new NetworkCredential(Username, Password);

            ErrorLog oErrorLog = new ErrorLog();
            try
            {


                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    /* splits the filename as so:
                     * 0: vid
                     * 1: 000XXXXXX (site number)
                     * 2: Camera?
                     * 3: camera?
                     * 4: year
                     * 5: month
                     * 6: day
                     * 7: hour
                     * 8: minute
                     * 
                     * All in greenwich time
                     */
                    string[] subs = line.Split('-');
                    // Only pictures are formatted with this many dashes
                    if (subs.Length < 8) continue;

                    //Removes the leading zeros in the filename
                    subs[1] = subs[1].Substring(3);


                    //Removes the .jpg from the last token
                    subs[8] = subs[8].Substring(0, subs[8].Length - 4);

                    DateTime rn = DateTime.UtcNow;


                    if (subs[0].Equals("Vid") && siteNums.Contains(subs[1]))
                    //Checks that it is a picture from the server, and if the site number is in the SQL output list
                    {
                        DateTime timeStamp = new DateTime(Convert.ToInt32(subs[4]), Convert.ToInt32(subs[5]), Convert.ToInt32(subs[6]), Convert.ToInt32(subs[7]), Convert.ToInt32(subs[8]), 0);

                        TimeSpan elapsed = rn - timeStamp;

                        if (elapsed.TotalMinutes <= Convert.ToInt32(ConfigurationManager.AppSettings.Get("range")))
                        {

                            Console.WriteLine("Downloading " + line);
                            ftpDownload(line, line.Substring(0, 19)+".jpg");

                        }


                    }
                    continue;

                }
            } catch(Exception ex)
            {
                oErrorLog.WriteErrorLog(ex.ToString());
            }
            return;


        }


        private static bool ftpDownload(string requestedPath, string destination)
        {
            // This Function downloads the given ftp file to a given path
            // Input:
            //      requestedPath: string of path of desired ftp file
            //      destination: string of path of where the file will go
            String RemoteFtpPath = ConfigurationManager.AppSettings.Get("ftpDir") + requestedPath;
            String LocalDestinationPath = ConfigurationManager.AppSettings.Get("targetDir") + destination;
            String Username = ConfigurationManager.AppSettings.Get("ftpUsr");
            String Password = ConfigurationManager.AppSettings.Get("ftpPwd");
            Boolean UseBinary = true; 
            Boolean UsePassive = false;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(RemoteFtpPath);

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.KeepAlive = true;
            request.UsePassive = UsePassive;
            request.UseBinary = UseBinary;

            request.Credentials = new NetworkCredential(Username, Password);

            ErrorLog oErrorLog = new ErrorLog();
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                using (FileStream writer = new FileStream(LocalDestinationPath, FileMode.Create))
                {

                    long length = response.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[2048];

                    readCount = responseStream.Read(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        writer.Write(buffer, 0, readCount);
                        readCount = responseStream.Read(buffer, 0, bufferSize);
                    }
                }

                reader.Close();
                response.Close();
                return true;
            }catch(Exception ex)
            {
                oErrorLog.WriteErrorLog(ex.ToString());
                return false;
            }
            
        }

        private static void deleteOld(string deleteWave)
        {
            int minutes = Convert.ToInt32(deleteWave);
            TimeSpan elapsed;
            
            string[] allfiles = Directory.GetFiles(ConfigurationManager.AppSettings.Get("targetDir"), "*.jpg*", SearchOption.TopDirectoryOnly);
            foreach (string file in allfiles)
            {
                FileInfo meta = new FileInfo(file);
                DateTime dt = meta.CreationTime;
                dt = dt.ToUniversalTime();
                elapsed = DateTime.UtcNow - dt;
                Console.WriteLine("File: " + file + ", creationTime:" + elapsed.TotalMinutes + " minutes ago");
                if (elapsed.TotalMinutes >= minutes)
                {
                    Console.WriteLine("Deleting "+file);
                    File.Delete(file);
                }
                else continue;
            }
        }

        class ErrorLog
        {
            public bool WriteErrorLog(string LogMessage)
            {
                bool Status = false;
                string LogDirectory = ConfigurationManager.AppSettings["LogDirectory"].ToString();

                DateTime CurrentDateTime = DateTime.Now;
                string CurrentDateTimeString = CurrentDateTime.ToString();
                CheckCreateLogDirectory(LogDirectory);
                string logLine = BuildLogLine(CurrentDateTime, LogMessage);
                LogDirectory = (LogDirectory + "Log_" + LogFileName(DateTime.Now) + ".txt");

                lock (typeof(ErrorLog))
                {
                    StreamWriter oStreamWriter = null;
                    try
                    {
                        oStreamWriter = new StreamWriter(LogDirectory, true);
                        oStreamWriter.WriteLine(logLine);
                        Status = true;
                    }
                    catch
                    {

                    }
                    finally
                    {
                        if (oStreamWriter != null)
                        {
                            oStreamWriter.Close();
                        }
                    }
                }
                return Status;
            }


            private bool CheckCreateLogDirectory(string LogPath)
            {
                bool loggingDirectoryExists = false;
                DirectoryInfo oDirectoryInfo = new DirectoryInfo(LogPath);
                if (oDirectoryInfo.Exists)
                {
                    loggingDirectoryExists = true;
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(LogPath);
                        loggingDirectoryExists = true;
                    }
                    catch
                    {
                        // Logging failure
                    }
                }
                return loggingDirectoryExists;
            }


            private string BuildLogLine(DateTime CurrentDateTime, string LogMessage)
            {
                StringBuilder loglineStringBuilder = new StringBuilder();
                loglineStringBuilder.Append(LogFileEntryDateTime(CurrentDateTime));
                loglineStringBuilder.Append(" \t");
                loglineStringBuilder.Append(LogMessage);
                return loglineStringBuilder.ToString();
            }


            public string LogFileEntryDateTime(DateTime CurrentDateTime)
            {
                return CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }


            private string LogFileName(DateTime CurrentDateTime)
            {
                return CurrentDateTime.ToString("yyyy_MM_dd");
            }
        }
    }



}