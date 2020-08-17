//
// GameField.cs
//
// Author:
//       César Andrés Morgan <xds_xps_ivx@hotmail.com>
//
// Copyright (c) 2020 César Andrés Morgan
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TheXDS.CoreBlocks
{
    /// <summary>
    /// Define una instancia del área de juego.
    /// </summary>
    internal class GameField
    {
        #region Configuración

        /// <summary>
        /// Define el ancho del área de juego.
        /// </summary>
        private const int _wellWidth = 10;

        /// <summary>
        /// Define el alto del área de juego.
        /// </summary>
        private const int _wellHeight = 24;

        /// <summary>
        /// Offset posicional X del área de juego.
        /// </summary>
        private const int _wellXOffset = 12;

        /// <summary>
        /// Offset posicional Y del área de juego.
        /// </summary>
        private const int _wellYOffset = 0;

        #endregion

        #region Objetos misceláneos

        /// <summary>
        /// Generador de números aleatorios del juego.
        /// </summary>
        private static readonly Random _rnd = new Random();

        /// <summary>
        /// Ayuda a sincronizar los hilos de dibujado y tareas que deben
        /// ejecutarse como una transacción única.
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// Contiene una colección de las teclas configuradas para controlar el
        /// juego.
        /// </summary>
        private readonly Dictionary<ConsoleKey, Action> _keyBindings = new Dictionary<ConsoleKey, Action>();

        #endregion

        #region Objetos de estado del juego

        /// <summary>
        /// Define la colección de figuras disponibles en el juego.
        /// </summary>
        private static readonly byte[] Shapes =
        {
            0b_1100_1100, // O
            0b_1111_0000, // I
            0b_0100_0111, // J
            0b_0010_1110, // L
            0b_1100_0110, // Z
            0b_0110_1100, // S
            0b_0100_1110, // T
        };

        /// <summary>
        /// Contiene el área de juego activa.
        /// </summary>
        private readonly byte?[,] _well = new byte?[_wellWidth, _wellHeight];

        /// <summary>
        /// Tóken de control de pausa del juego.
        /// </summary>
        private readonly PauseTokenSource _pauseSource = new PauseTokenSource();

        /// <summary>
        /// Interruptor del ciclo principal de figuras. Permite detener el
        /// tiempo de espera del nivel para tomar acciones inmediatas.
        /// </summary>
        private TaskCompletionSource<bool> _shapeBreaker = new TaskCompletionSource<bool>();

        /// <summary>
        /// Rotación actual de la pieza.
        /// </summary>
        private byte _r = 0;

        /// <summary>
        /// Posición X actual de la pieza.
        /// </summary>
        private int _px;

        /// <summary>
        /// Posición Y actual de la pieza.
        /// </summary>
        private int _py;

        /// <summary>
        /// Pieza activa.
        /// </summary>
        private byte _shape;

        /// <summary>
        /// Siguiente figura
        /// </summary>
        private byte _nextShape;

        /// <summary>
        /// Pieza almacenada en el Hold.
        /// </summary>
        private byte? _hold = null;

        /// <summary>
        /// Estado actual del Hold.
        /// </summary>
        private bool _holdUsed = false;

        /// <summary>
        /// Combo alcanzado.
        /// </summary>
        private int _combo;

        /// <summary>
        /// Obtiene o establece el nivel de juego activo.
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Obtiene el puntaje actual del juego.
        /// </summary>
        public int Score { get; private set; } = 0;

        /// <summary>
        /// Obtiene la cantidad actual de líneas hechas en el juego.
        /// </summary>
        public int Lines { get; private set; }

        /// <summary>
        /// Obtiene o establece un valor que indica si el juego continúa.
        /// </summary>
        public bool KeepPlaying { get; set; } = true;

        #endregion

        #region Métodos de dibujo básicos

        /// <summary>
        /// Dibuja toda el área de juego.
        /// </summary>
        private void DrawWell()
        {
            for (byte j = 0; j < _wellWidth; j++)
            {
                for (byte k = 0; k < _wellHeight; k++)
                {
                    if (!_well[j, k].HasValue) ClearBlock(j, k);
                    else DrawBlock(_well[j, k]!.Value, j, k);
                }
            }
        }

        /// <summary>
        /// Borra el área de juego.
        /// </summary>
        private void ClearWell()
        {
            for (byte j = 0; j < _wellWidth; j++)
            {
                for (byte k = 0; k < _wellHeight; k++)
                {
                    ClearBlock(j, k);
                }
            }
        }

        /// <summary>
        /// Dibuja la interfaz gráfica del juego.
        /// </summary>
        private void DrawUI()
        {
            Console.SetCursorPosition(_wellXOffset, _wellYOffset);
            Console.Write($"+{new string('-', _wellWidth * 2)}+");
            for (var j = 1; j <= _wellHeight; j++)
            {
                Console.SetCursorPosition(_wellXOffset, _wellYOffset + j);
                Console.Write("|");
                Console.SetCursorPosition(_wellXOffset + _wellWidth * 2 + 1, _wellYOffset + j);
                Console.Write("|");
            }
            Console.SetCursorPosition(_wellXOffset, _wellYOffset + _wellHeight + 1);
            Console.Write($"+{new string('-', _wellWidth * 2)}+");
            Console.SetCursorPosition(_wellXOffset + _wellWidth * 2 + 4, _wellYOffset);
            Console.Write("Siguiente:");
            Console.SetCursorPosition(_wellXOffset - 10, _wellYOffset);
            Console.Write("Hold:");
        }

        /// <summary>
        /// Dibuja un bloque en las coordenadas correspondientes del juego.
        /// </summary>
        /// <param name="block">Color del bloque a dibujar.</param>
        /// <param name="x">Posición X del bloque.</param>
        /// <param name="y">Posición Y del bloque.</param>
        private void DrawBlock(int block, int x, int y)
        {
            Console.SetCursorPosition(_wellXOffset + (x * 2) + 1, _wellYOffset + y + 1);
            Console.BackgroundColor = (ConsoleColor)((block + 1) % 16);
            Console.Write("[]");
            Console.ResetColor();
        }

        /// <summary>
        /// Borra un bloque en las coordenadas correspondientes del juego.
        /// </summary>
        /// <param name="x">Posición X del bloque.</param>
        /// <param name="y">Posición Y del bloque.</param>
        private void ClearBlock(int x, int y)
        {
            Console.SetCursorPosition(_wellXOffset + (x * 2) + 1, _wellYOffset + y + 1);
            Console.ResetColor();
            Console.Write("  ");
        }

        /// <summary>
        /// Dibuja la figura activa en su posición actual.
        /// </summary>
        private void DrawShape()
        {
            DrawShape(_shape, _px, _py, _r);
        }

        /// <summary>
        /// Dibuja una figura especificada en las coordenadas.
        /// </summary>
        /// <param name="shape">Figura a dibujar.</param>
        /// <param name="x">Coordenada X en la cual dibujar la figura.</param>
        /// <param name="y">Coordenada Y en la cual dibujar la figura.</param>
        /// <param name="rotation">
        /// Valor de rotación con el cual transformar la figura para dibujarla.
        /// </param>
        private void DrawShape(in byte shape, in int x, in int y, in byte rotation)
        {
            TransformRotate(shape, x, y, rotation, DrawBlock);
        }

        /// <summary>
        /// Dibuja la sombra de la figura actual.
        /// </summary>
        private void DrawShadow()
        {
            var bottom = CalcBottom();
            if (bottom > _py)
            {
                TransformRotate(_shape, _px, bottom, _r, (_, px, py) => DrawBlock(-1, px, py));
            }
        }

        /// <summary>
        /// Borra la figura activa de la pantalla.
        /// </summary>
        private void ClearShape()
        {
            ClearShape(_shape, _px, _py, _r);
        }

        /// <summary>
        /// Borra la figura dibujada previamente en las coordenadas y con la
        /// transformación de rotación especificadas.
        /// </summary>
        /// <param name="shape">Figura a borrar.</param>
        /// <param name="x">Coordenada X en la cual borrar la figura.</param>
        /// <param name="y">Coordenada Y en la cual borrar la figura.</param>
        /// <param name="rotation">
        /// Valor que indica el valor de rotación con el cual se dibujó la
        /// figura a borrar.
        /// </param>
        private void ClearShape(in byte shape, in int x, in int y, byte rotation)
        {
            TransformRotate(shape, x, y, rotation, (_, px, py) => ClearBlock(px, py));
        }

        /// <summary>
        /// Borra la sombra de la figura activa.
        /// </summary>
        private void ClearShadow()
        {
            var bottom = CalcBottom();
            if (bottom > _py)
            {
                TransformRotate(_shape, _px, bottom, _r, (_, px, py) => ClearBlock(px, py));
            }
        }

        #endregion

        #region Métodos internos del juego

        /// <summary>
        /// Aplica una transformación con rotación de los bits físicos de una
        /// figura, y ejecuta una acción con los bits establecidos de la misma.
        /// </summary>
        /// <param name="shape">Figura a transformar.</param>
        /// <param name="x">Coordenada lógica X.</param>
        /// <param name="y">Coordenada lógica Y.</param>
        /// <param name="r">Rotación de figura.</param>
        /// <param name="action">
        /// Acción a ejecutar sobre los bits de la figura.
        /// </param>
        private void TransformRotate(in byte shape, in int x, in int y, in byte r, Action<int, int, int> action)
        {
            TransformRotate(shape, x, y, r, (shape, nx, ny) => { action(shape, nx, ny); return true; });
        }

        /// <summary>
        /// Aplica una transformación con rotación de los bits físicos de una
        /// figura, y ejecuta una acción con los bits establecidos de la misma.
        /// </summary>
        /// <param name="shape">Figura a transformar.</param>
        /// <param name="x">Coordenada lógica X.</param>
        /// <param name="y">Coordenada lógica Y.</param>
        /// <param name="r">Rotación de figura.</param>
        /// <param name="function">
        /// Función a ejecutar sobre los bits de la figura. La función retornará
        /// <see langword="true"/> si la iteración sobre los bits puede
        /// continuar, <see langword="false"/> para detener inmediatamente la
        /// iteración.
        /// </param>
        private void TransformRotate(in byte shape, in int x, in int y, in byte r, Func<int, int, int, bool> function)
        {
            var shapeData = Shapes[shape];
            for (byte j = 0; j < 8; j++)
            {
                if (((shapeData << j) & 128) != 0)
                {
                    (int px, int py) = (r % 4) switch
                    {
                        0 => (x + (j % 4), y + (j / 4)),
                        1 => (x + (1 - (j / 4)), y + (j % 4)),
                        2 => (x + (2 - (j % 4)), y + (1 - (j / 4))),
                        3 => (x + (j / 4), y + (3 - (j % 4))),
                        _ => throw new InvalidOperationException()
                    };
                    if (!function(shape, px, py)) return;
                }
            }
        }

        /// <summary>
        /// Actualiza el estado de la figura activa.
        /// </summary>
        /// <param name="relX">Nueva posición X relativa.</param>
        /// <param name="relY">Nueva posición Y relativa.</param>
        /// <param name="relR">Nueva rotación relativa.</param>
        private bool UpdateShape(int relX, int relY, int relR)
        {
            if (Fits(relX, relY, relR) != true) return false;
            lock (_syncLock)
            {
                ClearShadow();
                ClearShape();
                _px += relX;
                _py += relY;
                _r = (byte)((_r + relR) % (_shape switch { 0 => 1, 1 => 2, _ => 4 }));
                DrawShadow();
                DrawShape();
            }
            return true;
        }

        /// <summary>
        /// Establece la figura activa a la figura especificada, reinicializando
        /// su posición y rotación.
        /// </summary>
        /// <param name="newShape">
        /// Nueva figura a establecer como la activa.
        /// </param>
        private void SelectNextShape(byte newShape)
        {
            _shape = newShape;
            _r = 0;
            _px = (_wellWidth - 2) / 2;
            _py = -1;
        }

        /// <summary>
        /// Establece la figura activa como la figura en la cola de "Siguiente".
        /// </summary>
        private void SelectNextShape()
        {
            SelectNextShape(_nextShape);
            TransformRotate(_nextShape, _wellWidth + 2, 2, 0, (_, px, py) => ClearBlock(px, py));
            _nextShape = (byte)_rnd.Next(Shapes.Length);
            TransformRotate(_nextShape, _wellWidth + 2, 2, 0, DrawBlock);
        }

        /// <summary>
        /// Al finalizar un ciclo de pieza activa, comprueba la completación de
        /// líneas.
        /// </summary>
        private void CheckLines()
        {
            var lines = 0;
            for (var j = 0; j < _wellHeight; j++)
            {
                var k = 0;
                for (; k < _wellWidth; k++)
                {
                    if (!_well[k, j].HasValue) break;
                }
                if (k == _wellWidth)
                {
                    lines++;
                    for (var l = j; l > 0; l--)
                    {
                        for (k = 0; k < _wellWidth; k++)
                        {
                            _well[k, l] = _well[k, l - 1];
                        }
                    }
                    for (k = 0; k < _wellWidth; k++)
                    {
                        _well[k, 0] = null;
                    }
                }
            }
            if (lines > 0)
            {
                Lines += lines;
                _combo++;
                CheckScore(lines);
                DrawWell();
            }
            else
            {
                _combo = 0;
            }
        }

        /// <summary>
        /// Ejecuta comprobaciones del puntaje actual, y aumenta el nivel de
        /// ser necesario.
        /// </summary>
        /// <param name="lines">
        /// Líneas completadas por el jugador en la acción actual.
        /// </param>
        private void CheckScore(int lines)
        {
            PrintMessage(lines == 1 ? "1 Línea" : $"{lines} Líneas!", 8);
            if (_combo > 1) PrintMessage($"(x{_combo} Combo)", 9);            
            Score += lines * 100 * _combo;
            PrintMessage($"Líneas {Lines}", 11);
            PrintMessage($"Puntaje {Score}", 12);

            if (Lines > (Level * 10))
            {
                PrintMessage($"Nivel {++Level}", 14);
            }
        }

        /// <summary>
        /// Establece los bits lógicos del área de juego a la figura (color)
        /// especificada(o).
        /// </summary>
        /// <param name="shape">
        /// Figura a establecer. Se utiliza el color asignado de la misma.
        /// </param>
        /// <param name="x">Posición lógica X del bloque a establecer.</param>
        /// <param name="y">Posición lógica Y del bloque a establecer.</param>
        private void SetWellBits(int shape, int x, int y)
        {
            _well[x, y] = (byte)shape;
        }

        /// <summary>
        /// Comprueba si la pieza activa puede ocupar el nuevo espacio.
        /// </summary>
        /// <param name="relX">Nueva posición relativa X.</param>
        /// <param name="relY">Nueva posición relativa Y</param>
        /// <param name="relR">Nueva rotación a aplicar.</param>
        /// <returns>
        /// <see langword="true"/> si la pieza activa cabrá en el nuevo
        /// espacio, <see langword="false"/> si es bloqueada por una pieza
        /// existente dentro del juego o al llegar al fondo, o 
        /// <see langword="null"/> si excede los límites del juego.
        /// </returns>
        private bool? Fits(int relX, int relY, int relR)
        {
            lock (_syncLock)
            {
                bool? retval = true;
                TransformRotate(_shape, _px + relX, _py + relY, (byte)(_r + relR), (_, px, py) =>
                {
                    if (px < 0 || px >= _wellWidth) { retval = null; return false; }
                    if (py < 0 || py >= _wellHeight) { retval = false; return false; }
                    if (_well[px, py].HasValue) { retval = false; return false; }
                    return true;
                });
                return retval;
            }
        }

        /// <summary>
        /// Calcula la posición más baja que puede ocupar la pieza activa en el
        /// juego.
        /// </summary>
        /// <returns>
        /// La posición lógica Y más maja que la pieza activa puede ocupar
        /// actualmente.
        /// </returns>
        private int CalcBottom()
        {
            lock (_syncLock)
            {
                var ny = 0;
                while (Fits(0, ++ny, 0) == true) ;
                return _py + ny - 1;
            }
        }

        /// <summary>
        /// Muestra un mensaje que desaparecerá luego de 3000 ms al jugador.
        /// </summary>
        /// <param name="message">Mensaje a mostrar.</param>
        /// <param name="line">Línea en la cual colocar el mensaje.</param>
        private async void PrintMessage(string message, int line = 1)
        {
            lock (_syncLock)
            {
                Console.SetCursorPosition(0, _wellYOffset + line);
                Console.WriteLine(message);
            }
            await Task.Delay(3000);
            lock (_syncLock)
            {
                Console.SetCursorPosition(0, _wellYOffset + line);
                Console.WriteLine(new string(' ', message.Length));
            }
        }

        /// <summary>
        /// Intenta ejecutar una rotación en una dirección en particular.
        /// </summary>
        /// <param name="direction">Dirección de rotación.</param>
        private void Rotate(int direction)
        {
            _ = UpdateShape(0, 0, direction) ||
                UpdateShape(-1, 0, direction) || // Probar a la izquierda...
                UpdateShape(1, 0, direction) ||  // Probar a la derecha...
                UpdateShape(0, 1, direction);    // Finalmente, probar arriba...
        }

        #endregion

        #region Hilos de ejecución

        /// <summary>
        /// Hilo que maneja la entrada de controles del juego.
        /// </summary>
        private void HandleInput()
        {
            while (KeepPlaying)
            {
                if (_keyBindings.TryGetValue(Console.ReadKey(true).Key, out var action)) action.Invoke();                
            }
        }

        /// <summary>
        /// Hilo que controla la aparición de figuras en el juego.
        /// </summary>
        private async Task ShapeLoopAsync()
        {
            _nextShape = (byte)_rnd.Next(Shapes.Length);
            while (KeepPlaying)
            {
                SelectNextShape();
                while (Fits(0, 1, 0) == true)
                {
                    await _pauseSource.WaitWhilePausedAsync();
                    UpdateShape(0, 1, 0);
                    if (_shapeBreaker.Task == await Task.WhenAny(Task.Delay(1000 / Level), _shapeBreaker.Task))
                    {
                        _shapeBreaker = new TaskCompletionSource<bool>();
                        continue;
                    }                 
                }
                if (_py < 0) break;
                _holdUsed = false;
                TransformRotate(_shape, _px, _py, _r, SetWellBits);
                CheckLines();
            }
            KeepPlaying = false;
            PrintMessage("Fin del juego.");
        }

        #endregion

        #region Acciones de control del juego

        private void MoveLeft() => UpdateShape(-1, 0, 0);
        private void MoveRight() => UpdateShape(1, 0, 0);
        private void RotateCw() => Rotate(1);
        private void RotateCcw() => Rotate(-1);
        private void SoftDrop() => UpdateShape(0, 1, 0);
        private void HardDrop()
        {
            UpdateShape(0, CalcBottom() - _py, 0);
            _shapeBreaker.SetResult(true);
        }

        private void HoldCurrent()
        {
            if (_holdUsed) return;
            _holdUsed = true;

            ClearShape();
            ClearShadow();

            if (!_hold.HasValue)
            {
                _hold = _shape;
                SelectNextShape();
            }
            else
            {
                ClearShape(_hold.Value, -6, 2, 0);
                var tmphold = _shape;
                SelectNextShape(_hold.Value);
                _hold = tmphold;
            }
            DrawShape(_hold.Value, -6, 2, 0);
            _shapeBreaker.SetResult(true);
        }
        private void TogglePause()
        {
            if (_pauseSource.IsPaused)
            {
                DrawWell();
                _pauseSource.IsPaused = false;
            }
            else
            {
                _pauseSource.IsPaused = true;
                ClearWell();
                const string pauseMsg = "Juego en pausa";
                Console.SetCursorPosition(_wellXOffset + 4, _wellYOffset + (_wellHeight / 2));
                Console.Write(pauseMsg);
            }
            
        }

        #endregion

        /// <summary>
        /// Punto de entrada principal del ciclo del juego.
        /// </summary>
        /// <returns>
        /// Una tarea que finaliza cuando el jugador pierde o se retira.
        /// </returns>
        public Task PlayAsync()
        {
            DrawUI();

            _keyBindings.Add(ConsoleKey.LeftArrow, MoveLeft);
            _keyBindings.Add(ConsoleKey.A, MoveLeft);
            _keyBindings.Add(ConsoleKey.NumPad4, MoveLeft);
            _keyBindings.Add(ConsoleKey.RightArrow, MoveRight);
            _keyBindings.Add(ConsoleKey.D, MoveRight);
            _keyBindings.Add(ConsoleKey.NumPad6, MoveRight);
            _keyBindings.Add(ConsoleKey.UpArrow, RotateCw);
            _keyBindings.Add(ConsoleKey.W, RotateCw);
            _keyBindings.Add(ConsoleKey.NumPad9, RotateCw);
            _keyBindings.Add(ConsoleKey.NumPad8, RotateCw);
            _keyBindings.Add(ConsoleKey.NumPad7, RotateCcw);
            _keyBindings.Add(ConsoleKey.DownArrow, SoftDrop);
            _keyBindings.Add(ConsoleKey.S, SoftDrop);
            _keyBindings.Add(ConsoleKey.NumPad5, SoftDrop);
            _keyBindings.Add(ConsoleKey.Spacebar, HardDrop);
            _keyBindings.Add(ConsoleKey.NumPad2, HardDrop);
            _keyBindings.Add(ConsoleKey.C, HoldCurrent);
            _keyBindings.Add(ConsoleKey.NumPad0, HoldCurrent);
            _keyBindings.Add(ConsoleKey.Pause, TogglePause);
            _keyBindings.Add(ConsoleKey.P, TogglePause);

            return Task.WhenAll(
                ShapeLoopAsync(),
                Task.Run(HandleInput)
            );
        }
    }


    //https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/
    public class PauseTokenSource
    {
        public bool IsPaused
        {
            get => m_paused != null;
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref m_paused, new TaskCompletionSource<bool>(), null);
                }
                else
                {
                    while (true)
                    {
                        var tcs = m_paused;
                        if (tcs is null) return;
                        if (Interlocked.CompareExchange(ref m_paused, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }

        public PauseToken Token => new PauseToken(this);

        private volatile TaskCompletionSource<bool>? m_paused;

        internal Task WaitWhilePausedAsync()
        {
            var cur = m_paused;
            return cur != null ? cur.Task : Completed;
        }

        private static readonly Task<bool> Completed = Task.FromResult(true);
    }

    public struct PauseToken
    {
        private readonly PauseTokenSource? m_source;

        internal PauseToken(PauseTokenSource source)
        { 
            m_source = source;
        }

        public bool IsPaused => m_source?.IsPaused ?? false;

        public Task WaitWhilePausedAsync()
        {
            return IsPaused ?
                m_source!.WaitWhilePausedAsync() :
                Task.CompletedTask;
        }
    }
}
