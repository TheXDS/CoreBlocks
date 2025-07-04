#if NET45
#pragma warning disable IDE0130
#pragma warning disable CS9113

namespace System.Runtime.CompilerServices;

/* Estas clases son necesarias para que el compilador de C# reconozca varias
 * características del lenguaje, como los miembros requeridos, los
 * registros y las propiedades init-only al compilar en .NET Framework 4.5.
 * Por favor, NO las elimines ni las utilices en tu propio código.
 */

internal static class IsExternalInit;

internal class RequiredMemberAttribute : Attribute;

internal class CompilerFeatureRequiredAttribute(string featureName) : Attribute;

#endif