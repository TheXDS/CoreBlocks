using System;
using System.Collections.Generic;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Define un modelo de configuración del juego.
/// </summary>
/// <param name="MaxLevel">Nivel máximo alcanzable.</param>
/// <param name="LevelTimerStep">Paso en milisegundos al subir de nivel.</param>
/// <param name="LinesPerLevel">Líneas requeridas para subir de nivel.</param>
/// <param name="PointsPerLine">Puntos a otorgar por cada línea completada.</param>
/// <param name="PointsPerTSpin">Puntos a otorgar por cada maniobra especial T-Spin.</param>
/// <param name="PointsPerBravo">Puntos a otorgar al vaciar por completo el área de juego.</param>
/// <param name="WellWidth">Ancho del área de juego.</param>
/// <param name="WellHeight">Alto del área de juego.</param>
/// <param name="AllowHold">Indica si se debe habilitar el área de Hold.</param>
/// <param name="ShapeSet">Set de figuras a utilizar.</param>
/// <param name="NextBlockSelector">Función selectora de siguiente figura.</param>
/// <param name="DefaultState">Estado predeterminado del juego.</param>
/// <param name="ExtraKeyBindings">Teclas de efecto especial a agregar al juego.</param>
public readonly partial record struct GameConfig(
    int MaxLevel,
    int LevelTimerStep,
    int LinesPerLevel,
    int PointsPerLine,
    int PointsPerTSpin,
    int PointsPerBravo,
    int WellWidth,
    int WellHeight,
    bool AllowHold,
    ShapeDefinition[] ShapeSet,
    Func<byte> NextBlockSelector,
    GameState? DefaultState = null,
    IEnumerable<KeyValuePair<ConsoleKey, Action<GameState>>>? ExtraKeyBindings = null)
{
    /// <summary>
    /// Indica la cantidad inicial de milisegundos para cada ciclo del juego en el primer nivel.
    /// </summary>
    public int LevelTimerStart => MaxLevel * LevelTimerStep;

    /// <summary>
    /// Obtiene una configuracíón de juego clásica, como la vista en las
    /// versiones origiales de Tetris(R).
    /// </summary>
    public static GameConfig Classic { get; } = new(
        MaxLevel: 29,
        LevelTimerStep: 75,
        LinesPerLevel: 20,
        PointsPerLine: 100,
        PointsPerTSpin: 0,
        PointsPerBravo: 0,
        WellWidth: 10,
        WellHeight: 24,
        AllowHold: false,
        ShapeSet: ShapeBlockSets.Standard,
        NextBlockSelector: DefaultNextBlockSelector(ShapeBlockSets.Standard));

    /// <summary>
    /// Obtiene una configuración de juego estándar.
    /// </summary>
    public static GameConfig Standard { get; } = Classic with
    { 
        AllowHold = true,
        PointsPerTSpin = 400,
        PointsPerBravo = 800,
    };

    /// <summary>
    /// Obtiene una configuración de juego estándar que incluye un set de
    /// bloques extendido.
    /// </summary>
    public static GameConfig Extended { get; } = Standard with
    {
        ShapeSet = ShapeBlockSets.Extended,
        NextBlockSelector = DefaultNextBlockSelector(ShapeBlockSets.Extended),
    };

    /// <summary>
    /// Obtiene una configuración de juego estándar con un área de juego con un
    /// patrón inicial.
    /// </summary>
    public static GameConfig HeadStart { get; } = Standard with
    {
        DefaultState = new GameState(new byte?[10, 24]
        {
            {null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null},
            {null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null},
            {null,null,null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
            {null,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8},
        })
    };

    /// <summary>
    /// Obtiene una configuración de juego estándar con un área de juego
    /// gigantesca.
    /// </summary>
    public static GameConfig Huge { get; } = Standard with
    {
        WellWidth = 20,
    };

    static readonly Random _rnd = new();

    private static Func<byte> DefaultNextBlockSelector(ShapeDefinition[] shapes)
    {
        return () => (byte)_rnd.Next(shapes.Length);
    }
}
