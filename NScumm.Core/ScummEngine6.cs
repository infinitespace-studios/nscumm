﻿//
//  ScummEngine6.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace NScumm.Core
{
    enum VoiceMode
    {
        Voice = 0,
        VoiceAndText = 1,
        Text = 2
    }

    partial class ScummEngine6: ScummEngine5
    {
        int VariableTimeDateYear;
        int VariableTimeDateMonth;
        int VariableTimeDateDay;
        int VariableTimeDateHour;
        int VariableTimeDateMinute;
        int VariableV6EMSSpace;
        int? VariableV6SoundMode;
        int? VariableCharsetMask;
        int? VariableTimeDateSecond;

        public ScummEngine6(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            VariableRandomNumber = 118;
            VariableRoomWidth = 41;
            VariableRoomHeight = 54;

            VariableVoiceMode = 60; // 0 is voice, 1 is voice+text, 2 is text only
            VariableSaveLoadScript = 61;
            VariableSaveLoadScript2 = 62;

            VariableLeftButtonHold = 74;
            VariableRightButtonHold = 75;

            VariableV6EMSSpace = 76;
            VariableRandomNumber = 118;

            VariableTimeDateYear = 119;
            VariableTimeDateMonth = 129;
            VariableTimeDateDay = 128;
            VariableTimeDateHour = 125;
            VariableTimeDateMinute = 126;

            // Sam & Max specific
            if (Game.Id == "samnmax")
            {
                VariableV6SoundMode = 9;
                VariableCharsetMask = 123;
            }

            Variables[VariableRoomWidth.Value] = ScreenWidth;
            Variables[VariableRoomHeight.Value] = ScreenHeight;
            Variables[VariableVoiceMode.Value] = (int)VoiceMode.VoiceAndText;
            Variables[VariableV6EMSSpace] = 10000;

            foreach (var array in _resManager.ArrayDefinitions)
            {
                DefineArray(array.Index, (ArrayType)array.Type, array.Dim2, array.Dim1);
            }
        }

        protected override void InitOpCodes()
        {
            _opCodes = (from method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                 let attributes = (OpCodeAttribute[])method.GetCustomAttributes(typeof(OpCodeAttribute), false)
                                 where attributes.Length > 0
                                 from id in attributes[0].Ids
                                 select new {OpCode = id,Action = OpCode(method)}).ToDictionary(o => o.OpCode, o => o.Action);
        }

        Action OpCode(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentException("A method was expected.", "method");

            var args = new List<Func<object>>();
            foreach (var param in method.GetParameters().Reverse())
            {
                var paramType = param.ParameterType;
                if (paramType.IsArray && paramType.GetElementType() == typeof(int))
                {
                    args.Add(() => GetStackList(int.MaxValue));
                }
                else if (paramType == typeof(byte))
                {
                    args.Add(() => (byte)Pop());
                }
                else if (paramType == typeof(short))
                {
                    args.Add(() => (short)Pop());
                }
                else if (paramType == typeof(ushort))
                {
                    args.Add(() => (ushort)Pop());
                }
                else if (paramType == typeof(int))
                {
                    args.Add(() => Pop());
                }
                else
                {
                    throw new ArgumentException("An array was expected as parameter.", "method");
                }
            }

            var action = new Action(() =>
                { 
                    var parameterNames = method.GetParameters().Select(p => p.Name).ToArray();
                    var parameters = args.Select(arg => arg()).Reverse().ToArray();
                    Console.WriteLine("Room = {1,3}, Script = {0,3}, Offset = {4,4}, Name = [{3:X2}] {2}({5})", 
                        Slots[CurrentScript].Number, 
                        _roomResource, 
                        _opCodes.ContainsKey(_opCode) ? method.Name : "Unknown", 
                        _opCode,
                        CurrentPos - 1,
                        string.Join(",", parameters.Select((p, i) => string.Format("{0}={1}", parameterNames[i], GetDebuggerDisplayFor(p)))));
                    method.Invoke(this, parameters);
                });
            return action;
        }

        static string GetDebuggerDisplayFor(object value)
        {
            return value is int[] ? GetDebuggerDisplayForArray((int[])value) : value.ToString();
        }

        static string GetDebuggerDisplayForArray(int[] values)
        {
            return string.Format("[{0}]", string.Join(",", values.Select(o => o.ToString())));
        }
    }
}
