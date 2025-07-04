using System;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Describe el estado actual del juego.
/// </summary>
public class GameState
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="GameState"/> que
    /// representa un estado vacío a partir de la configuración de juego
    /// provista.
    /// </summary>
    /// <param name="config">
    /// Configuración a utilizar para inicializar el estado del juego.
    /// </param>
    public GameState(GameConfig config)
    {
        Well = new byte?[config.WellWidth, config.WellHeight];
    }

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="GameState"/>,
    /// especificando el estado del área de juego activa.
    /// </summary>
    /// <param name="well">
    /// Área de juego activa a establecer. Debe tener el mismo tamaño que la
    /// configuración que se pretende usar.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Se produce si <paramref name="well"/> es <see langword="null"/>.
    /// </exception>
    public GameState(byte?[,] well)
    {
        Well = well ?? throw new ArgumentNullException(nameof(well));
    }

    /// <summary>
    /// Contiene el área de juego activa.
    /// </summary>
    public byte?[,] Well { get; private init; }
}
