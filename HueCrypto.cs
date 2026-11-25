using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AmbiBack.PhilipsHue
{
    /// <summary>
    /// AES-GCM through Windows CNG (bcrypt.dll). .NET Framework 4.7.2 has no AesGcm class, and CNG gives
    /// a hardware accelerated, constant time implementation of the whole mode (including GHASH), which
    /// is safer than building GCM on top of AES-ECB by hand. Available since Windows Vista.
    /// One instance holds one key. Not thread safe: use one instance per thread or lock outside.
    /// </summary>
    public sealed class AesGcmCng : IDisposable
    {
        public const int TagSize = 16;
        public const int NonceSize = 12;

        private const uint STATUS_AUTH_TAG_MISMATCH = 0xC000A002;

        private SafeBCryptAlgorithmHandle _alg;
        private SafeBCryptKeyHandle _key;

        public AesGcmCng(byte[] key)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("AES key must be 16, 24 or 32 bytes.", nameof(key));

            Check(NativeMethods.BCryptOpenAlgorithmProvider(out _alg, "AES", null, 0), "BCryptOpenAlgorithmProvider");
            byte[] mode = Encoding.Unicode.GetBytes("ChainingModeGCM\0");
            Check(NativeMethods.BCryptSetProperty(_alg, "ChainingMode", mode, mode.Length, 0), "BCryptSetProperty");
            Check(NativeMethods.BCryptGenerateSymmetricKey(_alg, out _key, IntPtr.Zero, 0, key, key.Length, 0), "BCryptGenerateSymmetricKey");
        }

        /// <summary>Encrypts plaintext into ciphertext (same length) and writes the 16 byte tag.</summary>
        public unsafe void Encrypt(byte[] nonce, byte[] plaintext, int ptOffset, int length,
            byte[] ciphertext, int ctOffset, byte[] tag, int tagOffset, byte[] aad)
        {
            CheckArgs(nonce, plaintext, ptOffset, length, ciphertext, ctOffset, tag, tagOffset);
            if (_key == null)
                throw new ObjectDisposedException(nameof(AesGcmCng));

            byte[] tagBuffer = new byte[TagSize];
            fixed (byte* pNonce = nonce)
            fixed (byte* pAad = aad)
            fixed (byte* pTag = tagBuffer)
            fixed (byte* pIn = plaintext)
            fixed (byte* pOut = ciphertext)
            {
                var info = NativeMethods.AuthInfo.Create(pNonce, nonce.Length, pAad, aad?.Length ?? 0, pTag, TagSize);
                int written;
                byte* input = length == 0 ? null : pIn + ptOffset;
                byte* output = length == 0 ? null : pOut + ctOffset;
                Check(NativeMethods.BCryptEncrypt(_key, input, length, ref info, null, 0, output, length, out written, 0), "BCryptEncrypt");
            }
            Buffer.BlockCopy(tagBuffer, 0, tag, tagOffset, TagSize);
        }

        /// <summary>Decrypts and verifies. Returns false when the tag does not match (output is then cleared).</summary>
        public unsafe bool Decrypt(byte[] nonce, byte[] ciphertext, int ctOffset, int length,
            byte[] tag, int tagOffset, byte[] plaintext, int ptOffset, byte[] aad)
        {
            CheckArgs(nonce, ciphertext, ctOffset, length, plaintext, ptOffset, tag, tagOffset);
            if (_key == null)
                throw new ObjectDisposedException(nameof(AesGcmCng));

            byte[] tagBuffer = new byte[TagSize];
            Buffer.BlockCopy(tag, tagOffset, tagBuffer, 0, TagSize);
            uint status;
            fixed (byte* pNonce = nonce)
            fixed (byte* pAad = aad)
            fixed (byte* pTag = tagBuffer)
            fixed (byte* pIn = ciphertext)
            fixed (byte* pOut = plaintext)
            {
                var info = NativeMethods.AuthInfo.Create(pNonce, nonce.Length, pAad, aad?.Length ?? 0, pTag, TagSize);
                int written;
                byte* input = length == 0 ? null : pIn + ctOffset;
                byte* output = length == 0 ? null : pOut + ptOffset;
                status = NativeMethods.BCryptDecrypt(_key, input, length, ref info, null, 0, output, length, out written, 0);
            }
            if (status == STATUS_AUTH_TAG_MISMATCH)
            {
                Array.Clear(plaintext, ptOffset, length);
                return false;
            }
            Check(status, "BCryptDecrypt");
            return true;
        }

        public void Dispose()
        {
            _key?.Dispose();
            _key = null;
            _alg?.Dispose();
            _alg = null;
        }

        private static void CheckArgs(byte[] nonce, byte[] input, int inOffset, int length,
            byte[] output, int outOffset, byte[] tag, int tagOffset)
        {
            if (nonce == null || nonce.Length != NonceSize)
                throw new ArgumentException("GCM nonce must be 12 bytes.", nameof(nonce));
            if (input == null || output == null || tag == null)
                throw new ArgumentNullException(input == null ? nameof(input) : output == null ? nameof(output) : nameof(tag));
            if (length < 0 || inOffset < 0 || outOffset < 0 || tagOffset < 0
                || inOffset + length > input.Length || outOffset + length > output.Length || tagOffset + TagSize > tag.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
        }

        private static void Check(uint status, string function)
        {
            if (status != 0)
                throw new CryptographicException(function + " failed: 0x" + status.ToString("X8"));
        }

        private sealed class SafeBCryptAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeBCryptAlgorithmHandle() : base(true) { }
            protected override bool ReleaseHandle() { return NativeMethods.BCryptCloseAlgorithmProvider(handle, 0) == 0; }
        }

        private sealed class SafeBCryptKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeBCryptKeyHandle() : base(true) { }
            protected override bool ReleaseHandle() { return NativeMethods.BCryptDestroyKey(handle) == 0; }
        }

        private static class NativeMethods
        {
            /// <summary>BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO</summary>
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct AuthInfo
            {
                public int cbSize;
                public int dwInfoVersion;
                public byte* pbNonce;
                public int cbNonce;
                public byte* pbAuthData;
                public int cbAuthData;
                public byte* pbTag;
                public int cbTag;
                public byte* pbMacContext;
                public int cbMacContext;
                public int cbAAD;
                public long cbData;
                public int dwFlags;

                public static AuthInfo Create(byte* nonce, int nonceLength, byte* aad, int aadLength, byte* tag, int tagLength)
                {
                    return new AuthInfo
                    {
                        cbSize = sizeof(AuthInfo),
                        dwInfoVersion = 1,
                        pbNonce = nonce,
                        cbNonce = nonceLength,
                        pbAuthData = aadLength == 0 ? null : aad,
                        cbAuthData = aadLength,
                        pbTag = tag,
                        cbTag = tagLength,
                    };
                }
            }

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            public static extern uint BCryptOpenAlgorithmProvider(out SafeBCryptAlgorithmHandle phAlgorithm, string pszAlgId, string pszImplementation, uint dwFlags);

            [DllImport("bcrypt.dll")]
            public static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint dwFlags);

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            public static extern uint BCryptSetProperty(SafeBCryptAlgorithmHandle hObject, string pszProperty, byte[] pbInput, int cbInput, uint dwFlags);

            [DllImport("bcrypt.dll")]
            public static extern uint BCryptGenerateSymmetricKey(SafeBCryptAlgorithmHandle hAlgorithm, out SafeBCryptKeyHandle phKey,
                IntPtr pbKeyObject, int cbKeyObject, byte[] pbSecret, int cbSecret, uint dwFlags);

            [DllImport("bcrypt.dll")]
            public static extern uint BCryptDestroyKey(IntPtr hKey);

            [DllImport("bcrypt.dll")]
            public static extern unsafe uint BCryptEncrypt(SafeBCryptKeyHandle hKey, byte* pbInput, int cbInput, ref AuthInfo pPaddingInfo,
                byte* pbIV, int cbIV, byte* pbOutput, int cbOutput, out int pcbResult, uint dwFlags);

            [DllImport("bcrypt.dll")]
            public static extern unsafe uint BCryptDecrypt(SafeBCryptKeyHandle hKey, byte* pbInput, int cbInput, ref AuthInfo pPaddingInfo,
                byte* pbIV, int cbIV, byte* pbOutput, int cbOutput, out int pcbResult, uint dwFlags);
        }
    }

    /// <summary>TLS 1.2 pseudo random function with HMAC-SHA256 (RFC 5246, section 5).</summary>
    public static class TlsPrf
    {
        public static byte[] Compute(byte[] secret, string label, byte[] seed, int length)
        {
            byte[] labelBytes = Encoding.ASCII.GetBytes(label);
            byte[] labelSeed = new byte[labelBytes.Length + seed.Length];
            Buffer.BlockCopy(labelBytes, 0, labelSeed, 0, labelBytes.Length);
            Buffer.BlockCopy(seed, 0, labelSeed, labelBytes.Length, seed.Length);

            byte[] result = new byte[length];
            using (var hmac = new HMACSHA256(secret))
            {
                byte[] a = labelSeed;
                int done = 0;
                byte[] input = new byte[32 + labelSeed.Length];
                while (done < length)
                {
                    a = hmac.ComputeHash(a);                      // A(i) = HMAC(secret, A(i-1))
                    Buffer.BlockCopy(a, 0, input, 0, 32);
                    Buffer.BlockCopy(labelSeed, 0, input, 32, labelSeed.Length);
                    byte[] block = hmac.ComputeHash(input);       // HMAC(secret, A(i) + seed)
                    int n = Math.Min(block.Length, length - done);
                    Buffer.BlockCopy(block, 0, result, done, n);
                    done += n;
                }
            }
            return result;
        }

        /// <summary>Premaster secret for plain PSK (RFC 4279, section 2): N zero bytes and the PSK, each with a uint16 length.</summary>
        public static byte[] PskPremaster(byte[] psk)
        {
            int n = psk.Length;
            byte[] pms = new byte[4 + 2 * n];
            pms[0] = (byte)(n >> 8);
            pms[1] = (byte)n;
            pms[2 + n] = (byte)(n >> 8);
            pms[3 + n] = (byte)n;
            Buffer.BlockCopy(psk, 0, pms, 4 + n, n);
            return pms;
        }

        public static byte[] FromHex(string hex)
        {
            if (hex == null || hex.Length % 2 != 0)
                throw new FormatException("Invalid hex string.");
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = (byte)((HexValue(hex[2 * i]) << 4) | HexValue(hex[2 * i + 1]));
            return result;
        }

        public static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            throw new FormatException("Invalid hex string.");
        }
    }
}
