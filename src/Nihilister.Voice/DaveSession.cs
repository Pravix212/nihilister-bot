using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Serilog;

namespace Nihilister.Voice
{
    public enum CommitProcessResult
    {
        Success,
        Ignored,
        Failed
    }

    internal sealed class DaveSession : IDisposable
    {
        private IntPtr _session;
        private IntPtr _encryptor;
        private IntPtr _selfKeyRatchet;
        private readonly LibDave.MlsFailureCallback _failureCallback;
        private bool _disposed;

        public DaveSession()
        {
            _failureCallback = OnMlsFailure;
            _session = LibDave.SessionCreate(IntPtr.Zero, null, _failureCallback, IntPtr.Zero);
            _encryptor = LibDave.EncryptorCreate();
        }

        public bool IsValid => _session != IntPtr.Zero;

        public void Init(ushort protocolVersion, ulong groupId, string selfUserId)
        {
            if (_session == IntPtr.Zero) return;
            LibDave.SessionInit(_session, protocolVersion, groupId, selfUserId);
        }

        public void Reset()
        {
            if (_session == IntPtr.Zero) return;
            LibDave.SessionReset(_session);
        }

        public ushort GetProtocolVersion()
        {
            if (_session == IntPtr.Zero) return 0;
            return LibDave.SessionGetProtocolVersion(_session);
        }

        public void SetExternalSender(byte[] externalSender)
        {
            if (_session == IntPtr.Zero) return;
            LibDave.SessionSetExternalSender(_session, externalSender, (UIntPtr)externalSender.Length);
        }

        public byte[]? GetKeyPackage()
        {
            if (_session == IntPtr.Zero) return null;

            LibDave.SessionGetMarshalledKeyPackage(_session, out var ptr, out var length);
            if (ptr == IntPtr.Zero || (int)length == 0)
            {
                Log.Warning("DAVE Session: GetKeyPackage returned null/empty");
                return null;
            }

            var result = new byte[(int)length];
            Marshal.Copy(ptr, result, 0, result.Length);
            LibDave.Free(ptr);
            Log.Debug("DAVE Session: GetKeyPackage returned {Size} bytes", result.Length);
            return result;
        }

        public byte[]? ProcessProposals(byte[] proposals, string[] recognizedUserIds)
        {
            if (_session == IntPtr.Zero) return null;

            var userIdPtrs = MarshalStringArray(recognizedUserIds);
            try
            {
                LibDave.SessionProcessProposals(
                    _session,
                    proposals,
                    (UIntPtr)proposals.Length,
                    userIdPtrs,
                    (UIntPtr)userIdPtrs.Length,
                    out var commitWelcomePtr,
                    out var commitWelcomeLen);

                if (commitWelcomePtr == IntPtr.Zero || (int)commitWelcomeLen == 0)
                    return null;

                var result = new byte[(int)commitWelcomeLen];
                Marshal.Copy(commitWelcomePtr, result, 0, result.Length);
                LibDave.Free(commitWelcomePtr);
                return result;
            }
            finally
            {
                FreeStringArray(userIdPtrs);
            }
        }

        public CommitProcessResult ProcessCommit(byte[] commit)
        {
            if (_session == IntPtr.Zero) return CommitProcessResult.Failed;

            var result = LibDave.SessionProcessCommit(_session, commit, (UIntPtr)commit.Length);
            if (result == IntPtr.Zero)
            {
                Log.Warning("DAVE Session: ProcessCommit returned null pointer");
                return CommitProcessResult.Failed;
            }

            try
            {
                if (LibDave.CommitResultIsIgnored(result))
                {
                    Log.Information("DAVE Session: ProcessCommit result=Ignored");
                    return CommitProcessResult.Ignored;
                }
                if (LibDave.CommitResultIsFailed(result))
                {
                    Log.Warning("DAVE Session: ProcessCommit result=Failed");
                    return CommitProcessResult.Failed;
                }

                Log.Information("DAVE Session: ProcessCommit result=Success");
                return CommitProcessResult.Success;
            }
            finally
            {
                LibDave.CommitResultDestroy(result);
            }
        }

        /// <returns>True if welcome was processed successfully</returns>
        public bool ProcessWelcome(byte[] welcome, string[] recognizedUserIds)
        {
            if (_session == IntPtr.Zero) return false;

            var userIdPtrs = MarshalStringArray(recognizedUserIds);
            try
            {
                var result = LibDave.SessionProcessWelcome(
                    _session,
                    welcome,
                    (UIntPtr)welcome.Length,
                    userIdPtrs,
                    (UIntPtr)userIdPtrs.Length);

                if (result == IntPtr.Zero)
                {
                    Log.Warning("DAVE Session: ProcessWelcome failed (null result), recognizedUsers={UserCount}", recognizedUserIds.Length);
                    return false;
                }

                LibDave.WelcomeResultDestroy(result);
                Log.Information("DAVE Session: ProcessWelcome succeeded, recognizedUsers={UserCount}", recognizedUserIds.Length);
                return true;
            }
            finally
            {
                FreeStringArray(userIdPtrs);
            }
        }

        public void UpdateSelfKeyRatchet(string selfUserId)
        {
            if (_session == IntPtr.Zero || _encryptor == IntPtr.Zero) return;

            if (_selfKeyRatchet != IntPtr.Zero)
            {
                LibDave.KeyRatchetDestroy(_selfKeyRatchet);
                _selfKeyRatchet = IntPtr.Zero;
            }

            _selfKeyRatchet = LibDave.SessionGetKeyRatchet(_session, selfUserId);
            if (_selfKeyRatchet != IntPtr.Zero)
            {
                LibDave.EncryptorSetKeyRatchet(_encryptor, _selfKeyRatchet);
                LibDave.EncryptorSetPassthroughMode(_encryptor, false);
                Log.Information("DAVE Session: Key ratchet obtained and set for user={UserId}", selfUserId);
            }
            else
            {
                Log.Warning("DAVE Session: Failed to obtain key ratchet for user={UserId}", selfUserId);
            }
        }

        public void SetPassthroughMode(bool passthrough)
        {
            if (_encryptor == IntPtr.Zero) return;
            LibDave.EncryptorSetPassthroughMode(_encryptor, passthrough);
        }

        public void AssignSsrcToCodec(uint ssrc)
        {
            if (_encryptor == IntPtr.Zero) return;
            LibDave.EncryptorAssignSsrcToCodec(_encryptor, ssrc, LibDave.DAVE_CODEC_OPUS);
        }

        public bool HasKeyRatchet()
        {
            if (_encryptor == IntPtr.Zero) return false;
            return LibDave.EncryptorHasKeyRatchet(_encryptor);
        }

        public int Encrypt(uint ssrc, byte[] frame, int frameLength, byte[] output, int outputCapacity)
        {
            if (_encryptor == IntPtr.Zero) return -1;

            var result = LibDave.EncryptorEncrypt(
                _encryptor,
                LibDave.DAVE_MEDIA_TYPE_AUDIO,
                ssrc,
                frame,
                (UIntPtr)frameLength,
                output,
                (UIntPtr)outputCapacity,
                out var bytesWritten);

            if (result != LibDave.DAVE_ENCRYPTOR_RESULT_CODE_SUCCESS)
            {
                Log.Warning("DAVE Session: Encrypt failed, ssrc={Ssrc}, frameLen={FrameLength}, resultCode={ResultCode}",
                    ssrc, frameLength, result);
                return -1;
            }

            return (int)bytesWritten;
        }

        public int GetMaxCiphertextSize(int frameSize)
        {
            if (_encryptor == IntPtr.Zero) return frameSize + 128;
            return (int)LibDave.EncryptorGetMaxCiphertextByteSize(
                _encryptor, LibDave.DAVE_MEDIA_TYPE_AUDIO, (UIntPtr)frameSize);
        }

        private static void OnMlsFailure(string source, string reason, IntPtr userData)
        {
            Log.Warning("DAVE MLS failure: {Source} - {Reason}", source, reason);
        }

        private static IntPtr[] MarshalStringArray(string[] strings)
        {
            var ptrs = new IntPtr[strings.Length];
            for (int i = 0; i < strings.Length; i++)
                ptrs[i] = Marshal.StringToHGlobalAnsi(strings[i]);
            return ptrs;
        }

        private static void FreeStringArray(IntPtr[] ptrs)
        {
            for (int i = 0; i < ptrs.Length; i++)
            {
                if (ptrs[i] != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrs[i]);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_selfKeyRatchet != IntPtr.Zero)
            {
                LibDave.KeyRatchetDestroy(_selfKeyRatchet);
                _selfKeyRatchet = IntPtr.Zero;
            }

            if (_encryptor != IntPtr.Zero)
            {
                LibDave.EncryptorDestroy(_encryptor);
                _encryptor = IntPtr.Zero;
            }

            if (_session != IntPtr.Zero)
            {
                LibDave.SessionDestroy(_session);
                _session = IntPtr.Zero;
            }
        }
    }
}
