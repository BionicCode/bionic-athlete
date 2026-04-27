namespace BionicAthlete.Training.Test.Decoding;

using System.Collections.Immutable;
using System.IO;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Caching;
using BionicAthlete.Training.Domain.Decoding;
using BionicAthlete.Training.Domain.Fields;

public sealed class CachingFitActivityDecoderTests
{
    [Fact]
    public async Task DecodeAsyncReusesCachedContentAcrossDifferentSourceNames()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingDecoder innerDecoder = new();
        CachingFitActivityDecoder decoder = new(innerDecoder, new InMemoryFitActivityCache());

        await using MemoryStream firstStream = new([1, 2, 3]);
        await using MemoryStream secondStream = new([1, 2, 3]);

        FitActivityDecodeResult firstResult = await decoder.DecodeAsync(firstStream, "first.fit", cancellationToken);
        FitActivityDecodeResult secondResult = await decoder.DecodeAsync(secondStream, "second.fit", cancellationToken);

        Assert.Equal(1, innerDecoder.StreamDecodeCallCount);
        Assert.False(firstResult.IsFromCache);
        Assert.True(secondResult.IsFromCache);
        Assert.Equal("second.fit", secondResult.Source.DisplayName);
        Assert.Equal("second.fit", Assert.IsType<FitActivity>(secondResult.Activity).Source.DisplayName);
    }

    [Fact]
    public async Task DecodeAsyncReturnsIndependentMutableStateFromCache()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingDecoder innerDecoder = new();
        CachingFitActivityDecoder decoder = new(innerDecoder, new InMemoryFitActivityCache());

        await using MemoryStream firstStream = new([5, 0, 0]);
        FitActivityDecodeResult firstResult = await decoder.DecodeAsync(firstStream, "first.fit", cancellationToken);
        FitField firstField = Assert.IsType<FitActivity>(firstResult.Activity).Sessions[0].Records[0].Fields[0];
        firstField.SetEditedDecodedValues([999]);

        await using MemoryStream secondStream = new([5, 0, 0]);
        FitActivityDecodeResult secondResult = await decoder.DecodeAsync(secondStream, "second.fit", cancellationToken);
        FitField secondField = Assert.IsType<FitActivity>(secondResult.Activity).Sessions[0].Records[0].Fields[0];

        Assert.Equal(1, innerDecoder.StreamDecodeCallCount);
        Assert.True(secondResult.IsFromCache);
        Assert.False(secondField.State.HasEditedDecodedValues);
        Assert.Equal((byte)5, secondField.GetEffectiveDecodedValues().Single());
    }

    [Fact]
    public async Task DecodeAsyncNormalizesStreamToPositionZeroBeforeHashingAndDecoding()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingDecoder innerDecoder = new();
        CachingFitActivityDecoder decoder = new(innerDecoder, new InMemoryFitActivityCache());

        // Stream positioned past the first byte — without normalization, hashing and
        // decoding would cover different byte ranges, producing a cache-key mismatch.
        await using MemoryStream firstStream = new([7, 2, 3]);
        firstStream.Position = 1;

        await using MemoryStream secondStream = new([7, 2, 3]);

        FitActivityDecodeResult firstResult = await decoder.DecodeAsync(firstStream, "first.fit", cancellationToken);
        FitActivityDecodeResult secondResult = await decoder.DecodeAsync(secondStream, "second.fit", cancellationToken);

        // Both streams have the same content; the second call must hit the cache.
        Assert.Equal(1, innerDecoder.StreamDecodeCallCount);
        Assert.False(firstResult.IsFromCache);
        Assert.True(secondResult.IsFromCache);
    }

    [Fact]
    public async Task DecodeAsyncDifferentContentProducesDifferentCacheEntries()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecordingDecoder innerDecoder = new();
        CachingFitActivityDecoder decoder = new(innerDecoder, new InMemoryFitActivityCache());

        await using MemoryStream firstStream = new([1, 2, 3]);
        await using MemoryStream secondStream = new([9, 2, 3]);

        FitActivityDecodeResult firstResult = await decoder.DecodeAsync(firstStream, "first.fit", cancellationToken);
        FitActivityDecodeResult secondResult = await decoder.DecodeAsync(secondStream, "first.fit", cancellationToken);

        Assert.Equal(2, innerDecoder.StreamDecodeCallCount);
        Assert.False(firstResult.IsFromCache);
        Assert.False(secondResult.IsFromCache);
        Assert.NotEqual(
            Assert.IsType<FitActivity>(firstResult.Activity).Sessions[0].Records[0].Fields[0].GetEffectiveDecodedValues().Single(),
            Assert.IsType<FitActivity>(secondResult.Activity).Sessions[0].Records[0].Fields[0].GetEffectiveDecodedValues().Single());
    }

    private sealed class RecordingDecoder : IFitActivityDecoder
    {
        public int StreamDecodeCallCount { get; private set; }

        public Task<FitActivityDecodeResult> DecodeFileAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<FitActivityDecodeResult> DecodeAsync(Stream stream, string? sourceName = null, CancellationToken cancellationToken = default)
        {
            StreamDecodeCallCount++;
            byte value = ReadFirstByte(stream);
            FitFileSource source = new(string.IsNullOrWhiteSpace(sourceName) ? "stream" : sourceName.Trim());

            FitField field = new(
                new FitFieldSnapshot(
                    new FitFieldKey(FitNodeType.Record, FitFieldKind.Standard, 20, 1),
                    FitExportColumnKey.FromField(new FitFieldKey(FitNodeType.Record, FitFieldKind.Standard, 20, 1)),
                    "value",
                    "record",
                    FitFieldKind.Standard,
                    baseType: 2,
                    baseTypeName: "uint8",
                    profileTypeName: "Uint8",
                    units: null,
                    scale: 1,
                    offset: 0,
                    isAccumulated: false,
                    isExpandedField: false,
                    developerApplicationIdBytes: ImmutableArray<byte>.Empty,
                    developerApplicationVersion: null,
                    nativeOverrideFieldNumber: null,
                    nativeOverrideMessageNumber: null,
                    isArray: false,
                    originalValues: ImmutableArray.Create(new FitFieldValue(value, value))));

            FitRecord record = new(
                new FitNodeSnapshot(
                    new FitNodeIdentity(FitNodeType.Record, 0, null),
                    messageNumber: 20,
                    messageName: "record",
                    localMessageNumber: 0,
                    timestampUtc: new DateTimeOffset(2024, 01, 01, 00, 00, value, TimeSpan.Zero),
                    startTimeUtc: null),
                ImmutableArray.Create(field));

            FitSession session = new(
                new FitNodeSnapshot(
                    new FitNodeIdentity(FitNodeType.Session, 0, 0),
                    messageNumber: 18,
                    messageName: "session",
                    localMessageNumber: 0,
                    timestampUtc: new DateTimeOffset(2024, 01, 01, 00, 00, 10, TimeSpan.Zero),
                    startTimeUtc: new DateTimeOffset(2024, 01, 01, 00, 00, 00, TimeSpan.Zero)),
                ImmutableArray<FitField>.Empty,
                ImmutableArray<FitLap>.Empty,
                ImmutableArray.Create(record));

            FitActivity activity = new(
                new FitNodeSnapshot(
                    new FitNodeIdentity(FitNodeType.Activity, 0, null),
                    messageNumber: 34,
                    messageName: "activity",
                    localMessageNumber: 0,
                    timestampUtc: new DateTimeOffset(2024, 01, 01, 00, 01, 00, TimeSpan.Zero),
                    startTimeUtc: null),
                ImmutableArray<FitField>.Empty,
                ImmutableArray.Create(session),
                source,
                FitActivityAncillaryData.Empty);

            return Task.FromResult(new FitActivityDecodeResult(activity, source, ImmutableArray<FitDecodeIssue>.Empty));
        }

        private static byte ReadFirstByte(Stream stream)
        {
            long originalPosition = stream.Position;
            int value = stream.ReadByte();
            stream.Position = originalPosition;
            return value < 0 ? (byte)0 : (byte)value;
        }
    }
}
