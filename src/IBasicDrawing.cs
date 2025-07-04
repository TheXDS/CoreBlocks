namespace TheXDS.CoreBlocks;

/// <summary>
/// Define una serie de miembros a implementar por un tipo que incluya
/// funciones básicas de dibujo.
/// </summary>
public interface IBasicDrawing
{
    /// <summary>
    /// Borra un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    void ClearBlock(in int x, in int y);

    /// <summary>
    /// Dibuja un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="block">Color del bloque a dibujar.</param>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    void DrawBlock(int block, int x, int y);

    /// <summary>
    /// Coloca un mensaje a la par del área de juego.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="line">Línea en la cual colocar el mensaje.</param>
    void PutMessage(string message, int line);

}