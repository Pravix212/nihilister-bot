using System;
using System.Runtime.InteropServices;

namespace Nihilister.Voice
{
    internal static class LibDave
    {
        private const string DAVE = "data/lib/libdave";

        [DllImport(DAVE, EntryPoint = "daveMaxSupportedProtocolVersion", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort MaxSupportedProtocolVersion();

        [DllImport(DAVE, EntryPoint = "daveFree", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Free(IntPtr ptr);

        // Session
        [DllImport(DAVE, EntryPoint = "daveSessionCreate", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SessionCreate(
            IntPtr context,
            [MarshalAs(UnmanagedType.LPStr)] string? authSessionId,
            MlsFailureCallback? callback,
            IntPtr userData);

        [DllImport(DAVE, EntryPoint = "daveSessionDestroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionDestroy(IntPtr session);

        [DllImport(DAVE, EntryPoint = "daveSessionInit", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionInit(
            IntPtr session,
            ushort version,
            ulong groupId,
            [MarshalAs(UnmanagedType.LPStr)] string selfUserId);

        [DllImport(DAVE, EntryPoint = "daveSessionReset", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionReset(IntPtr session);

        [DllImport(DAVE, EntryPoint = "daveSessionSetProtocolVersion", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionSetProtocolVersion(IntPtr session, ushort version);

        [DllImport(DAVE, EntryPoint = "daveSessionGetProtocolVersion", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort SessionGetProtocolVersion(IntPtr session);

        [DllImport(DAVE, EntryPoint = "daveSessionSetExternalSender", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionSetExternalSender(
            IntPtr session,
            byte[] externalSender,
            UIntPtr length);

        [DllImport(DAVE, EntryPoint = "daveSessionProcessProposals", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionProcessProposals(
            IntPtr session,
            byte[] proposals,
            UIntPtr length,
            IntPtr[] recognizedUserIds,
            UIntPtr recognizedUserIdsLength,
            out IntPtr commitWelcomeBytes,
            out UIntPtr commitWelcomeBytesLength);

        [DllImport(DAVE, EntryPoint = "daveSessionProcessCommit", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SessionProcessCommit(
            IntPtr session,
            byte[] commit,
            UIntPtr length);

        [DllImport(DAVE, EntryPoint = "daveSessionProcessWelcome", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SessionProcessWelcome(
            IntPtr session,
            byte[] welcome,
            UIntPtr length,
            IntPtr[] recognizedUserIds,
            UIntPtr recognizedUserIdsLength);

        [DllImport(DAVE, EntryPoint = "daveSessionGetMarshalledKeyPackage", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SessionGetMarshalledKeyPackage(
            IntPtr session,
            out IntPtr keyPackage,
            out UIntPtr length);

        [DllImport(DAVE, EntryPoint = "daveSessionGetKeyRatchet", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SessionGetKeyRatchet(
            IntPtr session,
            [MarshalAs(UnmanagedType.LPStr)] string userId);

        // Commit Result
        [DllImport(DAVE, EntryPoint = "daveCommitResultIsFailed", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CommitResultIsFailed(IntPtr commitResult);

        [DllImport(DAVE, EntryPoint = "daveCommitResultIsIgnored", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CommitResultIsIgnored(IntPtr commitResult);

        [DllImport(DAVE, EntryPoint = "daveCommitResultDestroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CommitResultDestroy(IntPtr commitResult);

        // Welcome Result
        [DllImport(DAVE, EntryPoint = "daveWelcomeResultDestroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void WelcomeResultDestroy(IntPtr welcomeResult);

        // Key Ratchet
        [DllImport(DAVE, EntryPoint = "daveKeyRatchetDestroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void KeyRatchetDestroy(IntPtr keyRatchet);

        // Encryptor
        [DllImport(DAVE, EntryPoint = "daveEncryptorCreate", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EncryptorCreate();

        [DllImport(DAVE, EntryPoint = "daveEncryptorDestroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptorDestroy(IntPtr encryptor);

        [DllImport(DAVE, EntryPoint = "daveEncryptorSetKeyRatchet", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptorSetKeyRatchet(IntPtr encryptor, IntPtr keyRatchet);

        [DllImport(DAVE, EntryPoint = "daveEncryptorSetPassthroughMode", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptorSetPassthroughMode(
            IntPtr encryptor,
            [MarshalAs(UnmanagedType.I1)] bool passthroughMode);

        [DllImport(DAVE, EntryPoint = "daveEncryptorAssignSsrcToCodec", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptorAssignSsrcToCodec(IntPtr encryptor, uint ssrc, int codecType);

        [DllImport(DAVE, EntryPoint = "daveEncryptorGetMaxCiphertextByteSize", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr EncryptorGetMaxCiphertextByteSize(IntPtr encryptor, int mediaType, UIntPtr frameSize);

        [DllImport(DAVE, EntryPoint = "daveEncryptorHasKeyRatchet", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool EncryptorHasKeyRatchet(IntPtr encryptor);

        [DllImport(DAVE, EntryPoint = "daveEncryptorEncrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EncryptorEncrypt(
            IntPtr encryptor,
            int mediaType,
            uint ssrc,
            byte[] frame,
            UIntPtr frameLength,
            byte[] encryptedFrame,
            UIntPtr encryptedFrameCapacity,
            out UIntPtr bytesWritten);

        // Logging
        [DllImport(DAVE, EntryPoint = "daveSetLogSinkCallback", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetLogSinkCallback(LogSinkCallback? callback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void MlsFailureCallback(
            [MarshalAs(UnmanagedType.LPStr)] string source,
            [MarshalAs(UnmanagedType.LPStr)] string reason,
            IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LogSinkCallback(
            int severity,
            [MarshalAs(UnmanagedType.LPStr)] string file,
            int line,
            [MarshalAs(UnmanagedType.LPStr)] string message);

        internal const int DAVE_CODEC_OPUS = 1;
        internal const int DAVE_MEDIA_TYPE_AUDIO = 0;
        internal const int DAVE_ENCRYPTOR_RESULT_CODE_SUCCESS = 0;
    }
}
