namespace TheXDS.CoreBlocks;

/// <summary>
/// Contiene distintas definiciones de bloques de juego.
/// </summary>
public static class ShapeBlockSets
{
    /// <summary>
    /// Obtiene un arreglo de definiciones de bloques estándar.
    /// </summary>
    public static readonly ShapeDefinition[] Standard =
    [
        0b_1100_1100, // O, 0
        0b_1111_0000, // I, 1
        0b_0100_0111, // J, 2
        0b_0010_1110, // L, 3
        0b_1100_0110, // Z, 4
        0b_0110_1100, // S, 5
        new(0b_0100_1110, CenterPreTransform) // T, 6
    ];

    /// <summary>
    /// Obtiene un arreglo de definiciones de bloques extendido, que incluye los bloques estándar y algunos adicionales.
    /// </summary>
    public static readonly ShapeDefinition[] Extended =
    [
        .. Standard,
        0b_1001_1111, // C
        new(0b_0100_0110, CenterPreTransform), // r
        new(0b_1010_1110, CenterPreTransform), // u
        0b_0110_0000, // i
        0b_0010_0000, // .
    ];

    private static void CenterPreTransform(byte rotation, ref int x, ref int y)
    {
        switch (rotation)
        {
            case 1: x += 1; break;
            case 2: y += 1; break;
            case 3: y -= 1; break;
        }
    }
}
