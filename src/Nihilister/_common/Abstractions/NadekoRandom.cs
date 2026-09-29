using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Nihilister.Common;

/// <summary>
/// Cryptographically secure, bias-free random number generator.
/// All methods are thread-safe.
/// </summary>
public sealed class NadekoRandom
{
    /// <summary>
    /// Returns a non-negative random integer in [0, int.MaxValue).
    /// </summary>
    public int Next()
        => RandomNumberGenerator.GetInt32(int.MaxValue);

    /// <summary>
    /// Returns a random integer in [0, <paramref name="maxValue"/>).
    /// </summary>
    public int Next(int maxValue)
        => RandomNumberGenerator.GetInt32(maxValue);

    /// <summary>
    /// Returns a random integer in [<paramref name="minValue"/>, <paramref name="maxValue"/>).
    /// </summary>
    public int Next(int minValue, int maxValue)
        => RandomNumberGenerator.GetInt32(minValue, maxValue);

    /// <summary>
    /// Returns a random long in [0, <paramref name="maxValue"/>).
    /// </summary>
    public long NextLong(long maxValue)
        => NextLong(0, maxValue);

    /// <summary>
    /// Returns a random long in [<paramref name="minValue"/>, <paramref name="maxValue"/>).
    /// </summary>
    public long NextLong(long minValue, long maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minValue, maxValue);

        var range = (ulong)(maxValue - minValue);
        var threshold = (0UL - range) % range;

        Span<byte> buf = stackalloc byte[8];
        ulong result;
        do
        {
            RandomNumberGenerator.Fill(buf);
            result = BinaryPrimitives.ReadUInt64LittleEndian(buf);
        } while (result < threshold);

        return (long)(result % range) + minValue;
    }

    /// <summary>
    /// Returns a uniform random double in [0.0, 1.0) with 53 bits of mantissa precision.
    /// </summary>
    public double NextDouble()
    {
        Span<byte> buf = stackalloc byte[8];
        RandomNumberGenerator.Fill(buf);
        var bits = BinaryPrimitives.ReadUInt64LittleEndian(buf) >> 11;
        return bits * (1.0 / (1UL << 53));
    }
}