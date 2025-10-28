// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

// Source code is based on https://github.com/LayoutFarm/Typography
// see https://github.com/LayoutFarm/Typography/blob/master/Typography.OpenFont/WebFont/Woff2Reader.cs
internal class TripleEncodingTable
{
    public static readonly TripleEncodingTable EncTable = new();
    private readonly List<TripleEncodingRecord> records = [];

    private TripleEncodingTable() => this.BuildTable();

    public TripleEncodingRecord this[int i] => this.records[i];

    private void BuildTable()
    {
        // Each of the 128 index values define the following properties and specified in details in the table below:

        // Byte count(total number of bytes used for this set of coordinate values including one byte for 'flag' value).
        // Number of bits used to represent X coordinate value(X bits).
        // Number of bits used to represent Y coordinate value(Y bits).
        // An additional incremental amount to be added to X bits value(delta X).
        // An additional incremental amount to be added to Y bits value(delta Y).
        // The sign of X coordinate value(X sign).
        // The sign of Y coordinate value(Y sign).

        // Please note that "Byte Count" field reflects total size of the triplet(flag, xCoordinate, yCoordinate),
        // including ‘flag’ value that is encoded in a separate stream.

        // Triplet Encoding
        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 1.1)
        // 0     2            0       8       N/A       0     N/A     -
        // 1                                            0             +
        // 2                                           256            -
        // 3                                           256            +
        // 4                                           512            -
        // 5                                           512            +
        // 6                                           768            -
        // 7                                           768            +
        // 8                                           1024           -
        // 9                                           1024           +
        this.BuildRecords(2, 0, 8, [], [0, 256, 512, 768, 1024]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 1.2)
        // 10    2            8       0        0       N/A     -     N/A
        // 11                                  0               +
        // 12                                256               -
        // 13                                256               +
        // 14                                512               -
        // 15                                512               +
        // 16                                768               -
        // 17                                768               +
        // 18                                1024              -
        // 19                                1024              +
        this.BuildRecords(2, 8, 0, [0, 256, 512, 768, 1024], []);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 2.1)
        // 20    2           4       4        1        1       -      -
        // 21                                          1       +      -
        // 22                                          1       -      +
        // 23                                          1       +      +
        // 24                                          17      -      -
        // 25                                          17      +      -
        // 26                                          17      -      +
        // 27                                          17      +      +
        // 28                                          33      -      -
        // 29                                          33      +      -
        // 30                                          33      -      +
        // 31                                          33      +      +
        // 32                                          49      -      -
        // 33                                          49      +      -
        // 34                                          49      -      +
        // 35                                          49      +      +
        this.BuildRecords(2, 4, 4, [1], [1, 17, 33, 49]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 2.2)
        // 36    2           4       4       17        1       -      -
        // 37                                          1       +      -
        // 38                                          1       -      +
        // 39                                          1       +      +
        // 40                                          17      -      -
        // 41                                          17      +      -
        // 42                                          17      -      +
        // 43                                          17      +      +
        // 44                                          33      -      -
        // 45                                          33      +      -
        // 46                                          33      -      +
        // 47                                          33      +      +
        // 48                                          49      -      -
        // 49                                          49      +      -
        // 50                                          49      -      +
        // 51                                          49      +      +
        this.BuildRecords(2, 4, 4, [17], [1, 17, 33, 49]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 2.3)
        // 52    2           4          4     33        1      -      -
        // 53                                           1      +      -
        // 54                                           1      -      +
        // 55                                           1      +      +
        // 56                                          17      -      -
        // 57                                          17      +      -
        // 58                                          17      -      +
        // 59                                          17      +      +
        // 60                                          33      -      -
        // 61                                          33      +      -
        // 62                                          33      -      +
        // 63                                          33      +      +
        // 64                                          49      -      -
        // 65                                          49      +      -
        // 66                                          49      -      +
        // 67                                          49      +      +
        this.BuildRecords(2, 4, 4, [33], [1, 17, 33, 49]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 2.4)
        // 68    2           4         4     49         1      -      -
        // 69                                           1      +      -
        // 70                                           1      -      +
        // 71                                           1      +      +
        // 72                                          17      -      -
        // 73                                          17      +      -
        // 74                                          17      -     +
        // 75                                          17      +     +
        // 76                                          33      -     -
        // 77                                          33      +     -
        // 78                                          33      -     +
        // 79                                          33      +     +
        // 80                                          49      -     -
        // 81                                          49      +     -
        // 82                                          49      -     +
        // 83                                          49      +     +
        this.BuildRecords(2, 4, 4, [49], [1, 17, 33, 49]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 3.1)
        // 84    3             8       8         1      1      -     -
        // 85                                           1      +     -
        // 86                                           1      -     +
        // 87                                           1      +     +
        // 88                                         257      -     -
        // 89                                         257      +     -
        // 90                                         257      -     +
        // 91                                         257      +     +
        // 92                                         513      -     -
        // 93                                         513      +     -
        // 94                                         513      -     +
        // 95                                         513      +     +
        this.BuildRecords(3, 8, 8, [1], [1, 257, 513]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 3.2)
        // 96    3               8       8      257      1     -      -
        // 97                                            1     +      -
        // 98                                            1     -      +
        // 99                                            1     +      +
        // 100                                         257     -      -
        // 101                                         257     +      -
        // 102                                         257     -      +
        // 103                                         257     +      +
        // 104                                         513     -      -
        // 105                                         513     +      -
        // 106                                         513     -      +
        // 107                                         513     +      +
        this.BuildRecords(3, 8, 8, [257], [1, 257, 513]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 3.3)
        // 108   3              8        8       513     1     -      -
        // 109                                           1     +      -
        // 110                                           1     -      +
        // 111                                           1     +      +
        // 112                                         257     -      -
        // 113                                         257     +      -
        // 114                                         257     -      +
        // 115                                         257     +      +
        // 116                                         513     -      -
        // 117                                         513     +      -
        // 118                                         513     -      +
        // 119                                         513     +      +
        this.BuildRecords(3, 8, 8, [513], [1, 257, 513]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 4)
        // 120   4               12     12         0      0    -      -
        // 121                                                 +      -
        // 122                                                 -      +
        // 123                                                 +      +
        this.BuildRecords(4, 12, 12, [0], [0]);

        // Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
        // (set 5)
        // 124   5               16      16      0       0     -      -
        // 125                                                 +      -
        // 126                                                 -      +
        // 127                                                 +      +
        this.BuildRecords(5, 16, 16, [0], [0]);
    }

    private void BuildRecords(byte byteCount, byte xbits, byte ybits, ushort[] deltaXs, ushort[] deltaYs)
    {
        if (deltaXs.Equals(Array.Empty<ushort>()))
        {
            // (set 1.1)
            for (int y = 0; y < deltaYs.Length; ++y)
            {
                this.AddRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, -1);
                this.AddRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, 1);
            }
        }
        else if (deltaYs.Equals(Array.Empty<ushort>()))
        {
            // (set 1.2)
            for (int x = 0; x < deltaXs.Length; ++x)
            {
                this.AddRecord(byteCount, xbits, ybits, deltaXs[x], 0, -1, 0);
                this.AddRecord(byteCount, xbits, ybits, deltaXs[x], 0, 1, 0);
            }
        }
        else
        {
            // set 2.1, - set5
            for (int x = 0; x < deltaXs.Length; ++x)
            {
                ushort deltaX = deltaXs[x];

                for (int y = 0; y < deltaYs.Length; ++y)
                {
                    ushort deltaY = deltaYs[y];

                    this.AddRecord(byteCount, xbits, ybits, deltaX, deltaY, -1, -1);
                    this.AddRecord(byteCount, xbits, ybits, deltaX, deltaY, 1, -1);
                    this.AddRecord(byteCount, xbits, ybits, deltaX, deltaY, -1, 1);
                    this.AddRecord(byteCount, xbits, ybits, deltaX, deltaY, 1, 1);
                }
            }
        }
    }

    private void AddRecord(byte byteCount, byte xbits, byte ybits, ushort deltaX, ushort deltaY, sbyte xsign, sbyte ysign)
    {
        TripleEncodingRecord rec = new(byteCount, xbits, ybits, deltaX, deltaY, xsign, ysign);
        this.records.Add(rec);
    }
}
