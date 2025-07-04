namespace TheXDS.CoreBlocks;

/// <summary>
/// Define un delegado a utilizar para realizar operaciones de
/// pre-transformación sobre una figura que será rotada.
/// </summary>
/// <param name="rotation">Rotación a ejecutar.</param>
/// <param name="x">Posición actual sobre el eje X.</param>
/// <param name="y">Posición actual sobre el eje Y.</param>
public delegate void ShapeRotationPreTransform(byte rotation, ref int x, ref int y);
