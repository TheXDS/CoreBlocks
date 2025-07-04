namespace TheXDS.CoreBlocks;

/// <summary>
/// Define una serie de miembros a implementar por un tipo que permita dibujar
/// los elementos visuales del juego.
/// </summary>
public interface IGameDrawing : IBasicDrawing
{
    /// <summary>
    /// Borra el área de juego.
    /// </summary>
    void ClearWell();

    /// <summary>
    /// Dibuja la interfaz gráfica del juego.
    /// </summary>
    void DrawUI();

    /// <summary>
    /// Muestra un mensaje directamente sobre el área de juego.
    /// </summary>
    /// <param name="message"></param>
    void PrintMainMessage(in string message);

}
