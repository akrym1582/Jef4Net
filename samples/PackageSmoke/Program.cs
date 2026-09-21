using System.Diagnostics;
using System.Text;
using Jef4Net.Fujitsu;
using Jef4Net.Hitachi;

Encoding.RegisterProvider(FujitsuEncodingProvider.Instance);
Encoding.RegisterProvider(HitachiEncodingProvider.Instance);
Encoding encoding = Encoding.GetEncoding("x-Fujitsu-EBCDIC-Lower+JEF");
byte[] bytes = encoding.GetBytes("aあb海c");
if (Convert.ToHexString(bytes) != "8128A4A2298228B3A42983" || encoding.GetString(bytes) != "aあb海c")
    throw new InvalidOperationException("Package smoke test failed.");
Console.WriteLine("Package smoke test passed: " + encoding.GetString(bytes));
Encoding keis = Encoding.GetEncoding("x-Hitachi-EBCDIC+KEIS83", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
if (Convert.ToHexString(keis.GetBytes("aあb")) != "810A42A4A20A4182" || keis.GetString(keis.GetBytes("aあb")) != "aあb")
    throw new InvalidOperationException("KEIS package test failed.");
Encoding roundtrip = Encoding.GetEncoding("JEF-Roundtrip", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
if (Convert.ToHexString(roundtrip.GetBytes("あ")) != "A4A2" ||
    roundtrip.GetString(Convert.FromHexString("A4A2")) != "あ")
    throw new InvalidOperationException("Roundtrip package test failed.");
try { roundtrip.GetString(Convert.FromHexString("A1A1")); throw new InvalidOperationException("Roundtrip rejected-code test failed."); }
catch (DecoderFallbackException) { }
Encoding hanyo = Encoding.GetEncoding("JEF-HanyoDenshi", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
string ivs = "\u4E08\U000E0103";
if (Convert.ToHexString(hanyo.GetBytes(ivs)) != "41A5" || hanyo.GetString(Convert.FromHexString("41A5")) != ivs)
    throw new InvalidOperationException("HanyoDenshi package test failed.");
Encoding mixedHanyo = Encoding.GetEncoding("EBCDIC-Lower+JEF-HanyoDenshi", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
if (mixedHanyo.GetString(mixedHanyo.GetBytes("a" + ivs + "b")) != "a" + ivs + "b")
    throw new InvalidOperationException("Mixed HanyoDenshi package test failed.");
if (args.Contains("--benchmark"))
{
    foreach (string name in new[] { "x-Fujitsu-JEF", "x-Fujitsu-EBCDIC-Lower+JEF" })
    {
        var e = Encoding.GetEncoding(name);
        string text = string.Concat(Enumerable.Repeat(name.EndsWith("Lower+JEF") ? "aあb海c" : "あ海", 10000));
        var buffer = new byte[e.GetByteCount(text)];
        var chars = new char[text.Length];
        e.GetBytes(text.AsSpan(), buffer); e.GetChars(buffer, chars);
        long before = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < 100; i++) { e.GetBytes(text.AsSpan(), buffer); e.GetChars(buffer, chars); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
        Console.WriteLine($"{name}: {100.0 * buffer.Length / seconds / 1048576:F1} MiB/s encode+decode, {allocated} allocated bytes");
    }
}
