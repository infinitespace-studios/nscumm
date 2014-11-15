﻿//
//  ScummEngine6_Script.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
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
using System;

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        bool _skipVideo;

        int? VariableRandomNumber;

        [OpCode(0x5e)]
        void StartScript(int flags, int script, int[] args)
        {
            RunScript((byte)script, (flags & 1) != 0, (flags & 2) != 0, args);
        }

        [OpCode(0x5f)]
        void StartScriptQuick(int script, int[] args)
        {
            RunScript((byte)script, false, false, args);
        }

        [OpCode(0x60)]
        void StartObject(int flags, int script, byte entryp, int[] args)
        {
            RunObjectScript(script, entryp, (flags & 1) != 0, (flags & 2) != 0, args);
        }

        [OpCode(0x65, 0x66)]
        void StopObjectCode6()
        {
            StopObjectCode();
        }

        [OpCode(0x67)]
        void EndCutscene()
        {
            EndCutsceneCore();
        }

        [OpCode(0x68)]
        void Cutscene(int[] args)
        {
            BeginCutscene(args);
        }

        [OpCode(0x6a)]
        void FreezeUnfreeze(int script)
        {
            if (script != 0)
                FreezeScripts(script);
            else
                UnfreezeScripts();
        }

        [OpCode(0x6c)]
        void BreakHere()
        {
            BreakHereCore();
        }

        [OpCode(0x77)]
        void StopObjectScript(ushort script)
        {
            StopObjectScriptCore(script);
        }

        [OpCode(0x7c)]
        void StopScript6(int script)
        {
            if (script == 0)
            {
                StopObjectCode();
            }
            else
            {
                StopScript(script);
            }
        }

        [OpCode(0x83)]
        void DoSentence(byte verb, ushort objectA, int tmp, ushort objectB)
        {
            DoSentence(verb, objectA, objectB);
        }

        [OpCode(0x87)]
        void GetRandomNumber(int max)
        {
            var rnd = new Random().Next(Math.Abs(max) + 1);
            Variables[VariableRandomNumber.Value] = rnd;
            Push(rnd);
        }

        [OpCode(0x88)]
        void GetRandomNumberRange(int min, int max)
        {
            var rnd = new Random().Next(min, max);
            Variables[VariableRandomNumber.Value] = rnd;
            Push(rnd);
        }

        [OpCode(0x95)]
        void BeginOverride()
        {
            BeginOverrideCore();
            _skipVideo = false;
        }

        [OpCode(0x96)]
        void EndOverride()
        {
            EndOverrideCore();
        }

        [OpCode(0x99)]
        void SetBoxFlags(int[] args, int value)
        {
            var num = args.Length;
            while (--num >= 0)
            {
                SetBoxFlags(args[num], value);
            }
        }

        [OpCode(0x9a)]
        void CreateBoxMatrix()
        {
            CreateBoxMatrixCore();

            if ((Game.Id == "dig") || (Game.Id == "cmi"))
                PutActors();
        }

        [OpCode(0xa1)]
        void PseudoRoom(byte value, int[] args)
        {
            var num = args.Length;
            while (--num >= 0)
            {
                var a = args[num];
                if (a > 0x7F)
                    _resourceMapper[a & 0x7F] = value;
            }
        }

        [OpCode(0xa9)]
        void Wait()
        {
            var offs = -2;
            var subOp = ReadByte();

            switch (subOp)
            {
                case 168:               // SO_WAIT_FOR_ACTOR Wait for actor
                    {
                        offs = ReadWordSigned();
                        var index = Pop();
                        var actor = _actors[index];
                        if (Game.Version >= 7)
                        {
                            if (actor.IsInCurrentRoom && actor.Moving != MoveFlags.None)
                                break;
                        }
                        else
                        {
                            if (actor.Moving != MoveFlags.None)
                                break;
                        }
                    }
                    return;
                case 169:               // SO_WAIT_FOR_MESSAGE Wait for message
                    if (Variables[VariableHaveMessage.Value] != 0)
                        break;
                    return;
                case 170:               // SO_WAIT_FOR_CAMERA Wait for camera
                    if (Game.Version >= 7)
                    {
                        if (Camera.DestinationPosition != Camera.CurrentPosition)
                            break;
                    }
                    else
                    {
                        if (Camera.CurrentPosition.X / 8 != Camera.DestinationPosition.X / 8)
                            break;
                    }

                    return;
                case 171:               // SO_WAIT_FOR_SENTENCE
                    if (SentenceNum != 0)
                    {
                        if (Sentence[SentenceNum - 1].IsFrozen && !IsScriptInUse(Variables[VariableSentenceScript.Value]))
                            return;
                        break;
                    }
                    if (!IsScriptInUse(Variables[VariableSentenceScript.Value]))
                        return;
                    break;
                case 226:               // SO_WAIT_FOR_ANIMATION
                    {
                        offs = ReadWordSigned();
                        var index = Pop();
                        var actor = _actors[index];
                        if (actor.IsInCurrentRoom && actor.NeedRedraw)
                            break;
                        return;
                    }
                case 232:               // SO_WAIT_FOR_TURN
                    // WORKAROUND for bug #744441: An angle will often be received as the
                    // actor number due to script bugs in The Dig. In all cases where this
                    // occurs, _curActor is set just before it, so we can use it instead.
                    //
                    // For now, if the value passed in is divisible by 45, assume it is an
                    // angle, and use _curActor as the actor to wait for.
                    {
                        offs = ReadWordSigned();
                        var index = Pop();
                        if (index % 45 == 0)
                        {
                            index = _curActor;
                        }
                        var actor = _actors[index];
                        if (actor.IsInCurrentRoom && actor.Moving.HasFlag(MoveFlags.Turn))
                            break;
                        return;
                    }
                default:
                    throw new NotSupportedException(string.Format("Wait: default case 0x{0:X}", subOp));
            }

            CurrentPos += offs;
            BreakHere();
        }

        [OpCode(0xb0)]
        void Delay(int delay)
        {
            DelayCore(delay);
        }

        [OpCode(0xb1)]
        void DelaySeconds(int seconds)
        {
            DelayCore(seconds * 60);
        }

        [OpCode(0xb2)]
        void DelayMinutes(int minutes)
        {
            DelayCore(minutes * 3600);
        }

        [OpCode(0xb3)]
        void StopSentence()
        {
            SentenceNum = 0;
            StopScript(Variables[VariableSentenceScript.Value]);
            // TODO: scumm6
            //            ClearClickedStatus();
        }

        [OpCode(0xbe)]
        void StartObjectQuick(int script, byte entryp, int[] args)
        {
            RunObjectScript(script, entryp, false, true, args);
        }

        [OpCode(0xca)]
        void DelayFrames()
        {
            var ss = Slots[CurrentScript];
            if (ss.DelayFrameCount == 0)
            {
                ss.DelayFrameCount = (ushort)Pop();
            }
            else
            {
                ss.DelayFrameCount--;
            }
            if (ss.DelayFrameCount != 0)
            {
                CurrentPos--;
                BreakHere();
            }
        }

        [OpCode(0x8b)]
        void IsScriptRunning(int script)
        {
            Push(IsScriptRunningCore(script));
        }

        [OpCode(0xbf)]
        void StartScriptQuick2(byte script, int[] args)
        {
            RunScript(script, false, true, args);
        }

        [OpCode(0xd5)]
        void JumpToScript(int flags, int script, int[] args)
        {
            StopObjectCode();
            RunScript((byte)script, (flags & 1) != 0, (flags & 2) != 0, args);
        }

        [OpCode(0xd8)]
        void IsRoomScriptRunning(int script)
        {
            Push(IsRoomScriptRunningCore(script));
        }

        void PutActors()
        {
            for (var i = 1; i < _actors.Length; i++)
            {
                var a = _actors[i];
                if (a != null && a.IsInCurrentRoom)
                    a.PutActor();
            }
        }

        void DelayCore(int delay)
        {
            Slots[CurrentScript].Delay = delay;
            Slots[CurrentScript].Status = ScriptStatus.Paused;
            BreakHere();
        }

        bool IsRoomScriptRunningCore(int script)
        {
            for (var i = 0; i < Slots.Length; i++)
                if (Slots[i].Number == script && Slots[i].Where == WhereIsObject.Room && Slots[i].Status != ScriptStatus.Dead)
                    return true;
            return false;
        }
    }
}
