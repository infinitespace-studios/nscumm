﻿//
//  IIMuse.cs
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

namespace NScumm.Core.Audio.IMuse
{
    delegate void SysExFunc(Player player,byte[] data,ushort length);

    enum ImuseProperty
    {
        TempoBase,
        NativeMt32,
        Gs,
        LimitPlayers,
        RecyclePlayers,
        GameId,
        PcSpeaker
    }

    interface IIMuse: IMusicEngine
    {
        void OnTimer(MidiDriver midi);

        void Pause(bool paused);
        //        int save_or_load(Serializer *ser, ScummEngine *scumm, bool fixAfterLoad = true);
        bool GetSoundActive(int sound);

        int DoCommand(int numargs, int[] args);

        int ClearQueue();

        uint Property(ImuseProperty prop, uint value);

        void AddSysexHandler(byte mfgID, SysExFunc handler);

        void HandleMarker(int id, int data);

        int GetMusicTimer();
    }

    static class IMuse
    {
        public static IIMuse Create(MidiDriver nativeMidiDriver, MidiDriver adlibMidiDriver)
        {
            var imuse = new IMuseInternal();
            imuse.Initialize(nativeMidiDriver, adlibMidiDriver);
            return imuse;
        }
    }
}
