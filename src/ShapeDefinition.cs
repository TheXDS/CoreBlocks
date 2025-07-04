namespace TheXDS.CoreBlocks;

/// <summary>
/// Representa una definición de figura, que incluye los bits que la conforman
/// así como una función de pre-transformación opcional que ajusta el eje de
/// rotación de la figura.
/// </summary>
/// <param name="ShapeBits">Bits de la figura en una matrix de 4x2.</param>
/// <param name="CustomPreRotationTransform">
/// Función de pre-transformación de rotación. Puede omitirse o estabecerse en
/// <see langword="null"/> para indicar que no se requiere ajustar el eje de
/// rotación.
/// </param>
public readonly record struct ShapeDefinition(byte ShapeBits, ShapeRotationPreTransform? CustomPreRotationTransform = null)
{
    /// <summary>
    /// Convierte implícitamente un <see cref="byte"/> en un
    /// <see cref="ShapeDefinition"/> que no requiere de una función de
    /// pre-transformación.
    /// </summary>
    /// <param name="shapeBits">
    /// <see cref="byte"/> que describe los bits de la figura.
    /// </param>
    public static implicit operator ShapeDefinition(byte shapeBits) => new(shapeBits, null);
}