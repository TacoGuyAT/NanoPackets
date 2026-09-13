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

    #region Quaternion
    /// <inheritdoc cref="AddQuaternion(Message, Quaternion)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddQuaternion(Message, Quaternion)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Quaternion value) => message.AddQuaternion(value);

    /// <summary>Adds a <see cref="Quaternion"/> to the message.</summary>
    /// <param name="value">The <see cref="Quaternion"/> to add.</param>
    /// <returns>The message that the <see cref="Quaternion"/> was added to.</returns>
    public static Message AddQuaternion(this Message message, Quaternion value) {
        return message.AddFloat(value.X).AddFloat(value.Y).AddFloat(value.Z).AddFloat(value.W);
    }

    /// <summary>Retrieves a <see cref="Quaternion"/> from the message.</summary>
    /// <returns>The <see cref="Quaternion"/> that was retrieved.</returns>
    public static Quaternion GetQuaternion(this Message message) {
        return new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
    }
    #endregion

    #region QuaternionHalf
    /// <summary>Adds a unit <see cref="Quaternion"/> to the message with each component in <see cref="short"/> precision.</summary>
    /// <param name="value">The <see cref="Quaternion"/> to add. Must be normalized: each component lies in -1..=1.</param>
    /// <returns>The message that the <see cref="Quaternion"/> was added to.</returns>
    public static Message AddQuaternionHalf(this Message message, Quaternion value) {
        return message.AddSHalf(value.X).AddSHalf(value.Y).AddSHalf(value.Z).AddSHalf(value.W);
    }

    /// <summary>Retrieves a unit <see cref="Quaternion"/> from the message in <see cref="short"/> precision.</summary>
    /// <returns>The <see cref="Quaternion"/> that was retrieved.</returns>
    public static Quaternion GetQuaternionHalf(this Message message) {
        return new Quaternion(message.GetSHalf(), message.GetSHalf(), message.GetSHalf(), message.GetSHalf());
    }
    #endregion

    #region Basis
    /// <inheritdoc cref="AddBasis(Message, Basis)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddBasis(Message, Basis)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Basis value) => message.AddBasis(value);

    /// <summary>Adds a <see cref="Basis"/> to the message as its three column vectors.</summary>
    /// <param name="value">The <see cref="Basis"/> to add.</param>
    /// <returns>The message that the <see cref="Basis"/> was added to.</returns>
    public static Message AddBasis(this Message message, Basis value) {
        return message.AddVector3(value.X).AddVector3(value.Y).AddVector3(value.Z);
    }

    /// <summary>Retrieves a <see cref="Basis"/> from the message.</summary>
    /// <returns>The <see cref="Basis"/> that was retrieved.</returns>
    public static Basis GetBasis(this Message message) {
        return new Basis(message.GetVector3(), message.GetVector3(), message.GetVector3());
    }
    #endregion

    #region BasisHalf
    /// <summary>
    /// Adds a rotation-only <see cref="Basis"/> to the message as a quantized quaternion (see
    /// <see cref="AddQuaternionHalf(Message, Quaternion)"/>). A <see cref="Basis"/> with scale or
    /// shear cannot round-trip through this - use <see cref="AddBasis(Message, Basis)"/> instead.
    /// </summary>
    /// <param name="value">The rotation-only <see cref="Basis"/> to add.</param>
    /// <returns>The message that the <see cref="Basis"/> was added to.</returns>
    public static Message AddBasisHalf(this Message message, Basis value) {
        return message.AddQuaternionHalf(value.GetRotationQuaternion());
    }

    /// <summary>Retrieves a rotation-only <see cref="Basis"/> from the message in quantized-quaternion precision.</summary>
    /// <returns>The <see cref="Basis"/> that was retrieved.</returns>
    public static Basis GetBasisHalf(this Message message) {
        return new Basis(message.GetQuaternionHalf());
    }
    #endregion

    #region Transform3D
    /// <inheritdoc cref="AddTransform3D(Message, Transform3D)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddTransform3D(Message, Transform3D)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Transform3D value) => message.AddTransform3D(value);

    /// <summary>Adds a <see cref="Transform3D"/> to the message.</summary>
    /// <param name="value">The <see cref="Transform3D"/> to add.</param>
    /// <returns>The message that the <see cref="Transform3D"/> was added to.</returns>
    public static Message AddTransform3D(this Message message, Transform3D value) {
        return message.AddBasis(value.Basis).AddVector3(value.Origin);
    }

    /// <summary>Retrieves a <see cref="Transform3D"/> from the message.</summary>
    /// <returns>The <see cref="Transform3D"/> that was retrieved.</returns>
    public static Transform3D GetTransform3D(this Message message) {
        return new Transform3D(message.GetBasis(), message.GetVector3());
    }
    #endregion

    #region Transform3DHalf
    /// <summary>
    /// Adds a <see cref="Transform3D"/> to the message with its rotation quantized (see
    /// <see cref="AddBasisHalf(Message, Basis)"/>). The origin is not quantized - unlike a rotation
    /// or a normal, world-space position has no fixed range to quantize against.
    /// </summary>
    /// <param name="value">The <see cref="Transform3D"/> to add. Its basis must be rotation-only (no scale/shear).</param>
    /// <returns>The message that the <see cref="Transform3D"/> was added to.</returns>
    public static Message AddTransform3DHalf(this Message message, Transform3D value) {
        return message.AddBasisHalf(value.Basis).AddVector3(value.Origin);
    }

    /// <summary>Retrieves a <see cref="Transform3D"/> from the message with a quantized rotation and a full-precision origin.</summary>
    /// <returns>The <see cref="Transform3D"/> that was retrieved.</returns>
    public static Transform3D GetTransform3DHalf(this Message message) {
        return new Transform3D(message.GetBasisHalf(), message.GetVector3());
    }
    #endregion

    #region Color
    /// <inheritdoc cref="AddColor(Message, Color)"/>
    /// <remarks>This method is simply an alternative way of calling <see cref="AddColor(Message, Color)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message Add(this Message message, Color value) => message.AddColor(value);

    /// <summary>Adds a <see cref="Color"/> to the message.</summary>
    /// <param name="value">The <see cref="Color"/> to add.</param>
    /// <returns>The message that the <see cref="Color"/> was added to.</returns>
    public static Message AddColor(this Message message, Color value) {
        return message.AddFloat(value.R).AddFloat(value.G).AddFloat(value.B).AddFloat(value.A);
    }

    /// <summary>Retrieves a <see cref="Color"/> from the message.</summary>
    /// <returns>The <see cref="Color"/> that was retrieved.</returns>
    public static Color GetColor(this Message message) {
        return new Color(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
    }
    #endregion

    #region ColorHalf
    /// <summary>Adds a <see cref="Color"/> to the message with each channel in <see cref="ushort"/> precision.</summary>
    /// <param name="value">The <see cref="Color"/> to add.</param>
    /// <returns>The message that the <see cref="Color"/> was added to.</returns>
    public static Message AddColorHalf(this Message message, Color value) {
        return message.AddHalf(value.R).AddHalf(value.G).AddHalf(value.B).AddHalf(value.A);
    }

    /// <summary>Retrieves a <see cref="Color"/> from the message in <see cref="ushort"/> precision.</summary>
    /// <returns>The <see cref="Color"/> that was retrieved.</returns>
    public static Color GetColorHalf(this Message message) {
        return new Color(message.GetHalf(), message.GetHalf(), message.GetHalf(), message.GetHalf());
    }
    #endregion
}
