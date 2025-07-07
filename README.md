![CoreBlocks](https://github.com/TheXDS/CoreBlocks/raw/master/Docs/TitleScreen.png)
# CoreBlocks
[![MIT](https://img.shields.io/github/license/TheXDS/Triton)](https://mit-license.org)

CoreBlocks es un clon del famoso juego de figuras, Tetris®. Fue creado con fines experimentales y de entretenimiento personal.

![CoreBlocks screenshot](https://github.com/TheXDS/CoreBlocks/raw/master/Docs/Screenshot.png)

## Compilación
CoreBlocks requiere de un compilador compatible con C# 12, debido a ciertas
características especiales del lenguaje que ayudan a disminuir la
complejidad del código. CoreBlocks también requiere que [.Net SDK 8.0](https://dotnet.microsoft.com/) o un targeting pack para el mismo esté instalado en el sistema.

Se han definidos destinos para .NET 8.0 para plataformas modernas, y una experimental para .Net Framework 4.5 lo que, permitiría ejecutar CoreBlocks en cualquier plataforma que soporte este Framework.

### Compilando CoreBlocks
```sh
dotnet build ./src/CoreBlocks.sln
```
Los binarios se encontarán en la carpeta `./Build` en la raíz del repositorio.

### Compatibilidad con .Net Framework 4.5
Tal como está descrito arriba, uno de los objetivos de compilación de CoreBlocks es .Net Framework 4.5. Para lograrlo, fue necesario proveer de ciertas clases que el compilador requiere por medio de "Stubs", definidos en la carpeta Net45Compat.

Estos "Stubs" permiten que una app con algunas características de .NET 8 y C# 12 se puedan compilar para .Net 4.5, como ser propiedades con accesor de inicialización, propiedades requeridas y `record`s; además de ciertos métodos existentes en los diccionarios que no se encentran presentes en .Net 4.5.

Estas extensiones sirven como una base primitiva, y es posible que no sean suficientes en lo absoluto para compilar aplicaciones más grandes que utilizan más características de C# 12.

## Contribuir
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W415UCHY)

Si CoreBlocks te ha sido de utilidad, o te interesa donar para fomentar el
desarrollo del proyecto, siéntete libre de hacer una donación por medio de
[PayPal](https://paypal.me/thexds), [Ko-fi](https://ko-fi.com/W7W415UCHY)
o ponte en contacto directamente conmigo.

Lamentablemente, no puedo ofrecer otros medios de donación por el momento
debido a que mi país (Honduras) no es soportado por casi ninguna plataforma.