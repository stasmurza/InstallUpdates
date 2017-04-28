using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;


namespace InstallUpdates
{

    interface IDownloadingFiles
    {
        void DownloadUpdateIfAvailable();
    }


    interface IUpdatingProgram
    {
        void UpdateInfoBase();
    }


    public abstract class ALogging
    {
        protected string LogDirectory { get; private set; }

        public enum Importance { Information, Warning, Error }

        protected static SemaphoreSlim SlimSemaphore = new SemaphoreSlim(1);

        public ALogging(string logDirectory)
        {
            LogDirectory = logDirectory;
        }

        public virtual bool IsFullLogging()
        {
            return false;
        }

        public virtual void Log(string LogMessage, Importance importance)
        {
            //SlimSemaphore.Wait();
            //Write your code here
            //SlimSemaphore.Release();
        }
    }


    public class Logging: ALogging
    {        

        /// <summary>
        /// Logging
        /// </summary>
        /// <param name="logDirectory"></param>
        public Logging(string logDirectory):base(logDirectory)
        {

        }

        /// <summary>
        /// Is full logging
        /// </summary>
        public override bool IsFullLogging()
        {
            if (File.Exists(LogDirectory + "FullLog.txt"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Log exception message into txt file into logs folder 
        /// </summary>
        /// <param name="LogMessage"></param>
        public override void Log(string LogMessage, Importance importance)
        {
            try
            {
                if ((importance == Importance.Information) && (!IsFullLogging()))
                {                    
                    return;
                }

                SlimSemaphore.Wait();

                string text = DateTime.Now.ToString();
                text += Environment.NewLine + LogMessage + Environment.NewLine + Environment.NewLine;

                Console.WriteLine(text);

                System.IO.File.AppendAllText(LogDirectory + GetType().Name + DateTime.Now.ToString("yyyyMMdd") + ".txt", text);

                SlimSemaphore.Release();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                Console.ReadLine();

                Environment.Exit(0);
            }
        }
    }


    public class Parameters
    {

        public Int32 KindOfRunning { get; private set; }
        public string WebDirectory { get; private set; }
        public string FileListName { get; private set; }
        public string DBDirectory { get; private set; }
        public string ExeFileDirectory { get; private set; }
        public Int32 DealersBaseVersion { get; private set; }
        public string UpdateDirectory { get; private set; }
        public string LogDirectory { get; private set; }
        public string PRMFileDirectory { get; private set; }
        public string DBUserName { get; private set; }
        public string DBUserPassword { get; private set; }
        public string FileListPath { get; private set; }

        private Logging logging;


        public Parameters(string[] args)
        {
            try
            {
                foreach (string str in args)
                {
                    Console.WriteLine(str);
                }

                string ParametersTip = "Параметры:" + Environment.NewLine;
                ParametersTip += "0 - режим работы. 0 - проверять наличие обновлений постоянно. 1 проверить наличие обновлений разово." + Environment.NewLine;
                ParametersTip += "1 - интернет каталог загрузки обновлений." + Environment.NewLine;
                ParametersTip += "2 - имя файла со списком загружаемых файлов." + Environment.NewLine;
                ParametersTip += "3 - путь к каталогу информационной базы." + Environment.NewLine;
                ParametersTip += "4 - путь к исполняемому файлу 1С7." + Environment.NewLine;
                ParametersTip += "5 - версия дилерской базы." + Environment.NewLine;

                if (args.Length != 6)
                {
                    throw new ArgumentException(@"Неверное количество параметров! " + Environment.NewLine + ParametersTip);
                }

                Int32 kindOfRunning = Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
                string webDirectory = args[1];
                string fileListName = args[2];
                string dBDirectory = args[3];
                string exeFileDirectory = args[4];
                string dealersBaseVersion = args[5];


                //Проверка корректности параметров командной строки

                //Проверка корректности параметра 1 KindOfRunning 
                string pattern1 = @"[0|1]{1}";
                Regex regex1 = new Regex(pattern1);
                if (!regex1.IsMatch(kindOfRunning.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 1: {0}", kindOfRunning.ToString()));

                //Проверка корректности параметра 2 WebDirectory
                string pattern2_1 = @"^((https://)|(http://))";
                Regex regex2_1 = new Regex(pattern2_1, RegexOptions.IgnoreCase);
                if (!regex2_1.IsMatch(webDirectory.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 2: {0}", webDirectory.ToString()));

                string pattern2_2 = @"\w*.\w";
                Regex regex2_2 = new Regex(pattern2_2, RegexOptions.IgnoreCase);
                if (!regex2_2.IsMatch(webDirectory.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 2: {0}", webDirectory.ToString()));

                //Проверка корректности параметра 3 FileListName
                string pattern3 = @"\w*\.json";
                Regex regex3 = new Regex(pattern3, RegexOptions.IgnoreCase);
                if (!regex3.IsMatch(fileListName.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 3: {0}", fileListName.ToString()));

                //Проверка корректности параметра 4 DBDirectory
                string pattern4 = @"^([A-Z]{1}:\\)";
                Regex regex4 = new Regex(pattern4, RegexOptions.IgnoreCase);
                if (!regex4.IsMatch(dBDirectory.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 4: {0}", dBDirectory.ToString()));

                //Проверка корректности параметра 5 DealersBaseVersion                
                string pattern5 = @"\d*";
                Regex regex5 = new Regex(pattern5, RegexOptions.IgnoreCase);
                if (!regex5.IsMatch(dealersBaseVersion.ToString()))
                    throw new ArgumentOutOfRangeException(String.Format("Неверный параметр № 5: {0}", dealersBaseVersion.ToString()));

                char[] MyChar = { '"', ' ' };

                webDirectory = webDirectory.Trim(MyChar).Trim();
                fileListName = fileListName.Trim(MyChar).Trim();
                dBDirectory = dBDirectory.Trim(MyChar).Trim();
                exeFileDirectory = exeFileDirectory.Trim(MyChar).Trim();

                if (dBDirectory.Substring(dBDirectory.Length - 1, 1) != "\\")
                    dBDirectory += "\\";


                KindOfRunning = kindOfRunning;
                WebDirectory = webDirectory;
                FileListName = fileListName;
                DBDirectory = dBDirectory;
                ExeFileDirectory = exeFileDirectory;
                DealersBaseVersion = Convert.ToInt32(dealersBaseVersion);
                UpdateDirectory = dBDirectory + "CP\\";
                LogDirectory = dBDirectory + "Update log\\" + DealersBaseVersion + "\\";
                PRMFileDirectory = UpdateDirectory + "PB.prm";
                DBUserName = @"Update";
                DBUserPassword = @"Admin1";
                FileListPath = UpdateDirectory + FileListName;

                Console.WriteLine("Creating update directory");
                if (!Directory.Exists(UpdateDirectory))
                {
                    Directory.CreateDirectory(UpdateDirectory);
                }

                Console.WriteLine("Creating log directory");
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }


                logging = new Logging(LogDirectory);

                string LogMessage = String.Format("Creating new instance of class Parameters with parameters: " + Environment.NewLine +
                    "KindOfRunning: {0};" + Environment.NewLine +
                    "WebDirectory: {1};" + Environment.NewLine +
                    "FileListName: {2};" + Environment.NewLine +
                    "DBDirectory:{3}" + Environment.NewLine +
                    "ExeFileDirectory:{4}" + Environment.NewLine +
                    "DealersBaseVersion:{5}" + Environment.NewLine +
                    "UpdateDirectory:{6}" + Environment.NewLine +
                    "LogDirectory:{7}" + Environment.NewLine +
                    "PRMFileDirectory:{8}" + Environment.NewLine,
                    KindOfRunning, WebDirectory, FileListName, DBDirectory, ExeFileDirectory, DealersBaseVersion, UpdateDirectory, LogDirectory, PRMFileDirectory);

                logging.Log(LogMessage, Logging.Importance.Information);
                
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                Console.ReadLine();
                throw;
            }
        }
    }


    public class GetAndInstallUpdates
    {
        private Parameters parameters;
        private Logging logging;

        public GetAndInstallUpdates(string[] args)
        {
            parameters = new Parameters(args);

            logging = new Logging(parameters.LogDirectory);
        }

        public void Start()
        {
            try
            {
                DownloadingFiles downloadingFiles = new DownloadingFiles(parameters.WebDirectory, parameters.UpdateDirectory, parameters.DBDirectory,
                    parameters.LogDirectory, parameters.FileListName, parameters.FileListPath);

                if (parameters.KindOfRunning == 0)
                {
                    //Должно быть запущено только одно приложение
                    //String GUID = typeof(Program).GUID.ToString();
                    String GUID = Properties.Resources.GUID;
                    //String GUID = "D4D12054 - DE63 - 4247 - BFB8 - 18F99ED0E9E4";


                    MutexSecurity mutexSecurity = new MutexSecurity();
                    SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    MutexAccessRule accessRule = new MutexAccessRule(securityIdentifier, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow);

                    mutexSecurity.AddAccessRule(accessRule);

                    bool onlyInstance;
                    string MutexName = @"Global\" + GUID + parameters.DealersBaseVersion;
                    //string MutexName = @"Global\" + GUID;
                    Mutex mtx = new Mutex(true, MutexName, out onlyInstance, mutexSecurity);

                    // Если другие процессы не владеют мьютексом, то
                    // приложение запущено в единственном экземпляре
                    if (!onlyInstance)
                    {
                        return;
                    }



                    UpdatingProgram updatingProgram = new UpdatingProgram(parameters.UpdateDirectory, parameters.DBDirectory, parameters.LogDirectory,
                        parameters.ExeFileDirectory, parameters.PRMFileDirectory, parameters.DBUserName, parameters.DBUserPassword, parameters.DealersBaseVersion,
                        parameters.FileListPath);


                    Timer WebTimer = new Timer((Object stateInfo) => { downloadingFiles.DownloadUpdateIfAvailable(); });
                    Timer RunUpdateTimer = new Timer((Object stateInfo) => { updatingProgram.UpdateInfoBase(); });

                    updatingProgram.InfoBaseUpdated += (obj, args) =>
                    {
                        WebTimer.Dispose();
                        RunUpdateTimer.Dispose();
                    };

                    TimeSpan ZeroTime = new TimeSpan(0);
                    TimeSpan PeriodSearchFileInWeb = new TimeSpan(0, 5, 0);
                    TimeSpan PeriodRunUpdate = new TimeSpan(0, 0, 5);

                    WebTimer.Change(ZeroTime, PeriodSearchFileInWeb);
                    RunUpdateTimer.Change(ZeroTime, PeriodRunUpdate);                    

                    GC.KeepAlive(WebTimer);
                    GC.KeepAlive(RunUpdateTimer);

                    Semaphore semaphore = new Semaphore(0, 1);
                    semaphore.WaitOne();
                }
                else
                {
                    downloadingFiles.DownloadUpdateIfAvailable();
                }
            }
            catch(Exception exc)
            {
                logging.Log(exc.ToString(), Logging.Importance.Error);
            }
        }
    }


    [DataContract]
    public class FilesForDownloading
    {
        [DataMember]
        private int version { get; set; }

        [DataMember]
        private string name { get; set; }

        [DataMember]
        private string webDirectory { get; set; }

        [DataMember]
        private string folder { get; set; }        

        public int Version
        {
            get { return version; }
            set
            {
                if (value == default(int))
                    throw new ArgumentException("Не выбрано значение поля Version");

                version = value;
            }
        }

        public string Name
        {
            get { return name; }
            set
            {

                //Проверка корректности ввода Name

                if (value.Trim() == String.Empty)
                    throw new ArgumentException(Localization.EmptyField + " Name");

                //Поиск в строке недопустимых символов
                string pattern = @"[\s\\\/\:\*\?\<\>\|" + "\"" + "]"; 
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(value))
                    throw new ArgumentOutOfRangeException(Localization.InvalidStringFormat + " " + value.ToString());

                name = value;
            }
        }

        public string WebDirectory
        {
            get { return webDirectory; }
            set
            {

                //Проверка корректности ввода WebDirectory

                if (value.Trim() == String.Empty)
                    throw new ArgumentException(Localization.EmptyField + " WebDirectory");

                //Проверка начала строки на соответсвие шаблону
                string pattern1 = @"^((https://)|(http://))";
                Regex regex1 = new Regex(pattern1, RegexOptions.IgnoreCase);
                if (!regex1.IsMatch(value))
                    throw new ArgumentOutOfRangeException(Localization.InvalidStringFormat + " " + value.ToString());

                //Поиск в строке недопустимых символов
                string pattern2 = @"[\s\\\*\?\<\>\|" + "\"" + "]";
                Regex regex2 = new Regex(pattern2, RegexOptions.IgnoreCase);
                if (regex2.IsMatch(value))
                    throw new ArgumentOutOfRangeException(Localization.InvalidStringFormat + " " + value.ToString());

                webDirectory = value;
            }
        }
        
        public string Folder
        {
            get { return folder; }
            set {

                //Поиск в строке недопустимых символов
                string pattern = @"[\/\*\?\<\>\|" + "\"" + "]";
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(value))
                    throw new ArgumentOutOfRangeException(Localization.InvalidStringFormat + " " + value.ToString());

                folder = value;

            }
        }

        public override string ToString()
        {
            return Name + " " + Version;
        }
    }


    public class DownloadingFiles: IDownloadingFiles
    {

        private string WebDirectory { get; set; }
        private string UpdateDirectory { get; set; }
        private string DBDirectory { get; set; }
        private string LogDirectory { get; set; }
        private string FileListName { get; set; }
        private string FileListPath { get; set; }
        private Logging logging;


        public DownloadingFiles(string WebDirectory, string UpdateDirectory, string DBDirectory, string LogDirectory, string FileListName, string FileListPath)
        {
            this.WebDirectory = WebDirectory;
            this.UpdateDirectory = UpdateDirectory;
            this.DBDirectory = DBDirectory;
            this.LogDirectory = LogDirectory;
            this.FileListName = FileListName;
            this.FileListPath = FileListPath;

            logging = new Logging(LogDirectory);
        }


        /// <summary>
        /// Get list of files
        /// </summary>
        /// <returns></returns>
        public static List<FilesForDownloading> GetListOfFiles(string ListOfFilesLocalPath)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<FilesForDownloading>));

            FileStream fs = new FileStream(ListOfFilesLocalPath, FileMode.Open);
            List<FilesForDownloading> listOfFiles = (List<FilesForDownloading>)serializer.ReadObject(fs);
            fs.Close();
            fs.Dispose();

            if (listOfFiles == null)
            {
                //"Attempt get list of files was unsuccessfull!"
                throw new Exception(Localization.UnsuccessfulGettingListOfFiles);
            }

            return listOfFiles;
        }


        /// <summary>
        /// Get list of files
        /// </summary>
        /// <returns></returns>
        public static List<FilesForDownloading> GetListOfFiles(string ListOfFilesLocalPath, string LogDirectory)
        {

            Logging logging = new Logging(LogDirectory);
            string LogMessage = String.Format("Start getting list of files from path {0}", ListOfFilesLocalPath);
            logging.Log(LogMessage, Logging.Importance.Information);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<FilesForDownloading>));

            FileStream fs = new FileStream(ListOfFilesLocalPath, FileMode.Open);
            List<FilesForDownloading> listOfFiles = (List<FilesForDownloading>)serializer.ReadObject(fs);
            fs.Close();
            fs.Dispose();

            if (listOfFiles == null)
            {
                //"Attempt get list of files was unsuccessfull!"
                logging = new Logging(LogDirectory);
                logging.Log("Attempt get list of files was unsuccessfull!", Logging.Importance.Warning);
                throw new Exception(Localization.UnsuccessfulGettingListOfFiles);
            }

            logging = new Logging(LogDirectory);
            LogMessage = String.Format("Finish getting list of files from path {0}", ListOfFilesLocalPath);
            logging.Log(LogMessage, Logging.Importance.Information);

            return listOfFiles;
        }


        /// <summary>
        /// Download update (if available)
        /// </summary>
        public void DownloadUpdateIfAvailable()
        {
            try
            {
                String URLString = WebDirectory.Trim();

                if (URLString.Substring(URLString.Length - 1, 1) != @"/")
                    URLString += @"/";

                URLString += FileListName.Trim();

                DownloadFile(URLString, UpdateDirectory, FileListName);

                List<FilesForDownloading> listOfFiles = GetListOfFiles(FileListPath, LogDirectory);

                int LocalFileVersion = GetLocalFileVersion();

                int WebFileVersion = GetWebFileVersion(listOfFiles);

                if (WebFileVersion > LocalFileVersion)
                {
                    DownloadFiles(listOfFiles, LocalFileVersion);

                    UpdateLocalFileVersion(WebFileVersion);
                }
            }
            catch (Exception exception)
            {
                logging.Log(exception.ToString(), Logging.Importance.Error);
            }
        }


        /// <summary>
        /// Get version of local update file
        /// </summary>
        /// <param name="IBDirectory"></param>
        private int GetLocalFileVersion()
        {
            string VersionFilePath = UpdateDirectory + "version.xml";
            string ElementName = String.Empty;
            int CurrentVersion = 0;

            string LogMessage = String.Format("Start getting local file version, version files path: {0}", VersionFilePath);
            logging.Log(LogMessage, Logging.Importance.Information);

            if (!File.Exists(VersionFilePath))
            {
                LogMessage = String.Format("File isn't exist: {0}", VersionFilePath);
                logging.Log(LogMessage, Logging.Importance.Warning);

                return CurrentVersion;
            }

            LogMessage = String.Format("Start reading file: {0}", VersionFilePath);
            logging.Log(LogMessage, Logging.Importance.Information);

            using (XmlTextReader reader = new XmlTextReader(VersionFilePath))
            {                
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:

                            ElementName = reader.Name;
                            break;

                        case XmlNodeType.Text:

                            LogMessage = String.Format("Name: {0}; Value: {1}", ElementName, reader.Value);
                            logging.Log(LogMessage, Logging.Importance.Information);

                            if (ElementName.ToLower() == "version")
                            {
                                //String StrVer = reader.Value.Replace(",", ".");
                                String StrVer = reader.Value;
                                CurrentVersion = Convert.ToInt32(StrVer, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                }
                reader.Close();
            }

            LogMessage = String.Format("Finish reading file: {0}", VersionFilePath);
            logging.Log(LogMessage, Logging.Importance.Information);

            LogMessage = String.Format("Current version from local file: {0}", CurrentVersion);
            logging.Log(LogMessage, Logging.Importance.Information);

            LogMessage = String.Format("Finish getting local file version, version files path: {0}", VersionFilePath);
            logging.Log(LogMessage, Logging.Importance.Information);

            return CurrentVersion;

        }


        /// <summary>
        /// Get version of update file
        /// </summary>
        /// <param name="WebDirectory"></param>
        /// <param name="LocalDirectory"></param>
        private int GetWebFileVersion(List<FilesForDownloading> listOfFiles)
        {
            logging.Log("Start getting web file version", Logging.Importance.Information);

            int WebFileVersion = (from item in listOfFiles
                                     orderby item.Version descending
                                     select item.Version).FirstOrDefault();

            string LogMessage = String.Format("Web file version: {0}", WebFileVersion);
            logging.Log(LogMessage, Logging.Importance.Information);

            //Can not get version of update file
            if (WebFileVersion == default(int))
                throw new Exception(Localization.UnsuccessfulGettingVersionOfUpdateFile);

            logging.Log("Finish getting web file version", Logging.Importance.Information);

            return WebFileVersion;

        }


        /// <summary>
        /// Download one file
        /// </summary>
        /// <param name="listOfFiles"></param>
        private void DownloadFile(string FilesURL, string LocalDirectory, string FilesName)
        {
            string LogMessage = String.Format("Start downloading file: {0}", FilesURL);
            logging.Log(LogMessage, Logging.Importance.Information);

            string LocalFilePath = LocalDirectory.Trim();

            if (LocalFilePath.Substring(LocalFilePath.Length - 1, 1) != "\\")
                LocalFilePath += "\\";

            LocalFilePath += FilesName;            

            if (!Directory.Exists(LocalDirectory))
            {
                LogMessage = String.Format("Creating directory: {0}", LocalDirectory);
                logging.Log(LogMessage, Logging.Importance.Information);

                Directory.CreateDirectory(LocalDirectory);
            }

            LogMessage = String.Format("Downloading file {0}, to directory {1}, full path {2}", FilesURL, LocalDirectory, LocalFilePath);
            logging.Log(LogMessage, Logging.Importance.Information);

            WebClient myWebClient = new WebClient();
            myWebClient.DownloadFile(FilesURL, LocalFilePath);

            myWebClient.Dispose();

            LogMessage = String.Format("Finish downloading file: {0}", FilesURL);
            logging.Log(LogMessage, Logging.Importance.Information);
        }


        /// <summary>
        /// Download updates file
        /// </summary>
        /// <param name="listOfFiles"></param>
        private void DownloadFiles(List<FilesForDownloading> listOfFiles, int LocalFileVersion)
        {
            logging.Log("Start downloading files", Logging.Importance.Information);

            WebClient myWebClient = new WebClient();

            foreach (FilesForDownloading item in listOfFiles)
            {

                if (item.Version <= LocalFileVersion)
                    continue;

                string CurrentFileDirectory = UpdateDirectory + item.Version + "\\";

                if (item.Folder != null)
                    if (item.Folder.Trim() != String.Empty)
                        CurrentFileDirectory += item.Folder.Trim(); 

                if (CurrentFileDirectory.Substring(CurrentFileDirectory.Length - 1, 1) != @"\")
                    CurrentFileDirectory += @"\";

                string CurrentFilePath = CurrentFileDirectory + item.Name;

                string FileURL = item.WebDirectory.Trim();

                if (FileURL.Substring(FileURL.Length - 1, 1) != @"/")
                    FileURL += @"/";

                FileURL += item.Name.Trim();

                if (!Directory.Exists(CurrentFileDirectory))
                {

                    logging.Log(String.Format("Creating directory: {0}", CurrentFileDirectory), Logging.Importance.Information);
                    Directory.CreateDirectory(CurrentFileDirectory);
                }

                string LogMessage = String.Format("Start downloading file {0}, version {1}, full path {2}", FileURL, item.Version, CurrentFilePath);
                logging.Log(LogMessage, Logging.Importance.Information);

                myWebClient.DownloadFile(FileURL, CurrentFilePath);

                LogMessage = String.Format("Finish downloading file {0}, version {1}, full path {2}", FileURL, item.Version, CurrentFilePath);
                logging.Log(LogMessage, Logging.Importance.Information);
            }

            myWebClient.Dispose();


            logging.Log("Finish downloading files", Logging.Importance.Information);
        }


        /// <summary>
        /// Update local file version
        /// </summary>
        /// <param name="WebFileVersion"></param>        
        private void UpdateLocalFileVersion(int WebFileVersion)
        {
            logging.Log("Start updating local file version", Logging.Importance.Information);

            //Linq to xml
            XDocument xdoc = new XDocument();

            // создаем первый элемент
            XElement version = new XElement("version", WebFileVersion);

            // создаем корневой элемент
            XElement update = new XElement("update");

            // добавляем в корневой элемент
            update.Add(version);

            // добавляем корневой элемент в документ
            xdoc.Add(update);

            //сохраняем документ
            xdoc.Save(UpdateDirectory + "version.xml");

            StringWriter stringWriter = new StringWriter();
            XmlTextWriter textWriter = new XmlTextWriter(stringWriter);
            xdoc.WriteTo(textWriter);

            string LogMessage = String.Format("Content of file version.xml: {0}", stringWriter.ToString());
            logging.Log(LogMessage, Logging.Importance.Information);

            textWriter.Close();
            stringWriter.Close();

            logging.Log("Finish updating local file version", Logging.Importance.Information);

        }
    }


    public class UpdatingProgram: IUpdatingProgram
    {
        private ResultOfUpdating form;
        public event EventHandler InfoBaseUpdated;

        private string UpdateDirectory { get; set; }
        private string DBDirectory { get; set; }
        private string ExeFileDirectory { get; set; }
        private string LogDirectory { get; set; }
        private string PRMFileDirectory { get; set; }
        private string DBUserName { get; set; }
        private string DBUserPassword { get; set; }
        private int DealersBaseVersion { get; set; }
        private string FileListPath { get; set; }
        private Logging logging;

        public static SemaphoreSlim SlimSemaphore = new SemaphoreSlim(1);

        public UpdatingProgram(string UpdateDirectory, string DBDirectory, string LogDirectory, string ExeFileDirectory, string PRMFileDirectory, string DBUserName,
        string DBUserPassword, int DealersBaseVersion, string FileListPath)
        {
            this.UpdateDirectory = UpdateDirectory;
            this.DBDirectory = DBDirectory;
            this.LogDirectory = LogDirectory;
            this.ExeFileDirectory = ExeFileDirectory;            
            this.PRMFileDirectory = PRMFileDirectory;
            this.DBUserName = DBUserName;
            this.DBUserPassword = DBUserPassword;
            this.DealersBaseVersion = DealersBaseVersion;
            this.FileListPath = FileListPath;
            logging = new Logging(LogDirectory);            
        }


        /// <summary>
        /// Command run has done
        /// </summary>
        /// <param name="IBDirectory"></param>
        private bool CommandRunHasDone()
        {
            bool commandRunHasDone = false;

            string LogMessage;
            logging.Log("Start checking if command run has done", Logging.Importance.Information);

            string VersionFileDirectory = UpdateDirectory + "running.xml";
            string ElementName = String.Empty;
            Int32 CurrentValue = 0;

            if (!File.Exists(VersionFileDirectory))
                return false;

            using (XmlTextReader reader = new XmlTextReader(VersionFileDirectory))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:

                            ElementName = reader.Name;
                            break;

                        case XmlNodeType.Text:

                            LogMessage = String.Format("Name: {0}; Value: {1}", ElementName, reader.Value);
                            logging.Log(LogMessage, Logging.Importance.Information);

                            if (ElementName.ToLower() == "run")
                            {
                                String StrVal = reader.Value;
                                CurrentValue = Convert.ToInt32(StrVal, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                }
                reader.Close();
            }

            if (CurrentValue == 1)
                commandRunHasDone = true;

            LogMessage = String.Format("Value of command run has done: {0}", commandRunHasDone);
            logging.Log(LogMessage, Logging.Importance.Information);

            logging.Log("Finish checking if command run has done", Logging.Importance.Information);

            return commandRunHasDone;

        }



        /// <summary>
        /// Transfer file from download directory to destination directory
        /// </summary>
        /// <param name="listOfFiles"></param>
        private void TransferFiles()
        {

            logging.Log("Start tranfering files", Logging.Importance.Information);

            List<FilesForDownloading> listOfFiles = DownloadingFiles.GetListOfFiles(FileListPath, LogDirectory);

            foreach (FilesForDownloading item in listOfFiles)
            {

                if (item.Version <= DealersBaseVersion)
                    continue;


                //Destination files path
                string DestFileDirectory = DBDirectory;

                if (item.Folder != null)
                    if (item.Folder.Trim() != String.Empty)
                        DestFileDirectory += item.Folder;

                if (DestFileDirectory.Substring(DestFileDirectory.Length - 1, 1) != @"\")
                    DestFileDirectory += @"\";

                string DestFilePath = DestFileDirectory + item.Name;


                //Source files path
                string SourceFileDirectory = UpdateDirectory + item.Version + "\\";
                if (item.Folder != null)
                    if (item.Folder.Trim() != String.Empty)
                        SourceFileDirectory  +=  item.Folder.Trim();

                if (SourceFileDirectory.Substring(SourceFileDirectory.Length - 1, 1) != @"\")
                    SourceFileDirectory += @"\";

                string SourceFilePath = SourceFileDirectory + item.Name;


                if (!Directory.Exists(DestFileDirectory))
                {

                    logging.Log(String.Format("Creating directory: {0}", DestFileDirectory), Logging.Importance.Information);
                    Directory.CreateDirectory(DestFileDirectory);
                }

                string LogMessage = String.Format("Tranfering file {0}, version {1}, source {2}, destination {3}", item.Name, item.Version, SourceFilePath, DestFilePath);
                logging.Log(LogMessage, Logging.Importance.Information);

                File.Copy(SourceFilePath, DestFilePath, true);
            }

            logging.Log("Finish transfering files", Logging.Importance.Information);
        }


        /// <summary>
        /// Run search and download updates
        /// </summary>
        public void UpdateInfoBase()
        {
            try
            {
                SlimSemaphore.Wait();

                bool RunUpdate = CommandRunHasDone();

                if (!RunUpdate)
                {
                    SlimSemaphore.Release();
                    return;
                }

                UpdateFileRunning();

                logging.Log("Start updating infobase", Logging.Importance.Information);

                //На случай длительного закрытия базы
                Thread.Sleep(3000);

                //Create log file for 1C
                FileStream file = File.Create(UpdateDirectory + "log.log");
                file.Close();


                //Transfer files
                TransferFiles();

                //Run 1C auto exchange
                Run1CAutoExchange();

                Thread.Sleep(1000);

                string UpdateHasDone = "DistUplSuc";
                //string UpdateHasntDone = "DistUplFail";
                string ErrorCode = ";5;";

                bool IsUpdateSuccessfull = false;
                string ErrorMessage = String.Empty;

                logging.Log("Start reading 1C log file", Logging.Importance.Information);

                string[] lines = System.IO.File.ReadAllLines(UpdateDirectory + "log.log", Encoding.Default);

                logging.Log("Finish reading 1C log file", Logging.Importance.Information);

                logging.Log("Start parsing 1C log file", Logging.Importance.Information);

                //Search is better make since last row
                for (int i = lines.Length; i > 0; i--)
                {
                    string line = lines[i - 1];

                    if (line.Contains(UpdateHasDone))
                    {
                        IsUpdateSuccessfull = true;
                        break;
                    }

                    if (line.Contains(ErrorCode))
                    {
                        int FirstSymbol = line.IndexOf(ErrorCode);
                        int StartSymbol = FirstSymbol + 3;
                        string ErrMessage = line.Substring(StartSymbol);
                        ErrMessage = ErrMessage.Replace(";;", "");

                        ErrorMessage += ErrorMessage == String.Empty ? ErrMessage : Environment.NewLine + ErrMessage;
                        break;
                    }
                }

                logging.Log("Finish parsing 1C log file", Logging.Importance.Information);

                if (IsUpdateSuccessfull)
                {
                    InfoBaseUpdated(this, new EventArgs());
                }

                string LogMessage = String.Empty;

                if (logging.IsFullLogging())
                {
                    LogMessage = "Content of 1C log:" + Environment.NewLine;

                    LogMessage += String.Join(Environment.NewLine, lines);

                    logging.Log(LogMessage, Logging.Importance.Information);
                }


                string SearchStr = "Ошибка блокировки метаданных. Возможно, метаданные используются другой задачей.";
                //string ReplaceString = "Ошибка блокировки базы. Обнаружены работающие пользователи!!";
                string ReplaceString = Localization.ErrorDatabaseIsLocked;
                ErrorMessage = ErrorMessage.Replace(SearchStr, ReplaceString);

                String MessageResult = String.Empty;
                if (IsUpdateSuccessfull)
                {
                    //Обновление установлено
                    MessageResult += Localization.UpdateIsInstalled;

                    LogMessage = String.Format("Result of updating: {0}", MessageResult);
                    logging.Log(LogMessage, Logging.Importance.Information);
                }
                else
                {
                    //Не удалось выполнить обновление информационной базы
                    MessageResult += Localization.UpdateIsNotInstalled;
                    MessageResult += Environment.NewLine;
                    MessageResult += Environment.NewLine + ErrorMessage;

                    LogMessage = String.Format("Result of updating: {0}", MessageResult);
                    logging.Log(LogMessage, Logging.Importance.Warning);
                }

                logging.Log("Finish updating infobase", Logging.Importance.Information);

                SlimSemaphore.Release();

                //if update is successfull aplication must be finished after showing message for user
                if (form == null)
                {
                    logging.Log("Creating new users form for showing results", Logging.Importance.Information);

                    form = new ResultOfUpdating();

                    form.FormClosed += (obj, args) =>
                    {
                        if (IsUpdateSuccessfull)
                        {
                            logging.Log("Aplication is finishing work after successfull updating", Logging.Importance.Warning);
                            Environment.Exit(0);
                        }
                    };
                }


                //Showing message with result of update for user
                form.labelResult.Text = MessageResult;

                bool FormOpen = false;
                foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                {
                    FormOpen = f == form;
                    if (FormOpen)
                        break;
                }

                if (!FormOpen)
                {
                    logging.Log("Showing dialog for user with results", Logging.Importance.Information);

                    form.ShowDialog();
                }

                logging.Log("Activating users form", Logging.Importance.Information);

                form.Activate();

            }
            catch (Exception exception)
            {

                logging.Log(exception.ToString(), Logging.Importance.Error);

                if (SlimSemaphore.CurrentCount == 0)
                    SlimSemaphore.Release();

                throw;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool Run1CAutoExchange()
        {
            logging.Log("Start running 1C auto exchange", Logging.Importance.Information);

            //WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            //bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);
            //Console.WriteLine("hasAdministrativeRight {0}", hasAdministrativeRight);

            bool ResultOfUpdate = false;

            //создаем новый процесс
            Process ProcAExch = new Process();
            //ProcAExch.StartInfo.CreateNoWindow = true;
            ProcAExch.StartInfo.FileName = ExeFileDirectory;
            ProcAExch.StartInfo.Arguments = @" config /D" + "\"" + DBDirectory + "\"" + " /N" + DBUserName + " /P" + DBUserPassword + " /@" + "\"" + PRMFileDirectory + "\"";
            //ProcAExch.StartInfo.UseShellExecute = false;

            //if (hasAdministrativeRight)
            if (Environment.OSVersion.Version.Major >= 6)
            {
                ProcAExch.StartInfo.Verb = "runas";
            }

            bool Started = ProcAExch.Start();
            ProcAExch.WaitForExit();
            int ExitCode = ProcAExch.ExitCode;
            Console.WriteLine(ProcAExch.ExitCode);
            ProcAExch.Close();

            if (ExitCode != 0)
                ResultOfUpdate = true;

            ProcAExch = new Process();
            ProcAExch.StartInfo.CreateNoWindow = false;
            ProcAExch.StartInfo.FileName = ExeFileDirectory;
            ProcAExch.StartInfo.Arguments = @" enterprise /D" + "\"" + DBDirectory + "\"" + " /N" + DBUserName + " /P" + DBUserPassword;
            ProcAExch.Start();
            ProcAExch.Close();

            logging.Log("Finish running 1C auto exchange", Logging.Importance.Information);

            return ResultOfUpdate;

        }


        /// <summary>
        /// Update file running
        /// </summary>
        /// <param name="UpdatesDirectory"></param>
        private void UpdateFileRunning()
        {

            logging.Log("Start updating file running", Logging.Importance.Information);

            //Linq to xml
            XDocument xdoc = new XDocument();

            // создаем первый элемент
            XElement version = new XElement("run", 0);

            // создаем корневой элемент
            XElement update = new XElement("update");

            // добавляем в корневой элемент
            update.Add(version);

            // добавляем корневой элемент в документ
            xdoc.Add(update);

            //сохраняем документ
            xdoc.Save(UpdateDirectory + "running.xml");


            StringWriter stringWriter = new StringWriter();
            XmlTextWriter textWriter = new XmlTextWriter(stringWriter);
            xdoc.WriteTo(textWriter);

            string LogMessage = String.Format("Content of file running: {0}", stringWriter.ToString());
            logging.Log(LogMessage, Logging.Importance.Information);

            textWriter.Close();
            stringWriter.Close();
            
            logging.Log("Finish updating file running", Logging.Importance.Information);

        }
    }
}
