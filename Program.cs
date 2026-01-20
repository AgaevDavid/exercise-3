using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class EnumerableExtensions
{
    public static T GetMax<T>(this IEnumerable<T> collection, Func<T, float> selector)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        T maxItem = default(T);
        float maxValue = float.MinValue;
        bool hasItems = false;

        foreach (var item in collection)
        {
            if (item == null && default(T) == null) continue;

            float value = selector(item);
            if (!hasItems || value > maxValue)
            {
                maxValue = value;
                maxItem = item;
                hasItems = true;
            }
        }

        if (!hasItems) throw new InvalidOperationException("Коллекция пуста");
        return maxItem;
    }
}

public class FileFoundEventArgs : EventArgs
{
    public string FilePath { get; }
    public long FileSize { get; }
    public bool Cancel { get; set; }

    public FileFoundEventArgs(string filePath)
    {
        FilePath = filePath;
        FileSize = new FileInfo(filePath).Length;
    }
}

public class FileSearcher
{
    public event EventHandler<FileFoundEventArgs> FileFound;

    public List<string> Search(string directory, string pattern = "*", int maxFiles = 0)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Директория не найдена: {directory}");

        var files = Directory.GetFiles(directory, pattern);
        var result = new List<string>();
        int count = 0;

        foreach (var file in files)
        {
            var args = new FileFoundEventArgs(file);

            FileFound?.Invoke(this, args);

            if (args.Cancel) break;

            result.Add(file);
            count++;

            if (maxFiles > 0 && count >= maxFiles) break;
        }

        return result;
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("🔍 ПОИСК САМОГО БОЛЬШОГО ФАЙЛА");
        Console.WriteLine(new string('=', 50));

        string directoryPath = GetDirectoryPathFromUser();

        if (string.IsNullOrEmpty(directoryPath))
        {
            Console.WriteLine("Операция отменена пользователем.");
            return;
        }

        Console.WriteLine($"\n📁 Выбранная директория: {directoryPath}");
        Console.WriteLine(new string('-', 50));

        Console.Write("\nВведите маску поиска (например, *.txt, *.*) [*.*]: ");
        string pattern = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(pattern))
            pattern = "*.*";

        Console.Write("\nВведите максимальное количество файлов (0 - без ограничений) [0]: ");
        string maxFilesInput = Console.ReadLine();
        int maxFiles = 0;
        if (!int.TryParse(maxFilesInput, out maxFiles) || maxFiles < 0)
            maxFiles = 0;

        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("НАСТРОЙКИ ПОИСКА:");
        Console.WriteLine($"Директория: {directoryPath}");
        Console.WriteLine($"Маска: {pattern}");
        Console.WriteLine($"Лимит файлов: {(maxFiles == 0 ? "без ограничений" : maxFiles.ToString())}");
        Console.WriteLine(new string('=', 50));

        Console.WriteLine("\n🔍 Начинаю поиск файлов...");

        var searcher = new FileSearcher();
        var foundFiles = new List<FileInfo>();
        int fileCount = 0;

        searcher.FileFound += (sender, e) =>
        {
            fileCount++;
            var file = new FileInfo(e.FilePath);
            foundFiles.Add(file);

            if (fileCount % 10 == 0)
                Console.Write($"[{fileCount}] ");
            else
                Console.Write(".");

            if (maxFiles > 0 && fileCount >= maxFiles)
            {
                Console.WriteLine("\n⚠️ Достигнут лимит файлов!");
                e.Cancel = true;
            }
        };

        try
        {
            var files = searcher.Search(directoryPath, pattern, maxFiles);

            Console.WriteLine($"\n\n✅ ПОИСК ЗАВЕРШЕН!");
            Console.WriteLine($"Найдено файлов: {files.Count}");

            if (foundFiles.Count > 0)
            {
                Console.WriteLine("\n📊 СТАТИСТИКА:");
                Console.WriteLine($"Всего файлов: {foundFiles.Count}");
                Console.WriteLine($"Общий размер: {foundFiles.Sum(f => f.Length):N0} байт");
                Console.WriteLine($"Средний размер: {foundFiles.Average(f => f.Length):N0} байт");

                Console.WriteLine("\n🎯 ПРОВЕРЯЕМ САМЫЙ БОЛЬШОЙ ФАЙЛ...");
                var largestFile = foundFiles.GetMax(f => (float)f.Length);

                Console.WriteLine("\n" + new string('═', 60));
                Console.WriteLine("РЕЗУЛЬТАТ ПОИСКА МАКСИМАЛЬНОГО ФАЙЛА:");
                Console.WriteLine(new string('═', 60));
                Console.WriteLine($"Файл: {largestFile.Name}");
                Console.WriteLine($"Путь: {largestFile.FullName}");
                Console.WriteLine($"Размер: {largestFile.Length:N0} байт ({largestFile.Length / 1024.0 / 1024.0:F2} MB)");
                Console.WriteLine($"Создан: {largestFile.CreationTime:dd.MM.yyyy HH:mm}");
                Console.WriteLine($"Изменен: {largestFile.LastWriteTime:dd.MM.yyyy HH:mm}");
                Console.WriteLine(new string('═', 60));

                if (foundFiles.Count > 1)
                {
                    Console.WriteLine("\n🏆 ТОП-5 САМЫХ БОЛЬШИХ ФАЙЛОВ:");
                    var topFiles = foundFiles.OrderByDescending(f => f.Length).Take(5).ToList();

                    for (int i = 0; i < topFiles.Count; i++)
                    {
                        var file = topFiles[i];
                        string size = file.Length > 1024 * 1024
                            ? $"{(file.Length / 1024.0 / 1024.0):F2} MB"
                            : $"{(file.Length / 1024.0):F2} KB";

                        Console.WriteLine($"{i + 1}. {file.Name} ({size})");
                    }
                }
            }
            else
            {
                Console.WriteLine("\n⚠️ Файлы по заданным критериям не найдены.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}");
            Console.WriteLine($"Тип ошибки: {ex.GetType().Name}");
        }

        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("Готово! Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static string GetDirectoryPathFromUser()
    {
        while (true)
        {
            Console.WriteLine("\nВВЕДИТЕ ПУТЬ К ДИРЕКТОРИИ ДЛЯ ПОИСКА:");
            Console.WriteLine("(Для выхода оставьте поле пустым и нажмите Enter)");
            Console.Write(">>> ");

            string path = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(path))
                return null;

            if (Directory.Exists(path))
                return path;

            Console.WriteLine($"\n❌ Директория '{path}' не существует.");

            if (path.Length == 2 && path[1] == ':')
            {
                string drive = path + "\\";
                if (!Directory.Exists(drive))
                {
                    Console.WriteLine($"Диск {path} не найден или не доступен.");
                }
            }

            Console.WriteLine("\nПримеры правильных путей:");
            Console.WriteLine($"• Текущая директория: {Environment.CurrentDirectory}");
            Console.WriteLine("• C:\\Users\\Имя\\Documents");
            Console.WriteLine("• D:\\Work\\Projects");

            Console.Write("\nПопробовать снова? (Y/N) [Y]: ");
            string answer = Console.ReadLine()?.Trim().ToUpper();
            if (answer == "N" || answer == "Н")
                return null;

            Console.Clear();
            Console.WriteLine("🔍 ПОИСК САМОГО БОЛЬШОГО ФАЙЛА");
            Console.WriteLine(new string('=', 50));
        }
    }
}
