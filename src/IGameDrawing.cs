namespace TheXDS.CoreBlocks;

/// <summary>
/// Define una serie de miembros a implementar por un tipo que permita dibujar
/// los elementos visuales del juego.
/// </summary>
public interface IGameDrawing
{
    /// <summary>
    /// Borra un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    void ClearBlock(in int x, in int y);

    /// <summary>
    /// Borra el área de juego.
    /// </summary>
    void ClearWell();

    /// <summary>
    /// Dibuja un bloque en las coordenadas correspondientes del juego.
    /// </summary>
    /// <param name="block">Color del bloque a dibujar.</param>
    /// <param name="x">Posición X del bloque.</param>
    /// <param name="y">Posición Y del bloque.</param>
    void DrawBlock(int block, int x, int y);

    /// <summary>
    /// Dibuja la interfaz gráfica del juego.
    /// </summary>
    void DrawUI();

    /// <summary>
    /// Muestra un mensaje directamente sobre el área de juego.
    /// </summary>
    /// <param name="message"></param>
    void PrintMainMessage(in string message);

    /// <summary>
    /// Coloca un mensaje a la par del área de juego.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="line">Línea en la cual colocar el mensaje.</param>
    void PutMessage(string message, int line);
}