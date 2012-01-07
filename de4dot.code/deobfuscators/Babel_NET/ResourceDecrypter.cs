﻿/*
    Copyright (C) 2011 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using Mono.Cecil;

namespace de4dot.code.deobfuscators.Babel_NET {
	class ResourceDecrypter {
		ModuleDefinition module;

		public ResourceDecrypter(ModuleDefinition module) {
			this.module = module;
		}

		public byte[] decrypt(byte[] encryptedData) {
			int index = 0;
			byte[] key, iv;
			bool isCompressed = getKeyIv(getHeaderData(encryptedData, ref index), out key, out iv);
			var data = DeobUtils.desDecrypt(encryptedData, index, encryptedData.Length - index, key, iv);
			if (isCompressed)
				data = DeobUtils.inflate(data, true);

			return data;
		}

		byte[] getHeaderData(byte[] encryptedData, ref int index) {
			bool xorDecrypt = encryptedData[index++] != 0;
			var headerData = new byte[BitConverter.ToUInt16(encryptedData, index)];
			Array.Copy(encryptedData, index + 2, headerData, 0, headerData.Length);
			index += headerData.Length + 2;
			if (!xorDecrypt)
				return headerData;

			var key = new byte[8];
			Array.Copy(encryptedData, index, key, 0, key.Length);
			index += key.Length;
			for (int i = 0; i < headerData.Length; i++)
				headerData[i] ^= key[i % key.Length];
			return headerData;
		}

		bool getKeyIv(byte[] headerData, out byte[] key, out byte[] iv) {
			var reader = new BinaryReader(new MemoryStream(headerData));
			var license = reader.ReadString();
			bool isCompressed = reader.ReadBoolean();
			iv = reader.ReadBytes(reader.ReadByte());
			bool hasEmbeddedKey = reader.ReadBoolean();
			if (hasEmbeddedKey)
				key = reader.ReadBytes(reader.ReadByte());
			else {
				key = new byte[reader.ReadByte()];
				Array.Copy(module.Assembly.Name.PublicKey, 12, key, 0, key.Length);
				key[5] |= 0x80;
			}
			return isCompressed;
		}
	}
}
