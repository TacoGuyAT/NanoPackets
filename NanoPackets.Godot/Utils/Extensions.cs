using Godot;
using NanoPackets.Utils;
using Riptide;
using System.Runtime.CompilerServices;

namespace NanoPackets.Godot.Utils;
public static class Extensions {
    #region Vector2
    /// <inheritdoc cref="AddVector2(Message, Vector2)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddVector2(Message, Vector2)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Vector2 value) => message.AddVector2(value);

    /// <summary>Adds a <see cref="Vector2"/> to the message.</summary>
    /// <param name="value">The <see cref="Vector2"/> to add.</param>
    /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
    public static Message AddVector2(this Message message, Vector2 value) {
        return message.AddFloat(value.X).AddFloat(value.Y);
    }

    /// <summary>Retrieves a <see cref="Vector2"/> from the message.</summary>
    /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
    public static Vector2 GetVector2(this Message message) {
        return new Vector2(message.GetFloat(), message.GetFloat());
    }
    #endregion

    #region Vector3
    /// <inheritdoc cref="AddVector3(Message, Vector3)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddVector3(Message, Vector3)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Vector3 value) => message.AddVector3(value);

    /// <summary>Adds a <see cref="Vector3"/> to the message.</summary>
    /// <param name="value">The <see cref="Vector3"/> to add.</param>
    /// <returns>The message that the <see cref="Vector3"/> was added to.</returns>
    public static Message AddVector3(this Message message, Vector3 value) {
        return message.AddFloat(value.X).AddFloat(value.Y).AddFloat(value.Z);
    }

    /// <summary>Retrieves a <see cref="Vector3"/> from the message.</summary>
    /// <returns>The <see cref="Vector3"/> that was retrieved.</returns>
    public static Vector3 GetVector3(this Message message) {
        return new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
    }
    #endregion

    #region Vector2Half
    /// <summary>Adds a <see cref="Vector2"/> in range of 0..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="Vector2"/> to add.</param>
    /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
    public static Message AddVector2Half(this Message message, Vector2 value) {
        return message.AddHalf(value.X).AddHalf(value.Y);
    }

    /// <summary>Retrieves a <see cref="Vector2"/> in range of 0..=1 from the message.</summary>
    /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
    public static Vector2 GetVector2Half(this Message message) {
        return new Vector2(message.GetHalf(), message.GetHalf());
    }
    #endregion

    #region Vector3Half
    /// <summary>Adds a <see cref="Vector3"/> in range of 0..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="Vector3"/> to add.</param>
    /// <returns>The message that the <see cref="Vector3"/> was added to.</returns>
    public static Message AddVector3Half(this Message message, Vector3 value) {
        return message.AddHalf(value.X).AddHalf(value.Y).AddHalf(value.Z);
    }

    /// <summary>Retrieves a <see cref="Vector3"/> in range of 0..=1 from the message in <see cref="ushort"/> precision.</summary>
    /// <returns>The <see cref="Vector3"/> that was retrieved.</returns>
    public static Vector3 GetVector3Half(this Message message) {
        return new Vector3(message.GetHalf(), message.GetHalf(), message.GetHalf());
    }
    #endregion

    #region Vector2SHalf
    /// <summary>Adds a <see cref="Vector2"/> in range of -1..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="Vector2"/> to add.</param>
    /// <returns>The message that the <see cref="Vector2"/> was added to.</returns>
    public static Message AddVector2SHalf(this Message message, Vector2 value) {
        return message.AddSHalf(value.X).AddSHalf(value.Y);
    }

    /// <summary>Retrieves a <see cref="Vector2"/> in range of -1..=1 from the message.</summary>
    /// <returns>The <see cref="Vector2"/> that was retrieved.</returns>
    public static Vector2 GetVector2SHalf(this Message message) {
        return new Vector2(message.GetSHalf(), message.GetSHalf());
    }
    #endregion

    #region Vector3SHalf
    /// <summary>Adds a <see cref="Vector3"/> in range of -1..=1 to the message in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="Vector3"/> to add.</param>
    /// <returns>The message that the <see cref="Vector3"/> was added to.</returns>
    public static Message AddVector3SHalf(this Message message, Vector3 value) {
        return message.AddSHalf(value.X).AddSHalf(value.Y).AddSHalf(value.Z);
    }

    /// <summary>Retrieves a <see cref="Vector3"/> in range of -1..=1 from the message in <see cref="ushort"/> precision.</summary>
    /// <returns>The <see cref="Vector3"/> that was retrieved.</returns>
    public static Vector3 GetVector3SHalf(this Message message) {
        return new Vector3(message.GetSHalf(), message.GetSHalf(), message.GetSHalf());
    }
    #endregion
}
