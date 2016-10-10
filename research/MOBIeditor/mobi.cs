using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBIeditor {
	// source: http://wiki.mobileread.com/wiki/MOBI
	public class MOBIFormat {
		public PDBFormat PDB;
		public PalmDocHeaderFormat PalmDocHeader;
		public MobiHeaderFormat MobiHeader;
		public ExthHeaderFormat ExthHeader;
		public byte[] Remainder; // remainder of record 0 // this is probably not super good; eg, the book title is in here with an offset in to the MOBI header - this is likely to shift if we change EXTH property values

		public class PalmDocHeaderFormat {
			public enum CompressionType { None = 1, PalmDoc = 2, HUFF_CDIC = 17480 }
			public enum EncryptionType { None = 0, OldMobiPocket = 1, MobiPocket = 2 }

			public CompressionType Compression;
			public int BookLength;
			public short Count;
			public short Size; // always 4096
			public EncryptionType Encryption;
			public short Unknown; // usually 0
		}

		public class MobiHeaderFormat {
			public enum MobiType { Mobipocket_Book = 2, PalmDoc_Book = 3, Audio = 4, mobipocket_generated_by_kindlegen1_2 = 232, KF8_generated_by_kindlegen2 = 248, News = 257, News_Feed = 258, News_Magazine = 259, PICS = 513, WORD = 514, XLS = 515, PPT = 516, TEXT = 517, HTML = 518 }
			public enum EncodingType { CP1252_WinLatin1 = 1252, UTF_8 = 65001 }

			public byte[] Data; // most of this is inscrutible so just dump it in a byte[]

			public MobiType Type { get { return (MobiType)GetInt32_BigEndian(0); } }
			public EncodingType Encoding { get { return (EncodingType)GetInt32_BigEndian(4); } }
			public int UniqueId { get { return GetInt32_BigEndian(8); } }
			public int FileVersion { get { return GetInt32_BigEndian(12); } }
			public bool HasEXTH { get { return ((GetInt32_BigEndian(104) & 0x40) == 0x40); } }

			public int GetInt32_BigEndian(int offset) {
				return Get_BigEndian_Int32_FromByteArray(Data, offset);
			}
		}

		public class ExthHeaderFormat {
			public List<ExthHeaderRecordFormat> Records;

			/// <summary> EXTH header length does include the final padding. </summary>
			public int Length { get { return 12 + Records.Sum(o => o.Data.Length + 8); } }

			/// <summary> The padding is not technically part of the EXTH header - it merely follows the EXTH header. </summary>
			public int Padding { get { return ((int)Math.Ceiling((double)Length / 4) * 4) - Length; } }

			public class ExthHeaderRecordFormat {
				public readonly static Dictionary<int, string> RecordTypes = new Dictionary<int, string> { { 1, "DRM Server Id" }, { 2, "DRM Commernece Id" }, { 3, "DRM eBookBase Book Id" }, { 100, "Author" }, { 101, "Publisher" }, { 102, "Imprint" }, { 103, "Description" }, { 104, "ISBN" }, { 105, "Subject" }, { 106, "Publishing Date" }, { 107, "Review" }, { 108, "Contributor" }, { 109, "Rights" }, { 110, "Subject Code" }, { 111, "Type" }, { 112, "Source" }, { 113, "ASIN" }, { 114, "Version Number" }, { 115, "Sample" }, { 116, "Start Reading" }, { 117, "Adult" }, { 118, "Retail Price" }, { 119, "Retail Price Currency" }, { 200, "Dictionary Short Name" }, { 201, "Cover Offset" }, { 202, "Thumb Offset" }, { 203, "Has Fake Cover" }, { 204, "Creator Software" }, { 205, "Creator Major Version" }, { 206, "Creator Minor Version" }, { 207, "Creator Build Number" }, { 208, "Watermark" }, { 209, "Tamper Proof Keys" }, { 300, "Font Signature" }, { 401, "Clipping Limit" }, { 402, "Publisher Limit" }, { 403, "Unknown" }, { 404, "Text to Speech Flat" }, { 405, "Unknown" }, { 406, "Unknown" }, { 407, "Unknown" }, { 450, "Unknown" }, { 451, "Unknown" }, { 452, "Unknown" }, { 453, "Unknown" }, { 501, "CDE Type" }, { 502, "Last Update Time" }, { 503, "Updated Title" }, { 504, "ASIN" } };

				public readonly Guid Id;
				public int Type;
				public byte[] Data;

				public ExthHeaderRecordFormat() {
					Id = Guid.NewGuid();
				}

				public int SortOrder {
					get {
						switch (Type) {
							case 503: return 10; // updated title
							case 100: return 11; // author
							case 101: return 12; // publisher
							case 106: return 20; // publish date
						}
						return 99999;
					}
				}
				public string TypeName { get { return RecordTypes.ContainsKey(Type) ? RecordTypes[Type] : "Unknown [" + Type + "]"; } }
				public string GetDataAsString() {
					return Encoding.ASCII.GetString(Data);
				}
			}
		}

		public MOBIFormat(FileInfo mobiFile) {
			PDB = new PDBFormat(mobiFile);
			if (PDB.Type != "BOOK" || PDB.Creator != "MOBI") throw new Exception("Invalid MOBI file (type or creator not correct).");

			var firstRecord = PDB.RecordInfos.First();

			using (var bin = new BinaryReader2(new MemoryStream(firstRecord.Data))) {
				// create PalmDoc header
				PalmDocHeader = new PalmDocHeaderFormat {Compression = (PalmDocHeaderFormat.CompressionType) bin.ReadInt16_BigEndian()};
			    bin.ReadInt16_BigEndian();
				PalmDocHeader.BookLength = bin.ReadInt32_BigEndian();
				PalmDocHeader.Count = bin.ReadInt16_BigEndian();
				PalmDocHeader.Size = bin.ReadInt16_BigEndian();
				PalmDocHeader.Encryption = (PalmDocHeaderFormat.EncryptionType)bin.ReadInt16_BigEndian();
				PalmDocHeader.Unknown = bin.ReadInt16_BigEndian();

				// create MOBI header
				MobiHeader = new MobiHeaderFormat();
				if (Encoding.ASCII.GetString(bin.ReadBytes(4)) != "MOBI") throw new Exception("MOBI header not found.");
				var headerLength = bin.ReadInt32_BigEndian();
				MobiHeader.Data = bin.ReadBytes(headerLength - 8);

				// create EXTH header (if one exists)
				if (MobiHeader.HasEXTH) {
					ExthHeader = new ExthHeaderFormat();
					if (Encoding.ASCII.GetString(bin.ReadBytes(4)) != "EXTH") throw new Exception("EXTH header not found.");
					headerLength = bin.ReadInt32_BigEndian();
					var numRecords = bin.ReadInt32_BigEndian();
					ExthHeader.Records = new List<ExthHeaderFormat.ExthHeaderRecordFormat>();
					for (var i = 0; i < numRecords; i++) {
						var rec = new ExthHeaderFormat.ExthHeaderRecordFormat {Type = bin.ReadInt32_BigEndian()};
					    var length = bin.ReadInt32_BigEndian();
						rec.Data = bin.ReadBytes(length - 8);
						ExthHeader.Records.Add(rec);
					}
					for (var p = 0; p < ExthHeader.Padding; p++) bin.ReadByte(); // consume and ignore the padding
				}

				Remainder = bin.ReadBytes((int)(bin.BaseStream.Length - bin.BaseStream.Position));
			}

		}

		public void Save(FileInfo mobiFile) {
			var newData = new MemoryStream();
			var bin = new BinaryWriter2(newData);

			// create PalmDoc header
			bin.WriteInt16_BigEndian((short)PalmDocHeader.Compression);
			bin.WriteInt16_BigEndian(0);
			bin.WriteInt32_BigEndian(PalmDocHeader.BookLength);
			bin.WriteInt16_BigEndian(PalmDocHeader.Count);
			bin.WriteInt16_BigEndian(PalmDocHeader.Size);
			bin.WriteInt16_BigEndian((short)PalmDocHeader.Encryption);
			bin.WriteInt16_BigEndian(PalmDocHeader.Unknown);

			// create MOBI header
			bin.Write(Encoding.ASCII.GetBytes("MOBI"));
			bin.WriteInt32_BigEndian(MobiHeader.Data.Length + 8);
			bin.Write(MobiHeader.Data);

			// create EXTH header (maybe)
			if (MobiHeader.HasEXTH) {
				bin.Write(Encoding.ASCII.GetBytes("EXTH"));
				bin.WriteInt32_BigEndian(ExthHeader.Length);
				bin.WriteInt32_BigEndian(ExthHeader.Records.Count);
				foreach (var rec in ExthHeader.Records) {
					bin.WriteInt32_BigEndian(rec.Type);
					bin.WriteInt32_BigEndian(rec.Data.Length + 8);
					bin.Write(rec.Data);
				}
				for (var p = 0; p < ExthHeader.Padding; p++) bin.Write((byte)0);
			}

			bin.Write(Remainder);

			PDB.RecordInfos[0].Data = newData.ToArray();
			PDB.Save(mobiFile);
		}

		public static short Get_BigEndian_Short_FromByteArray(byte[] b, int offset) {
			var temp = new byte[2];
			for (var i = 0; i < 2; i++) temp[i] = b[offset + (1 - i)];
			return BitConverter.ToInt16(temp, 0);
		}

		public static int Get_BigEndian_Int32_FromByteArray(byte[] b, int offset) {
			var temp = new byte[4];
			for (var i = 0; i < 4; i++) temp[i] = b[offset + (3 - i)];
			return BitConverter.ToInt32(temp, 0);
		}

	}
}
