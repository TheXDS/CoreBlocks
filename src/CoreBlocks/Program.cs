﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TheXDS.CoreBlocks
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.CursorVisible = false;
            var g = new GameField();
            await g.PlayAsync();
        }        
    }

    internal class GameField
    {
        private const int _wellWidth = 14;
        private const int _wellHeight = 24;

        private static readonly Random _rnd = new Random();
        private static readonly byte[] Shapes =
        {
            0b_1100_1100, // O
            0b_1111_0000, // I
            0b_1110_0010, // J
            0b_0111_0100, // L
            0b_1100_0110, // Z
            0b_0110_1100, // S
            0b_1110_0100, // T
        };

        private readonly byte?[,] _well = new byte?[_wellWidth, _wellHeight];
        private readonly object _syncLock = new object();

        private void DrawBlock(byte block, int x, int y)
        {
            Console.SetCursorPosition((x * 2) + 1, y + 1);
            Console.BackgroundColor = (ConsoleColor)(block % 16);
            Console.Write("[]");
            Console.ResetColor();
        }

        private void DrawBlock(int x, int y) => DrawBlock((byte)(_shape + 1), x, y);

        private void ClearBlock(int x, int y)
        {
            Console.SetCursorPosition((x * 2) + 1, y + 1);            
            Console.ResetColor();
            Console.Write("  ");
        }

        private void DrawShape(in int x, in int y, in byte rotation)
        {
            TransformRotate(x, y, rotation, DrawBlock);            
        }

        private void ClearShape(in int x, in int y, byte rotation)
        {
            TransformRotate(x, y, rotation, ClearBlock);
        }

        private void TransformRotate(in byte shapeData, in int x, in int y, in byte r, Action<int, int> action)
        {
            TransformRotate(shapeData, x, y, r, (nx, ny) => { action(nx, ny); return true; });
        }
        private void TransformRotate(in int x, in int y, in byte r, Action<int, int> action)
        {
            TransformRotate(Shapes[_shape], x, y, r, (nx, ny) => { action(nx, ny); return true; });
        }

        private void TransformRotate(in int x, in int y, in byte r, Func<int, int, bool> action)
        {
            TransformRotate(Shapes[_shape], x, y, r, action);
        }

        private void TransformRotate(byte shapeData, in int x, in int y, in byte r, Func<int, int, bool> action)
        {
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
                    if (!action(px, py)) return;
                }
            }
        }

        private void UpdateShape(int relX, int relY, byte relR)
        {
            if (Fits(relX, relY, relR) != true) return;
            lock (_syncLock)
            {
                ClearShape(_px, _py, _r);
                _px += relX;
                _py += relY;
                _r = (byte)((_r + relR) % (_shape switch { 0 => 1, 1 => 2, _ => 4 }));
                DrawShape(_px, _py, _r);
            }            
        }

        private void DrawWell()
        {
            for (byte j = 0; j < _wellWidth; j++)
            {
                for (byte k = 0; k < _wellHeight; k++)
                {
                    if (!_well[j, k].HasValue) ClearBlock(j,k);
                    else DrawBlock(_well[j, k].Value, j, k);
                }
            }
        }

        private void DrawUI()
        {
            Console.SetCursorPosition(0, 0);
            Console.Write($"+{new string('-', _wellWidth * 2)}+");
            for (var j = 1; j <= _wellHeight; j++)
            {
                Console.SetCursorPosition(0, j);
                Console.Write("|");
                Console.SetCursorPosition(_wellWidth*2 + 1, j);
                Console.Write("|");
            }
            Console.SetCursorPosition(0, _wellHeight + 1);
            Console.Write($"+{new string('-', _wellWidth * 2)}+");
            Console.SetCursorPosition(_wellWidth * 2 + 4, 6);
            Console.Write("Siguiente:");
        }

        public Task PlayAsync()
        {
            DrawUI();
            return Task.WhenAll(
                Task.Run(ShapeLoop),
                Task.Run(HandleInput)
            );
        }

        private void HandleInput()
        {
            while (KeepPlaying)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.LeftArrow:
                        UpdateShape(-1, 0, 0);
                        break;
                    case ConsoleKey.RightArrow:
                        UpdateShape(1, 0, 0);
                        break;
                    case ConsoleKey.UpArrow:
                        UpdateShape(0, 0, 1);
                        break;
                    case ConsoleKey.DownArrow:
                        UpdateShape(0, 1, 0);
                        break;
                }
            }
        }

        private void ShapeLoop()
        {
            _nextShape = (byte)_rnd.Next(Shapes.Length);
            _nextR = (byte)_rnd.Next(4);
            while (KeepPlaying)
            {
                SelectNextShape();

                _px = (_wellWidth - 2) / 2;
                _py = -1;

                while (Fits(0, 1, 0) == true)
                {
                    UpdateShape(0, 1, 0);
                    Thread.Sleep(1000 / Level);
                }
                if (_py < 0) break;
                //Thread.Sleep(1000 - (1000 / Level)); //Permitir movimiento por 1000 ms al llegar al fondo
                TransformRotate(_px, _py, _r, SetWellBits);
                CheckLines();
            }
            KeepPlaying = false;
            PrintMessage("Fin del juego.");
        }

        private void SelectNextShape()
        {
            _shape = _nextShape;
            _r = _nextR;
            ClearShape(_wellWidth + 2, 7, _r);
            _nextShape = (byte)_rnd.Next(Shapes.Length);
            _nextR = (byte)_rnd.Next(4);
            TransformRotate(Shapes[_nextShape], _wellWidth + 2, 7, _nextR, (px, py)=> DrawBlock((byte)(_nextShape + 1), px, py));
        }

        private void CheckLines()
        {
            var lines = 0;
            for (var j = 0; j < _wellHeight; j++)
            {
                var k = 0;
                for(; k < _wellWidth; k++)
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
                            _well[k,l] = _well[k, l - 1];
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
                CheckScore(lines);
            }
            else
            {
                _combo = 0;
            }
        }

        private void CheckScore(int lines)
        {
            DrawWell();
            if (_combo > 0)
            {
                PrintMessage(lines switch
                {
                    1 => $"1 Línea (x{_combo} Combo)",                        
                    _ => $"{lines} Líneas!! (x{_combo} Combo)"
                });
            }
            else
            {
                PrintMessage(lines switch
                {
                    1 => "1 Línea",
                    2 => "2 Líneas!",
                    3 => "3 Líneas!!",
                    4 => "4 Líneas!!!",
                    _ => $"WTF!? {lines} LÍNEAS!?"
                });
            }
            _combo++;
            Score += (lines * 100) * _combo;
            PrintMessage($"Puntaje {Score}", 3);
            if (Score > 1000 * Level)
            {
                PrintMessage($"Nivel {++Level}", 4);
            }
        }

        private void SetWellBits(int arg1, int arg2)
        {
            _well[arg1, arg2] = (byte)(_shape + 1);
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
        private bool? Fits(int relX, int relY, byte relR)
        {
            lock (_syncLock)
            {
                bool? retval = true;
                TransformRotate(_px + relX, _py + relY, (byte)(_r + relR), (px, py) => 
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

        private byte _nextShape;
        private byte _nextR;

        private int _combo;

        /// <summary>
        /// Obtiene o establece el nivel de juego activo.
        /// </summary>
        public int Level { get; set; } = 1;
        public int Score { get; private set; } = 0;

        /// <summary>
        /// Obtiene o establece un valor que indica si el juego continúa.
        /// </summary>
        public bool KeepPlaying { get; set; } = true;

        private async void PrintMessage(string message, int line = 1)
        {
            lock (_syncLock)
            {
                Console.SetCursorPosition(_wellWidth * 2 + 2, line);
                Console.WriteLine(message);
            }
            await Task.Delay(3000);
            lock (_syncLock)
            {
                Console.SetCursorPosition(_wellWidth * 2 + 2, line);
                Console.WriteLine(new string(' ', message.Length));
            }
        }
    }
}
