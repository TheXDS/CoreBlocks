using System;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Clase base para el dibujado en consola.
/// </summary>
public class ConsoleBasicDrawing : IBasicDrawing
{
    /// <summary>
    /// Obtiene el deplazamiento horizontal predeterminado del área de juego.
    /// </summary>
    protected const int WellXOffset = 12;

    /// <summary>
    /// Obtiene el desplazamiento vertical predeterminado del área de juego.
    /// </summary>
    protected const int WellYOffset = 0;

    /// <summary>
    /// Obtiene una instancia de un objeto que se puede utilizar para
    /// sincronizar las operaciones de dibujo.
    /// </summary>
    protected static readonly object SyncLock = new();

    /// <summary>
    /// Borra un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    public void ClearBlock(in int x, in int y)
    {
        Console.SetCursorPosition(WellXOffset + (x * 2) + 1, WellYOffset + y + 1);
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
        Console.SetCursorPosition(WellXOffset + (x * 2) + 1, WellYOffset + y + 1);
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
        lock (SyncLock)
        {
            Console.SetCursorPosition(0, WellYOffset + line);
            Console.WriteLine(message);
        }
    }
}
