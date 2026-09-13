using Riptide;
using System;
using System.Numerics;

namespace NanoPackets.Utils;
public static class Extensions {
    #region Half
    /// <summary>Adds a <see cref="float"/> in range of 0..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="float"/> to add.</param>
    /// <returns>The message that the <see cref="float"/> was added to.</returns>
    public static Message AddHalf(this Message message, float value) {
        return message.AddUShort((ushort)(Math.Clamp(value, 0f, 1f) * ushort.MaxValue));
    }

    /// <summary>Retrieves a <see cref="float"/> in range of 0..=1 from the message in <see cref="ushort"/> precision.</summary>
    /// <returns>The <see cref="ushort"/> that was retrieved.</returns>
    public static float GetHalf(this Message message) {
        return message.GetUShort() / (float)ushort.MaxValue;
    }
    #endregion

    #region SHalf
    /// <summary>Adds a <see cref="float"/> in range of -1..=1 to the message in <see cref="short"/> precision.</summary>
    /// <param name="value">The <see cref="float"/> to add.</param>
    /// <returns>The message that the <see cref="float"/> was added to.</returns>
    public static Message AddSHalf(this Message message, float value) {
        return message.AddShort((short)(Math.Clamp(value, -1f, 1f) * short.MaxValue));
    }

    /// <summary>Retrieves a <see cref="float"/> in range of -1..=1 from the message in <see cref="short"/> precision.</summary>
    /// <returns>The <see cref="ushort"/> that was retrieved.</returns>
    public static float GetSHalf(this Message message) {
        return message.GetShort() / (float)short.MaxValue;
    }
    #endregion

    public static void AddOptionalString(this Message msg, string? text) {
        if(text is string t) {
            msg.AddBool(true);
            msg.AddString(t);
        } else {
            msg.AddBool(false);
        }
    }

    public static string? GetOptionalString(this Message msg) {
        return msg.GetBool() ? msg.GetString() : null;
    }

    public static void AddVarULong<T>(this Message msg, T value) where T : IBinaryInteger<T> {
        msg.AddVarULong(ulong.CreateChecked(value));
    }

    public static T GetVarULong<T>(this Message msg) where T : IBinaryInteger<T> {
        return T.CreateChecked(msg.GetVarULong());
    }

    /// <remarks>
    /// Zigzag-encodes onto <see cref="AddVarULong(Message, ulong)"/> rather than using Riptide's own
    /// <c>Message.AddVarLong(long)</c>: that method corrupts any value whose zigzag encoding needs the
    /// 64th bit (confirmed in isolation, with no NanoPackets code involved - e.g. long.MinValue round
    /// trips as 0, long.MaxValue as -1), while the unsigned varint writer it and this both sit on top
    /// of is correct across its full range.
    /// </remarks>
    public static void AddVarLong<T>(this Message msg, T value) where T : IBinaryInteger<T> {
        var signed = long.CreateChecked(value);
        msg.AddVarULong((ulong)((signed << 1) ^ (signed >> 63)));
    }

    public static T GetVarLong<T>(this Message msg) where T : IBinaryInteger<T> {
        var zigzagged = msg.GetVarULong();
        var signed = (long)(zigzagged >> 1) ^ -(long)(zigzagged & 1);
        return T.CreateChecked(signed);
    }

    public static void AddVarULongs<T>(this Message msg, T[] values) where T : IBinaryInteger<T> {
        msg.AddVarULong((ulong)values.Length);
        foreach(var value in values) {
            msg.AddVarULong(ulong.CreateChecked(value));
        }
    }

    public static T[] GetVarULongs<T>(this Message msg) where T : IBinaryInteger<T> {
        var len = (int)msg.GetVarULong();
        var array = new T[len];
        for(var i = 0; i < len; i++) {
            array[i] = T.CreateChecked(msg.GetVarULong());
        }
        return array;
    }

    public static void AddVarLongs<T>(this Message msg, T[] values) where T : IBinaryInteger<T> {
        msg.AddVarULong((ulong)values.Length);
        foreach(var value in values) {
            msg.AddVarLong(value);
        }
    }

    public static T[] GetVarLongs<T>(this Message msg) where T : IBinaryInteger<T> {
        var len = (int)msg.GetVarULong();
        var array = new T[len];
        for(var i = 0; i < len; i++) {
            array[i] = msg.GetVarLong<T>();
        }
        return array;
    }
}
