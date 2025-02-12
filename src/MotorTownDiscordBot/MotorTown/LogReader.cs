namespace MotorTownDiscordBot.MotorTown;
internal class LogReader
{
    private string _path;
    private FileInfo? _file;
    private long _lastMaxOffset;

    public LogReader(string path)
    {
        _path = path;
        WatchDirectory();
    }

    public async IAsyncEnumerable<string> ReadAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync())
        {
            if (_file is null) continue;

            FileStream stream = _file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(stream);

            using (stream)
            using (reader)
            {
                //if the file size has not changed, idle
                if (reader.BaseStream.Length == _lastMaxOffset)
                    continue;

                //seek to the last max offset
                reader.BaseStream.Seek(_lastMaxOffset, SeekOrigin.Begin);

                //read out of the file until the EOF
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line.TrimEnd('\n');
                }

                //update the last max offset
                _lastMaxOffset = reader.BaseStream.Position;
            }
        }
    }

    private void WatchDirectory()
    {
        _file = GetLastLogFile(_path);
        if (_file is not null)
        {
            ReadFile(_file.FullName);
        }

        var watcher = new FileSystemWatcher(_path);
        watcher.Created += OnCreated;

        watcher.Filter = "*.log";
        watcher.EnableRaisingEvents = true;
        return;
    }

    private FileInfo? GetLastLogFile(string path)
    {
        DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder

        FileInfo[] Files = d.GetFiles("*.log"); //Getting Text files

        Files.OrderBy(file => file.LastWriteTime);

        return Files.Last();
    }


    private void ReadFile(string path)
    {
        _file = new FileInfo(path);
        _lastMaxOffset = _file.Length;
        Console.WriteLine($"Reading: {_file.FullName}");
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        ReadFile(e.FullPath);
    }
}