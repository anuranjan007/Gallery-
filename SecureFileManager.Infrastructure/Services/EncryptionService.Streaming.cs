using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace SecureFileManager.Infrastructure.Services
{
    public partial class EncryptionService
    {
        private static readonly byte[] HeaderMagic = new byte[] { (byte)'S',(byte)'F',(byte)'M' };

        // Format: [MAGIC(3)][VERSION(1)][chunkSize(4, little endian)] then repeated chunks
        // Each chunk is: [nonce(12)][tag(16)][cipherLength(4)][ciphertext]

        public async Task EncryptStreamAsync(Stream input, Stream output, byte[] dataKey, int chunkSize, CancellationToken cancellationToken)
        {
            // write header
            await output.WriteAsync(HeaderMagic, cancellationToken);
            await output.WriteAsync(new byte[] { 1 }, cancellationToken); // version 1
            await output.WriteAsync(BitConverter.GetBytes(chunkSize), cancellationToken);

            var buffer = new byte[chunkSize];
            int read;
            using var aes = new System.Security.Cryptography.AesGcm(dataKey);
            while ((read = await input.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)) > 0)
            {
                var plaintext = buffer.AsSpan(0, read).ToArray();
                var nonce = new byte[12];
                RandomNumberGenerator.Fill(nonce);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[16];
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
                // write nonce
                await output.WriteAsync(nonce, cancellationToken);
                // write tag
                await output.WriteAsync(tag, cancellationToken);
                // write length
                await output.WriteAsync(BitConverter.GetBytes(ciphertext.Length), cancellationToken);
                // write ciphertext
                await output.WriteAsync(ciphertext, cancellationToken);
            }
        }

        public async Task DecryptStreamAsync(Stream input, Stream output, byte[] dataKey, int chunkSize, CancellationToken cancellationToken)
        {
            // read header
            var magic = new byte[3];
            await input.ReadExactlyAsync(magic, cancellationToken);
            if (!magic.SequenceEqual(HeaderMagic)) throw new InvalidDataException("Invalid format");
            var version = new byte[1];
            await input.ReadExactlyAsync(version, cancellationToken);
            if (version[0] != 1) throw new InvalidDataException("Unsupported version");
            var chunkSizeBytes = new byte[4];
            await input.ReadExactlyAsync(chunkSizeBytes, cancellationToken);
            var readChunkSize = BitConverter.ToInt32(chunkSizeBytes, 0);

            using var aes = new System.Security.Cryptography.AesGcm(dataKey);
            var nonce = new byte[12];
            var tag = new byte[16];
            var lenBuf = new byte[4];

            while (true)
            {
                // read nonce
                var n = await input.ReadAsync(nonce, cancellationToken);
                if (n == 0) break; // EOF
                if (n < nonce.Length) throw new InvalidDataException("Unexpected EOF");
                var t = await input.ReadAsync(tag, cancellationToken);
                if (t < tag.Length) throw new InvalidDataException("Unexpected EOF");
                var l = await input.ReadExactlyAsync(lenBuf, cancellationToken);
                var cipherLen = BitConverter.ToInt32(lenBuf, 0);
                var cipher = new byte[cipherLen];
                var got = 0;
                while (got < cipherLen)
                {
                    var r = await input.ReadAsync(cipher, got, cipherLen - got, cancellationToken);
                    if (r == 0) throw new InvalidDataException("Unexpected EOF");
                    got += r;
                }
                var plain = new byte[cipherLen];
                try
                {
                    aes.Decrypt(nonce, cipher, tag, plain);
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidDataException("Authentication failed during decryption", ex);
                }
                await output.WriteAsync(plain, cancellationToken);
            }
        }
    }
}
