using System;
using System.Linq;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Implementa funciones de dibujado del juego sobre la ventana de la consola.
/// </summary>
public class ConsoleGameDrawing(GameConfig config) : IGameDrawing
{
    private const int _wellXOffset = 12;
    private const int _wellYOffset = 0;
    private static readonly object _syncLock = new();

    /// <summary>
    /// Obtiene la configuración del juego.
    /// </summary>
    public GameConfig Config { get; } = config;

    /// <summary>
    /// Dibuja la interfaz gráfica del juego.
    /// </summary>
    public void DrawUI()
    {
        Console.SetCursorPosition(_wellXOffset, _wellYOffset);
        Console.Write($"+{new string(' ', Config.WellWidth * 2)}+");
        for (var j = 1; j <= Config.WellHeight; j++)
        {
            Console.SetCursorPosition(_wellXOffset, _wellYOffset + j);
            Console.Write("|");
            Console.SetCursorPosition(_wellXOffset + Config.WellWidth * 2 + 1, _wellYOffset + j);
            Console.Write("|");
        }
        Console.SetCursorPosition(_wellXOffset, _wellYOffset + Config.WellHeight + 1);
        Console.Write($"+{new string('-', Config.WellWidth * 2)}+");
        Console.SetCursorPosition(_wellXOffset + Config.WellWidth * 2 + 4, _wellYOffset);
        Console.Write("Siguiente:");

        if (Config.AllowHold)
        {
            Console.SetCursorPosition(_wellXOffset - 10, _wellYOffset);
            Console.Write("Hold:");
        }
    }

    /// <summary>
    /// Borra un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    public void ClearBlock(in int x, in int y)
    {
        Console.SetCursorPosition(_wellXOffset + (x * 2) + 1, _wellYOffset + y + 1);
        Console.ResetColor();
        Console.Write("  ");
    }

    /// <summary>
    /// Dibuja un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="block">Color del bloque a dibujar.</param>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    public void DrawBlock(int block, int x, int y)
    {
        Console.SetCursorPosition(_wellXOffset + (x * 2) + 1, _wellYOffset + y + 1);
        Console.BackgroundColor = (ConsoleColor)((block + 1) % 16);
        Console.Write("[]");
        Console.ResetColor();
    }

    /// <summary>
    /// Coloca un mensaje a la par del área de juego.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="line">Línea en la cual colocar el mensaje.</param>
    public void PutMessage(string message, int line)
    {
        lock (_syncLock)
        {
            Console.SetCursorPosition(0, _wellYOffset + line);
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Muestra un mensaje directamente sobre el área de juego.
    /// </summary>
    /// <param name="message"></param>
    public void PrintMainMessage(in string message)
    {
        var lines = message.Split('\n');
        var width = _wellXOffset + (Config.WellWidth / 2) - 1;
        var height = _wellYOffset + (Config.WellHeight / 2) - (lines.Length / 2);

        lock (_syncLock)
        {
            foreach (var j in lines)
            {
                Console.SetCursorPosition(width + ((lines.Max(p => p.Length) - j.Length) / 2), ++height);
                Console.Write(j);
            }
        }
    }

    /// <summary>
    /// Borra el área de juego.
    /// </summary>
    public void ClearWell()
    {
        for (byte j = 0; j < Config.WellWidth; j++)
        {
            for (byte k = 0; k < Config.WellHeight; k++)
            {
                ClearBlock(j, k);
            }
        }
    }
}
