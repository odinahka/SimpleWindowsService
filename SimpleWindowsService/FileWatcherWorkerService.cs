using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWindowsService
{
    internal class FileWatcherWorkerService : IHostedService, IDisposable
    {

        private bool _isRunning;
        private Thread _thread;
        private FileSystemWatcher _watcher;
        private ReaderWriterLockSlim _lock; 
        private readonly ILogger<FileWatcherWorkerService> _logger;

        public FileWatcherWorkerService(ILogger<FileWatcherWorkerService> logger)
        {
            _logger = logger;
        }
        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lock = new ReaderWriterLockSlim();
            //file watcher
            _watcher = new FileSystemWatcher
            {
                Filter = "*.*",
                Path = GetAppSettings("TargetPath"),
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _watcher.Created += Watcher_Created;

            //thread
            _isRunning = true;
            ThreadStart start = new ThreadStart(Perform);

            _thread = new Thread(start);
            _thread.Start();

            _logger.LogInformation("File watcher started");

            return Task.CompletedTask;

        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            _lock.EnterWriteLock();
            string ext = Path.GetExtension(e.FullPath); 

            //filter only *.txt and *.doc, *.docx
            if (ext.ToLower().Contains(".txt")|| ext.ToLower().Contains(".doc") || ext.ToLower().Contains(".docx"))
            {
                var fileName = Path.GetFileName(e.FullPath);
                _logger.LogInformation("Found file: {fileName}", fileName);

                //move to /saved folder
                var newFileName = DateTime.Now.ToString("yyyyMMdd-HHmm");
                var SavedPath = GetAppSettings("SavePath");
                File.Move(e.FullPath, SavedPath + "\\" + newFileName + ext);
               
                _logger.LogInformation("File was moved");
            }
            _lock.ExitWriteLock();
        }
        public void Perform()
        {
            try
            {
                while (_isRunning)
                {
                    if (!_isRunning)
                        break;

                    Thread.Sleep(800);
                }
            }
            catch (Exception)
            {

            }
        }
        private string GetAppSettings(string attribute)
        {
            var value = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build().GetSection("AppSettings")[attribute];
            return value;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _thread.Join(500); //waiting for thread to terminate
            return Task.CompletedTask;
        }
    }
}
