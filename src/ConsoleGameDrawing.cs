using System;
using System.Linq;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Implementa funciones de dibujado del juego sobre la ventana de la consola.
/// </summary>
public class ConsoleGameDrawing(GameConfig config) : ConsoleBasicDrawing, IGameDrawing
{
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
        Console.SetCursorPosition(WellXOffset, WellYOffset);
        Console.Write($"+{new string(' ', Config.WellWidth * 2)}+");
        for (var j = 1; j <= Config.WellHeight; j++)
        {
            Console.SetCursorPosition(WellXOffset, WellYOffset + j);
            Console.Write("|");
            Console.SetCursorPosition(WellXOffset + Config.WellWidth * 2 + 1, WellYOffset + j);
            Console.Write("|");
        }
        Console.SetCursorPosition(WellXOffset, WellYOffset + Config.WellHeight + 1);
        Console.Write($"+{new string('-', Config.WellWidth * 2)}+");
        Console.SetCursorPosition(WellXOffset + Config.WellWidth * 2 + 4, WellYOffset);
        Console.Write("Siguiente:");

        if (Config.AllowHold)
        {
            Console.SetCursorPosition(WellXOffset - 10, WellYOffset);
            Console.Write("Hold:");
        }
    }

    /// <summary>
    /// Muestra un mensaje directamente sobre el área de juego.
    /// </summary>
    /// <param name="message"></param>
    public void PrintMainMessage(in string message)
    {
        var lines = message.Split('\n');
        var width = WellXOffset + (Config.WellWidth / 2) - 1;
        var height = WellYOffset + (Config.WellHeight / 2) - (lines.Length / 2);

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
