﻿/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace NScumm.Core.Graphics
{
    internal sealed class CharsetRendererClassic : CharsetRenderer
    {
        private int _offsX, _offsY;
        private int _width, _height, _origWidth, _origHeight;
        private byte[] _fontPtr;
        private int _fontPos;
        private int _charPos;
        private int _fontHeight;
        public uint _numChars;

        public CharsetRendererClassic(ScummEngine vm)
            : base(vm)
        {
        }

        public override void PrintChar(int chr, bool ignoreCharsetMask)
        {
            VirtScreen vs;
            bool is2byte = (chr >= 256 && _vm._useCJKMode);

            //ScummHelper.AssertRange(1, _curId, _vm._numCharsets - 1, "charset");

            if ((vs = _vm.FindVirtScreen(_top)) == null && (vs = _vm.FindVirtScreen(_top + GetFontHeight())) == null)
                return;

            if (chr == '@')
                return;

            _vm._charsetColorMap[1] = _color;

            if (!PrepareDraw(chr))
                return;

            if (_firstChar)
            {
                _str.left = 0;
                _str.top = 0;
                _str.right = 0;
                _str.bottom = 0;
            }

            _top += _offsY;
            _left += _offsX;

            if (_left + _origWidth > _right + 1 || _left < 0)
            {
                _left += _origWidth;
                _top -= _offsY;
                return;
            }

            _disableOffsX = false;

            if (_firstChar)
            {
                _str.left = _left;
                _str.top = _top;
                _str.right = _left;
                _str.bottom = _top;
                _firstChar = false;
            }

            if (_left < _str.left)
                _str.left = _left;

            if (_top < _str.top)
                _str.top = _top;

            int drawTop = _top - vs.TopLine;

            _vm.MarkRectAsDirty(vs, _left, _left + _width, drawTop, drawTop + _height);

            // This check for kPlatformFMTowns and kMainVirtScreen is at least required for the chat with
            // the navigator's head in front of the ghost ship in Monkey Island 1
            if (!ignoreCharsetMask)
            {
                _hasMask = true;
                _textScreen = vs;
            }

            PrintCharIntern(is2byte, _fontPtr, _charPos + 17, _origWidth, _origHeight, _width, _height, vs, ignoreCharsetMask);

            _left += _origWidth;

            if (_str.right < _left)
            {
                _str.right = _left;
            }

            if (_str.bottom < _top + _origHeight)
                _str.bottom = _top + _origHeight;

            _top -= _offsY;
        }

        private bool PrepareDraw(int chr)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(_fontPtr));
            reader.BaseStream.Seek(_fontPos + chr * 4 + 4, SeekOrigin.Begin);
            int charOffs = reader.ReadInt32();
            //assert(charOffs < 0x14000);
            if (charOffs == 0)
                return false;
            _charPos = charOffs;

            reader.BaseStream.Seek(_fontPos + _charPos, SeekOrigin.Begin);
            _width = _origWidth = reader.ReadByte();
            _height = _origHeight = reader.ReadByte();

            if (_disableOffsX)
            {
                _offsX = 0;
                reader.ReadByte();
            }
            else
            {
                _offsX = reader.ReadSByte();
            }

            _offsY = reader.ReadByte();

            _charPos += 4;	// Skip over char header
            return true;
        }

        public override void SetCurID(int id)
        {
            _curId = id;

            _fontPtr = _vm.Index.GetCharsetData((byte)id);
            if (_fontPtr == null)
                throw new NotSupportedException(string.Format("CharsetRendererCommon::setCurID: charset %d not found", id));

            _fontPos = 17;

            _fontHeight = _fontPtr[_fontPos + 1];
            _numChars = ((uint)_fontPtr[_fontPos + 2]) | (((uint)_fontPtr[_fontPos + 3]) << 8);
        }

        public override int GetFontHeight()
        {
            //if (_vm._useCJKMode)
            //    return Math.Max(_vm._2byteHeight + 1, _fontHeight);
            //else
            return _fontHeight;
        }

        public override int GetCharWidth(int chr)
        {
            int spacing = 0;
            BinaryReader reader = new BinaryReader(new MemoryStream(_fontPtr));
            reader.BaseStream.Seek(_fontPos + chr * 4 + 4, SeekOrigin.Begin);
            var offs = reader.ReadInt32();
            if (offs != 0)
            {
                reader.BaseStream.Seek(_fontPos + offs, SeekOrigin.Begin);
                spacing = reader.PeekChar();
                reader.BaseStream.Seek(2, SeekOrigin.Current);
                spacing += reader.ReadSByte();
            }
            return spacing;
        }

        private void PrintCharIntern(bool is2byte, byte[] _fontPtr, int charPos, int origWidth, int origHeight, int width, int height, VirtScreen vs, bool ignoreCharsetMask)
        {
            int drawTop = _top - vs.TopLine;

            PixelNavigator? back = null;
            PixelNavigator dstPtr;
            Surface dstSurface;
            if ((ignoreCharsetMask || !vs.HasTwoBuffers))
            {
                dstSurface = vs.Surfaces[0];
                dstPtr = new PixelNavigator(vs.Surfaces[0]);
                dstPtr.GoTo(vs.XStart + _left, drawTop);
            }
            else
            {
                dstSurface = _vm.TextSurface;
                dstPtr = new PixelNavigator(dstSurface);
                dstPtr.GoTo(_left, _top - _vm._screenTop);
            }

            if (_blitAlso && vs.HasTwoBuffers)
            {
                back = dstPtr;
                dstSurface = vs.Surfaces[0];
                dstPtr = new PixelNavigator(dstSurface);
                dstPtr.GoTo(vs.XStart + _left, drawTop);
            }

            if (!ignoreCharsetMask && vs.HasTwoBuffers)
            {
                drawTop = _top - _vm._screenTop;
            }

            DrawBitsN(dstSurface, dstPtr, _fontPtr, charPos, _fontPtr[_fontPos], drawTop, origWidth, origHeight);

            if (_blitAlso && vs.HasTwoBuffers)
            {
                // FIXME: Revisiting this code, I think the _blitAlso mode is likely broken
                // right now -- we are copying stuff from "dstPtr" to "back", but "dstPtr" really
                // only conatains charset data...
                // One way to fix this: don't copy etc.; rather simply render the char twice,
                // once to each of the two buffers. That should hypothetically yield
                // identical results, though I didn't try it and right now I don't know
                // any spots where I can test this...
                if (!ignoreCharsetMask)
                    throw new NotSupportedException("This might be broken -- please report where you encountered this to Fingolfin");

                // Perform some clipping
                int w = Math.Min(width, dstSurface.Width - _left);
                int h = Math.Min(height, dstSurface.Height - drawTop);
                if (_left < 0)
                {
                    w += _left;
                    back.Value.OffsetX(-_left);
                    dstPtr.OffsetX(-_left);
                }
                if (drawTop < 0)
                {
                    h += drawTop;
                    back.Value.OffsetY(-drawTop);
                    dstPtr.OffsetY(-drawTop);
                }

                // Blit the image data
                if (w > 0)
                {
                    while (h-- > 0)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            back.Value.Write(dstPtr.Read());
                            back.Value.OffsetX(1);
                            dstPtr.OffsetX(1);
                        }
                        back.Value.Offset(-w, 1);
                        dstPtr.Offset(-w, 1);
                    }
                }

            }
        }

        private void DrawBitsN(Surface s, PixelNavigator dst, byte[] src, int srcPos, byte bpp, int drawTop, int width, int height)
        {
            int y, x;
            int color;
            byte numbits, bits;

            if (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8)
                throw new ArgumentException("Invalid bpp","bpp");

            bits = src[srcPos++];
            numbits = 8;
            byte[] cmap = _vm._charsetColorMap;

            for (y = 0; y < height && y + drawTop < s.Height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    color = (bits >> (8 - bpp)) & 0xFF;

                    if (color != 0 && (y + drawTop >= 0))
                    {
                        dst.Write(cmap[color]);
                    }
                    dst.OffsetX(1);
                    bits <<= bpp;
                    numbits -= bpp;
                    if (numbits == 0)
                    {
                        bits = src[srcPos++];
                        numbits = 8;
                    }
                }
                dst.Offset(-width, 1);
            }
        }
    }
}
