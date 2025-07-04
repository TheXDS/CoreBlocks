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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheXDS.CoreBlocks;

/// <summary>
/// Define una instancia del área de juego.
/// </summary>
internal class GameField(GameConfig config, IGameDrawing gameDrawing)
{
    private readonly struct TransformRotateResult(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
    }

    /// <summary>
    /// Obtiene la configuración del juego.
    /// </summary>
    public GameConfig Config { get; } = config;

    public IGameDrawing GameDrawing { get; } = gameDrawing;

    /// <summary>
    /// Obtiene el estado actual del juego.
    /// </summary>
    public GameState CurrentState { get; } = config.DefaultState ?? new(config);

    private static readonly object _syncLock = new();

    /// <summary>
    /// Contiene una colección de las teclas configuradas para controlar el
    /// juego.
    /// </summary>
    private readonly Dictionary<ConsoleKey, Action<GameState>> _keyBindings = [];

    /// <summary>
    /// Token de control de pausa del juego.
    /// </summary>
    private readonly PauseTokenSource _pauseSource = new();

    /// <summary>
    /// Interruptor del ciclo principal de figuras. Permite detener el
    /// tiempo de espera del nivel para tomar acciones inmediatas.
    /// </summary>
    private TaskCompletionSource<bool> _shapeBreaker = new();

    /// <summary>
    /// Rotación actual de la pieza activa.
    /// </summary>
    private byte _r = 0;

    /// <summary>
    /// Posición X actual de la pieza activa.
    /// </summary>
    private int _px;

    /// <summary>
    /// Posición Y actual de la pieza activa.
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
    /// Usado en la detección de T-Spins.
    /// </summary>
    private bool _rotateMove = false;

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

    #region Métodos de dibujo básicos

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
    private void ClearShape(in byte shape, in int x, in int y, in byte rotation)
    {
        TransformRotate(shape, x, y, rotation, (_, px, py) => GameDrawing.ClearBlock(px, py));
    }

    /// <summary>
    /// Dibuja toda el área de juego.
    /// </summary>
    private void DrawWell()
    {
        for (byte j = 0; j < Config.WellWidth; j++)
        {
            for (byte k = 0; k < Config.WellHeight; k++)
            {
                if (!CurrentState.Well[j, k].HasValue) GameDrawing.ClearBlock(j, k);
                else GameDrawing.DrawBlock(CurrentState.Well[j, k]!.Value, j, k);
            }
        }
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
        TransformRotate(shape, x, y, rotation, GameDrawing.DrawBlock);
    }

    /// <summary>
    /// Dibuja la sombra de la figura actual.
    /// </summary>
    private void DrawShadow()
    {
        var bottom = CalcBottom();
        if (bottom > _py)
        {
            TransformRotate(_shape, _px, bottom, _r, (_, px, py) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                GameDrawing.DrawBlock(-1, px, py);
            });
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
    /// Borra la sombra de la figura activa.
    /// </summary>
    private void ClearShadow()
    {
        var bottom = CalcBottom();
        if (bottom > _py)
        {
            TransformRotate(_shape, _px, bottom, _r, (_, px, py) => GameDrawing.ClearBlock(px, py));
        }
    }

    /// <summary>
    /// Muestra un mensaje que desaparecerá luego de 3000 ms al jugador.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="line">Línea en la cual colocar el mensaje.</param>
    private async void PrintMessage(string message, int line = 1)
    {
        GameDrawing.PutMessage(message, line);
        await Task.Delay(3000);
        GameDrawing.PutMessage(new string(' ', message.Length), line);
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
    private void TransformRotate(in byte shape, int x, int y, in byte r, Func<int, int, int, bool> function)
    {
        var shapeData = Config.ShapeSet[shape];
        shapeData.CustomPreRotationTransform?.Invoke(r, ref x, ref y);

        for (byte j = 0; j < 8; j++)
        {
            if (((shapeData.ShapeBits << j) & 128) != 0)
            {
                TransformRotateResult result = (r % 4) switch
                {
                    0 => new(x + (j % 4), y + (j / 4)),
                    1 => new(x + (1 - (j / 4)), y + (j % 4)),
                    2 => new(x + (2 - (j % 4)), y + (1 - (j / 4))),
                    3 => new(x + (j / 4), y + (3 - (j % 4))),
                    _ => throw new InvalidOperationException()
                };
                if (!function(shape, result.X, result.Y)) return;
            }
        }
    }

    /// <summary>
    /// Actualiza el estado de la figura activa.
    /// </summary>
    /// <param name="relX">Nueva posición X relativa.</param>
    /// <param name="relY">Nueva posición Y relativa.</param>
    /// <param name="relR">Nueva rotación relativa.</param>
    private bool UpdateShape(in int relX, in int relY, in int relR)
    {
        if (Fits(relX, relY, relR) != true) return false;
        lock (_syncLock)
        {
            ClearShadow();
            ClearShape();
            _px += relX;
            _py += relY;
            _r = (byte)((byte)(_r + relR) % (_shape switch { 0 => 1, 1 => 2, _ => 4 }));
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
    private void SelectNextShape(in byte newShape)
    {
        _shape = newShape;
        _r = 0;
        _px = (Config.WellWidth - 2) / 2;
        _py = -1;
    }

    /// <summary>
    /// Establece la figura activa como la figura en la cola de "Siguiente".
    /// </summary>
    private void SelectNextShape()
    {
        SelectNextShape(_nextShape);
        TransformRotate(_nextShape, Config.WellWidth + 2, 2, 0, (_, px, py) => GameDrawing.ClearBlock(px, py));
        _nextShape = Config.NextBlockSelector.Invoke();
        TransformRotate(_nextShape, Config.WellWidth + 2, 2, 0, GameDrawing.DrawBlock);
    }

    /// <summary>
    /// Ejecuta una comprobación de maniobra T-Spin.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> si el sitio final de colocación de la pieza
    /// cumple con los requisitos de un T-Spin, <see langword="false"/> en
    /// caso contrario.
    /// </returns>
    private bool CheckTSpin()
    {
        bool HorizontalCheck()
        {
            return _py < Config.WellHeight - 2
                ? (CurrentState.Well[_px, _py].HasValue ^ CurrentState.Well[_px + 2, _py].HasValue) && CurrentState.Well[_px, _py + 2].HasValue && CurrentState.Well[_px + 2, _py + 2].HasValue
                : CurrentState.Well[_px, _py].HasValue ^ CurrentState.Well[_px + 2, _py].HasValue;
        }

        return _r switch
        {
            0 or 2 => HorizontalCheck(),
            1 => CurrentState.Well[_px + 2, _py].HasValue && CurrentState.Well[_px, _py + 2].HasValue && CurrentState.Well[_px + 2, _py + 2].HasValue,
            3 => CurrentState.Well[_px, _py].HasValue && CurrentState.Well[_px, _py + 2].HasValue && CurrentState.Well[_px + 2, _py + 2].HasValue,
            _ => false
        } && _rotateMove;
    }

    /// <summary>
    /// Al finalizar un ciclo de pieza activa, comprueba la completación de
    /// líneas.
    /// </summary>
    private void CheckLines()
    {
        var tspin = false;
        var lines = 0;

        // T-spin check
        if (_shape == 6 && (tspin = CheckTSpin()))
        {
            PrintMessage($"T-Spin!", 6);
            PrintMessage($"+{Config.PointsPerTSpin}", 7);
            Score += Config.PointsPerTSpin;
            UpdateScoreBoard();
        }

        for (var j = 0; j < Config.WellHeight; j++)
        {
            var k = 0;
            for (; k < Config.WellWidth; k++)
            {
                if (!CurrentState.Well[k, j].HasValue) break;
            }
            if (k == Config.WellWidth)
            {
                lines++;
                for (var l = j; l > 0; l--)
                {
                    for (k = 0; k < Config.WellWidth; k++)
                    {
                        CurrentState.Well[k, l] = CurrentState.Well[k, l - 1];
                    }
                }
                for (k = 0; k < Config.WellWidth; k++)
                {
                    CurrentState.Well[k, 0] = null;
                }
            }
        }
        if (lines > 0)
        {
            Lines += lines;
            _combo++;

            // Bravo check
            var bravo = true;
            foreach (var b in CurrentState.Well)
            {
                if (b.HasValue)
                {
                    bravo = false;
                    break;
                }
            }
            CheckScore(lines, bravo, tspin);
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
    /// <param name="bravo">
    /// Premio adicional por limpiar completamente el pozo.
    /// </param>
    /// <param name="tspin">
    /// Premio por ejecutar una maniobra de T-Spin.
    /// </param>
    private void CheckScore(in int lines, in bool bravo, in bool tspin)
    {
        if (tspin)
        {
            PrintMessage($"+{Config.PointsPerBravo}", 7);
            Score += Config.PointsPerTSpin * lines;
        }
        if (bravo)
        {
            PrintMessage($"Bravo!", 6);
            PrintMessage($"+{Config.PointsPerBravo}", 7);
            Score += Config.PointsPerBravo * lines;
        }
        PrintMessage(lines == 1 ? "1 Línea" : $"{lines} Líneas!", 8);
        if (_combo > 1) PrintMessage($"(x{_combo} Combo)", 9);
        Score += lines * Config.PointsPerLine * _combo;
        PrintMessage($"Líneas {Lines}", 11);
        UpdateScoreBoard();
        if (Lines > (Level * Config.LinesPerLevel))
        {
            PrintMessage($"Nivel {++Level}", 14);
        }
    }

    /// <summary>
    /// Actualiza el marcador de puntaje y nivel en el encabezado de la ventana de consola.
    /// </summary>
    private void UpdateScoreBoard()
    {
        GameDrawing.PutMessage($"Nivel {Level}", 20);
        GameDrawing.PutMessage($"{Lines} líneas", 21);
        GameDrawing.PutMessage("Puntos:", 23);
        GameDrawing.PutMessage(Score.ToString(), 24);
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
        CurrentState.Well[x, y] = (byte)shape;
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
    private bool? Fits(in int relX, in int relY, int relR)
    {
        lock (_syncLock)
        {
            bool? retval = true;
            TransformRotate(_shape, _px + relX, _py + relY, (byte)(_r + relR), (_, px, py) =>
            {
                if (_shape == 1 && relR != 0) px++;
                if (px < 0 || px >= Config.WellWidth) { retval = null; return false; }
                if (py < 0 || py >= Config.WellHeight) { retval = false; return false; }
                if (CurrentState.Well[px, py].HasValue) { retval = false; return false; }
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
    /// Intenta ejecutar una rotación en una dirección en particular.
    /// </summary>
    /// <param name="direction">Dirección de rotación.</param>
    private void Rotate(in int direction)
    {
        _ = UpdateShape(0, 0, direction) ||

            // Kicks
            // izquierda...
            UpdateShape(-1, 0, direction) ||
            UpdateShape(-1, -1, direction) ||
            UpdateShape(-1, 1, direction) ||
            (_shape == 1 && UpdateShape(-2, 0, direction)) ||

            // derecha...
            UpdateShape(1, 0, direction) ||
            UpdateShape(1, 1, direction) ||
            UpdateShape(1, -1, direction) ||

            // arriba...
            UpdateShape(0, 1, direction);
    }

    /// <summary>
    /// Obtiene la cantidad de milisegundos de espera del ciclo principal
    /// del juego basado en el nivel actual.
    /// </summary>
    /// <returns>
    /// La cantidad de milisegundos a esperar en el ciclo principal del
    /// juego.
    /// </returns>
    private int GetLevelTime()
    {
        return Config.LevelTimerStart - ((Level - 1) * Config.LevelTimerStep);
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
            if (_keyBindings.TryGetValue(Console.ReadKey(true).Key, out var action)) action.Invoke(CurrentState);
        }
    }

    /// <summary>
    /// Hilo que controla la aparición de figuras en el juego.
    /// </summary>
    private async Task ShapeLoopAsync()
    {
        _nextShape = Config.NextBlockSelector.Invoke();
        while (KeepPlaying)
        {
            SelectNextShape();
            while (Fits(0, 1, 0) == true)
            {
                await _pauseSource.WaitWhilePausedAsync();
                UpdateShape(0, 1, 0);
                if (_shapeBreaker.Task == await Task.WhenAny(Task.Delay(GetLevelTime()), _shapeBreaker.Task))
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
        QuitGame(CurrentState);
    }

    #endregion

    #region Acciones de control del juego

    /// <summary>
    /// Desplaza la pieza activa una posición hacia la izquierda.
    /// </summary>
    private void MoveLeft(GameState state)
    {
        _rotateMove = false; 
        UpdateShape(-1, 0, 0);
    }

    /// <summary>
    /// Desplaza la pieza activa una posición hacia la derecha.
    /// </summary>
    private void MoveRight(GameState state)
    {
        _rotateMove = false; 
        UpdateShape(1, 0, 0);
    }

    /// <summary>
    /// Gira la pieza activa hacia la derecha.
    /// </summary>
    private void RotateCw(GameState state)
    {
        _rotateMove = true;
        Rotate(1);
    }

    /// <summary>
    /// Gira la pieza activa hacia la izquierda.
    /// </summary>
    private void RotateCcw(GameState state)
    {
        _rotateMove = true;
        Rotate(-1);
    }

    /// <summary>
    /// Baja la pieza rápidamente.
    /// </summary>
    private void SoftDrop(GameState state)
    {
        UpdateShape(0, 1, 0);
    }

    /// <summary>
    /// Suelta la pieza y la envía a su ubucación final en el pozo de forma
    /// inmediata.
    /// </summary>
    private void HardDrop(GameState state)
    {
        UpdateShape(0, CalcBottom() - _py, 0);
        _shapeBreaker.SetResult(true);
    }

    /// <summary>
    /// Intercambia la pieza activa con la pieza actualmente en Hold. Si
    /// Hold no contiene ninguna pieza, envía la pieza actual al Hold e
    /// inicia un nuevo ciclo de pieza.
    /// </summary>
    private void HoldCurrent(GameState state)
    {
        if (!Config.AllowHold) return;
        _rotateMove = false;
        if (_holdUsed) return;
        _holdUsed = true;
        lock (_syncLock)
        {
            ClearShape();
            ClearShadow();
        }

        if (!_hold.HasValue)
        {
            _hold = _shape;
            SelectNextShape();
        }
        else
        {
            lock (_syncLock) ClearShape(_hold.Value, -6, 2, 0);
            var tmphold = _shape;
            SelectNextShape(_hold.Value);
            _hold = tmphold;
        }
        lock (_syncLock) DrawShape(_hold.Value, -6, 2, 0);
        _shapeBreaker.SetResult(true);
    }

    /// <summary>
    /// Activa o desactiva la pausa del juego.
    /// </summary>
    private void TogglePause(GameState state)
    {
        if (_pauseSource.IsPaused)
        {
            DrawWell();
            _pauseSource.IsPaused = false;
        }
        else
        {
            _pauseSource.IsPaused = true;
            GameDrawing.ClearWell();
            GameDrawing.PrintMainMessage("Juego en pausa");
        }
    }

    /// <summary>
    /// Termina el juego.
    /// </summary>
    private void QuitGame(GameState state)
    {
        KeepPlaying = false;
        GameDrawing.PrintMainMessage("Fin del juego.");
        _py = -4;
        _shapeBreaker.SetResult(true);
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
        GameDrawing.DrawUI();
        UpdateScoreBoard();
        DrawWell();
        ConfigureKeyBindings();
        return Task.WhenAll(
            ShapeLoopAsync(),
            Task.Run(HandleInput)
        );
    }

    private void ConfigureKeyBindings()
    {
        /* Agregar KeyBindings adicionales primero permite hacer "Override"
         * de las teclas originales.
         */
        foreach (var j in Config.ExtraKeyBindings ?? [])
        {
            _keyBindings.Add(j.Key, j.Value);
        }

        _keyBindings.TryAdd(ConsoleKey.LeftArrow, MoveLeft);
        _keyBindings.TryAdd(ConsoleKey.A, MoveLeft);
        _keyBindings.TryAdd(ConsoleKey.NumPad4, MoveLeft);
        _keyBindings.TryAdd(ConsoleKey.RightArrow, MoveRight);
        _keyBindings.TryAdd(ConsoleKey.D, MoveRight);
        _keyBindings.TryAdd(ConsoleKey.NumPad6, MoveRight);
        _keyBindings.TryAdd(ConsoleKey.UpArrow, RotateCw);
        _keyBindings.TryAdd(ConsoleKey.W, RotateCw);
        _keyBindings.TryAdd(ConsoleKey.NumPad9, RotateCw);
        _keyBindings.TryAdd(ConsoleKey.NumPad8, RotateCw);
        _keyBindings.TryAdd(ConsoleKey.NumPad7, RotateCcw);
        _keyBindings.TryAdd(ConsoleKey.X, RotateCcw);
        _keyBindings.TryAdd(ConsoleKey.DownArrow, SoftDrop);
        _keyBindings.TryAdd(ConsoleKey.S, SoftDrop);
        _keyBindings.TryAdd(ConsoleKey.NumPad5, SoftDrop);
        _keyBindings.TryAdd(ConsoleKey.Spacebar, HardDrop);
        _keyBindings.TryAdd(ConsoleKey.NumPad2, HardDrop);
        _keyBindings.TryAdd(ConsoleKey.C, HoldCurrent);
        _keyBindings.TryAdd(ConsoleKey.NumPad0, HoldCurrent);
        _keyBindings.TryAdd(ConsoleKey.Pause, TogglePause);
        _keyBindings.TryAdd(ConsoleKey.P, TogglePause);
        _keyBindings.TryAdd(ConsoleKey.Q, QuitGame);
        _keyBindings.TryAdd(ConsoleKey.Escape, QuitGame);
    }
}
