using MovieTheater.Application.Interfaces.Commons;

namespace MovieTheater.Application.Services.Commons;

public class LoggerService : ILoggerService
{
    public void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Logger Success] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Logger Error] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Logger Warn] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }

    public void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[Logger Info] - {DateTime.UtcNow.AddHours(7)} - " + msg);
        Console.ResetColor();
    }
}