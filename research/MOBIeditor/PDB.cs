using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MOBIeditor {

	// source: http://wiki.mobileread.com/wiki/PDB#Palm_Database_Format
	public class PDBFormat {
		public string DBName;
		public short Attributes;
		public short Version;
		public DateTime Creation, Modification, LastBackup;
		public int ModificationNumber, AppInfoOffset, SortInfoOffset;
		public byte[] AppInfo, SortInfo;
		public string Type, Creator; // four-byte strings
		public int UniqueIdSeed;
		public int NextRecordListId; // always 0
		public short NumRecords;
		public List<PDBRecordInfo> RecordInfos;
		public byte[] GapToData; // "traditionally 2 zero bytes to Info or raw data"

		public class PDBRecordInfo {
			public int DataOffset;
			public byte Attributes;
			public int UniqueId; // this is actually just three bytes
			public byte[] Data;
		}

		public PDBFormat(FileInfo pdbFile) {
			if (!pdbFile.Exists) throw new Exception("File not existing!");

			using (var bin = new BinaryReader2(pdbFile.OpenRead())) {
				DBName = Encoding.ASCII.GetString(bin.ReadBytes(32));
				Attributes = bin.ReadInt16_BigEndian();
				Version = bin.ReadInt16_BigEndian();

				Creation = GetDateTime(bin);
				Modification = GetDateTime(bin);
				LastBackup = GetDateTime(bin);

				ModificationNumber = bin.ReadInt32_BigEndian();
				AppInfoOffset = bin.ReadInt32_BigEndian(); // these two items are, I think, offsets into the data stream // we might want to save this data in a byte[] as well
				SortInfoOffset = bin.ReadInt32_BigEndian();

				Type = Encoding.ASCII.GetString(bin.ReadBytes(4));
				Creator = Encoding.ASCII.GetString(bin.ReadBytes(4));

				UniqueIdSeed = bin.ReadInt32_BigEndian();
				NextRecordListId = bin.ReadInt32_BigEndian();

				NumRecords = bin.ReadInt16_BigEndian();
				RecordInfos = new List<PDBRecordInfo>(NumRecords);
				for (short i = 0; i < NumRecords; i++) {
					RecordInfos.Add(new PDBRecordInfo
					    {
						DataOffset = bin.ReadInt32_BigEndian(),
						Attributes = bin.ReadByte(),
						UniqueId = bin.ReadByte() * 256 * 256 + bin.ReadByte() * 256 + bin.ReadByte()  // three byte value
					});
				}
				GapToData = bin.ReadBytes(RecordInfos.First().DataOffset - (int)bin.BaseStream.Position);

				// now collect the actual record data
				for (short i = 0; i < NumRecords; i++) {
					var rec = RecordInfos[i];
					var dataEnd = (i + 1 < NumRecords ? RecordInfos[i + 1].DataOffset : (int)bin.BaseStream.Length);
					bin.BaseStream.Position = rec.DataOffset;
					rec.Data = bin.ReadBytes(dataEnd - rec.DataOffset);
				}

				/* this is pure speculation // for all that we know the app and sort offsets just points to records
				if (AppInfoOffset != 0) {
					bin.BaseStream.Position = AppInfoOffset;
					AppInfo = bin.ReadBytes(4); // TODO: I have no idea how many to read here // perhaps (SortInfoOffset - AppInfoOfset)?
				}

				if (SortInfoOffset != 0) {
					bin.BaseStream.Position = SortInfoOffset;
					SortInfo = bin.ReadBytes(4); // TODO: same problem
				}
				*/

				bin.Close();
			}
		}

		public void Save(FileInfo pdbFile) {
			using (var bin = new BinaryWriter2(pdbFile.OpenWrite(), Encoding.ASCII)) {
				bin.Write(Encoding.ASCII.GetBytes(DBName), 0, 32);
				bin.WriteInt16_BigEndian(Attributes);
				bin.WriteInt16_BigEndian(Version);
				
				WriteDateTime(bin, Creation);
				WriteDateTime(bin, Modification);
				WriteDateTime(bin, LastBackup);

				bin.WriteInt32_BigEndian(ModificationNumber);
				bin.WriteInt32_BigEndian(AppInfoOffset); // TODO: if these offsets are not 0 we need to do some thinking
				bin.WriteInt32_BigEndian(SortInfoOffset);

				bin.Write(Encoding.ASCII.GetBytes(Type), 0, 4);
				bin.Write(Encoding.ASCII.GetBytes(Creator), 0, 4);

				bin.WriteInt32_BigEndian(UniqueIdSeed);
				bin.WriteInt32_BigEndian(NextRecordListId);

				// write record info list
				bin.WriteInt16_BigEndian(NumRecords);
				var nextOffset = 78 + NumRecords * 8 + GapToData.Length; // this may also have to include the appinfo and sortinfo byte[]
				for (short i = 0; i < NumRecords; i++) {
					var rec = RecordInfos[i];
					rec.DataOffset = nextOffset;
					bin.WriteInt32_BigEndian(rec.DataOffset);
					bin.Write(rec.Attributes);
					
					var temp = BitConverter.GetBytes(rec.UniqueId);
					Array.Reverse(temp);
					bin.Write(temp, 1, 3);

					nextOffset += rec.Data.Length;
				}
				bin.Write(GapToData);

				// write actual records
				for (short i = 0; i < NumRecords; i++) bin.Write(RecordInfos[i].Data);

				// var last = RecordInfos.Last();
			}
		}

		protected DateTime GetDateTime(BinaryReader2 bin) {
			// http://wiki.mobileread.com/wiki/PDB#PDB_Times
			// TODO: handle alternate case
			var c = bin.ReadInt32_BigEndian();
			return new DateTime(1970, 1, 1).AddSeconds(c);
		}

		protected void WriteDateTime(BinaryWriter2 bin, DateTime datetime) {
			var c = Convert.ToInt32(datetime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
			bin.WriteInt32_BigEndian(c);
		}
	}

	public class BinaryReader2 : BinaryReader {
		public BinaryReader2(Stream input) : base(input) { }

		public short ReadInt16_BigEndian() {
			return (short)(ReadByte() * 256 + ReadByte());
		}
		public int ReadInt32_BigEndian() {
			return ReadByte() * 256 * 256 * 256 + ReadByte() * 256 * 256 + ReadByte() * 256 + ReadByte();
		}
	}

	public class BinaryWriter2 : BinaryWriter {
		public BinaryWriter2(Stream output) : base(output) { }
		public BinaryWriter2(Stream output, Encoding encoding) : base(output, encoding) { }

		public void WriteInt16_BigEndian(short value) {
			var temp = BitConverter.GetBytes(value);
			Array.Reverse(temp);
			Write(BitConverter.ToInt16(temp, 0));
		}

		public void WriteInt32_BigEndian(int value) {
			var temp = BitConverter.GetBytes(value);
			Array.Reverse(temp);
			Write(BitConverter.ToInt32(temp, 0));
		}
	}
}
