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

using System.Threading;
using System.Threading.Tasks;

namespace TheXDS.CoreBlocks
{
    /// <summary>
    /// Clase que permite la creación y gestión de objetos
    /// <see cref="PauseToken"/>, utilizados para pausar la ejecución 
    /// cooperativa de tareas.
    /// </summary>
    /// <seealso href="https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/" />
    public class PauseTokenSource
    {
        /// <summary>
        /// Obtiene o establece un valor que indica si este orígen de pausa se
        /// encuentra en estado pausado.
        /// </summary>
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

        /// <summary>
        /// Obtiene un nuevo <see cref="PauseToken"/> que observará el estado
        /// de esta instancia.
        /// </summary>
        public PauseToken Token => new(this);

        private volatile TaskCompletionSource<bool>? m_paused;

        internal Task WaitWhilePausedAsync()
        {
            var cur = m_paused;
            return cur != null ? cur.Task : Completed;
        }

        private static readonly Task<bool> Completed = Task.FromResult(true);
    }
}
