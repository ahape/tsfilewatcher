﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

namespace MyNamespace
{
    class MyClassCS
    {
        /// <summary>File containing a list of {absolute/path/to/source::absolute/path/to/dest} entries</summary>
        static IEnumerable<IEnumerable<string>> mappings;

        static ConcurrentDictionary<string, object> filesBeingWritten = new();

        static ConcurrentDictionary<string, FileSystemWatcher> watchers = new();

        static void Main()
        {
            if (!File.Exists("watchlist.txt"))
                throw new Exception("You need a watchlist.txt file");

            mappings = File.ReadAllText("watchlist.txt")
                .Split('\n')
                .Where(x => x.Contains("::"))
                .Select(x => x.Split("::").Select(y => y.Trim()));

            Console.WriteLine("Current mapping entries:");

            foreach (var entry in mappings)
            {
                Console.WriteLine("\t{0} -> {1}", entry.ElementAt(0), entry.ElementAt(1));

                var sourceInfo = new FileInfo(entry.ElementAt(0));

                if (!sourceInfo.Exists)
                {
                    Console.WriteLine("\t\t Warning: Source file not found--no watcher created");
                    continue;
                }

                if (!watchers.TryGetValue(sourceInfo.DirectoryName, out FileSystemWatcher watcher))
                {
                    watcher = new FileSystemWatcher(sourceInfo.DirectoryName);
                    watcher.NotifyFilter = NotifyFilters.LastWrite;
                    watcher.IncludeSubdirectories = false;
                    watcher.EnableRaisingEvents = true;
                    watcher.Changed += OnChanged;

                    watchers.TryAdd(sourceInfo.DirectoryName, watcher);
                }

                watcher.Filters.Add(sourceInfo.Name);
            }

            Console.WriteLine("Current watchers:");

            foreach (var key in watchers.Keys)
            {
                Console.WriteLine("\t" + key);
            }

            Console.WriteLine("\nPress enter to exit.");
            Console.ReadLine();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            Console.WriteLine($"Changed: {e.FullPath} on {DateTime.Now:s}");

            var watcher = (FileSystemWatcher)sender;

            var mappingEntry = mappings.FirstOrDefault(x => 
                Path.Combine(watcher.Path, x.ElementAt(0)) == e.FullPath);

            if (mappingEntry == null)
                return;
                // throw new Exception("Couldn't find mapping entry");

            var source = Path.Combine(watcher.Path, mappingEntry.ElementAt(0));
            var dest = mappingEntry.ElementAt(1);

            new Thread(() => 
            {
                if (!filesBeingWritten.ContainsKey(dest))
                {
                    object v = new object();
                    filesBeingWritten.TryAdd(dest, v);

                    File.Copy(source, dest, true);

                    Console.WriteLine("Copied entry\n\t{0} ->\n\t{1}", source, dest);

                    // Debounce if necessary
                    Thread.Sleep(1000);

                    filesBeingWritten.TryRemove(dest, out v);
                }
            }).Start();
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            string value = $"Created: {e.FullPath}";
            Console.WriteLine(value);
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e) =>
            Console.WriteLine($"Deleted: {e.FullPath}");

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"Renamed:");
            Console.WriteLine($"    Old: {e.OldFullPath}");
            Console.WriteLine($"    New: {e.FullPath}");
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}
