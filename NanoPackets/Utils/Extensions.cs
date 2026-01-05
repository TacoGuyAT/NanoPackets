using Riptide;
using System.Numerics;

namespace NanoPackets.Utils;
public static class Extensions {
    #region Half
    /// <summary>Adds a <see cref="float"/> in range of 0..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="float"/> to add.</param>
    /// <returns>The message that the <see cref="float"/> was added to.</returns>
    public static Message AddHalf(this Message message, float value) {
        return message.AddUShort((ushort)(value * ushort.MaxValue));
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
        return message.AddShort((short)(value * short.MaxValue));
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

    public static bool GetOptionalString(this Message msg, out string? text) {
        if(msg.GetBool()) {
            text = msg.GetString();
            return true;
        } else {
            text = null;
            return false;
        }
    }

    public static void AddVarULong<T>(this Message msg, T value) where T : IBinaryInteger<T> {
        msg.AddVarULong(ulong.CreateChecked(value));
    }

    public static T GetVarULong<T>(this Message msg) where T : IBinaryInteger<T> {
        return T.CreateChecked(msg.GetVarULong());
    }

    public static void AddVarLong<T>(this Message msg, T value) where T : IBinaryInteger<T> {
        msg.AddVarLong(long.CreateChecked(value));
    }

    public static T GetVarLong<T>(this Message msg) where T : IBinaryInteger<T> {
        return T.CreateChecked(msg.GetVarLong());
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
        for(int i = 0; i < len; i++) {
            array[i] = T.CreateChecked(msg.GetVarULong());
        }
        return array;
    }

    public static void AddVarLongs<T>(this Message msg, T[] values) where T : IBinaryInteger<T> {
        msg.AddVarULong((ulong)values.Length);
        foreach(var value in values) {
            msg.AddVarLong(long.CreateChecked(value));
        }
    }

    public static T[] GetVarLongs<T>(this Message msg) where T : IBinaryInteger<T> {
        var len = (int)msg.GetVarULong();
        var array = new T[len];
        for(int i = 0; i < len; i++) {
            array[i] = T.CreateChecked(msg.GetVarLong());
        }
        return array;
    }
}
