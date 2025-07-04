//
// Program.cs
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
using System.Linq;
using System.Threading.Tasks;

namespace TheXDS.CoreBlocks;

internal static class Program
{
    private static async Task Main()
    {
        Console.Clear();
        while (true)
        {
            Console.Title = "Coreblocks";
            if (SelectGame() is { } config)
            {
                Console.CursorVisible = false;
                var g = new GameField(config, new ConsoleGameDrawing(config));
                Console.Clear();
                await g.PlayAsync();
                await Task.Delay(5000);
                Console.Clear();
                Console.CursorVisible = true;
            }
            else
            {
                break;
            }
        }
    }

    private static readonly IReadOnlyDictionary<string, GameConfig> _gameConfigs = new Dictionary<string, GameConfig>()
    {
        { "Estándar", GameConfig.Standard },
        { "Clásico", GameConfig.Classic },
        { "Extendido", GameConfig.Extended },
        { "Gigante", GameConfig.Huge }
    }.AsReadOnly();

    private static GameConfig? SelectGame()
    {
        DrawTitleScreen();
        int a = 0;
        Console.WriteLine("Seleccione el modo de juego:");
        foreach (var config in _gameConfigs)
        {
            Console.WriteLine($"  {++a}: {config.Key}");
        }
        Console.WriteLine($"  0: Salir");
        Console.Write("Ingrese el número del modo de juego: ");
        while (true)
        {
            if (int.TryParse(Console.ReadLine(), out int selected) && selected >= 0 && selected <= _gameConfigs.Count)
            {
                if (selected == 0)
                {
                    return null;
                }
                Console.Title = $"{_gameConfigs.Keys.ElementAt(selected - 1)} - Coreblocks";
                return _gameConfigs.Values.ElementAt(selected - 1);
            }
            Console.Write("Entrada inválida. Intente de nuevo: ");
        }

    }

    private static void DrawTitleScreen()
    {
        ConsoleBasicDrawing drawing = new();
        var r = 0;
        foreach (var row in TitleScreen)
        {
            var c = 0;
            foreach (var cell in row)
            {
                drawing.DrawBlock(cell, c, r);
                c++;
            }
            r++;
        }
        Console.WriteLine();
    }

    private static readonly byte[][] TitleScreen =
    [
        [15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15],
        [15,1,1,1,15,15,15,15,15,15,15,15,5,15,15,6,15,15,15,15,15,8,15,15,15,15,15,15],
        [15,1,15,15,2,2,2,3,3,4,4,4,5,15,15,6,2,2,2,7,7,8,15,15,15,9,9,15],
        [15,1,15,15,2,15,2,3,15,4,4,4,5,5,5,6,2,15,2,7,15,8,15,8,9,9,15,15],
        [15,1,15,15,2,15,2,3,15,4,15,15,5,15,5,6,2,15,2,7,15,8,8,15,15,15,9,15],
        [15,1,1,1,2,2,2,3,15,4,4,15,5,5,5,6,2,2,2,7,7,8,15,8,9,9,15,15],
        [15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15]
    ];
}
