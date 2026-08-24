using System.Security.Cryptography;
using System.Text;

namespace Catalog.Data.Seed;

internal static class DeterministicGuid
{
	public static Guid CreateVersion5(Guid namespaceId, string name)
	{
		Span<byte> namespaceBytes = stackalloc byte[16];
		_ = namespaceId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);

		byte[] nameBytes = Encoding.UTF8.GetBytes(name);
		byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
		namespaceBytes.CopyTo(input);
		nameBytes.CopyTo(input.AsSpan(namespaceBytes.Length));

		Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
		_ = SHA1.HashData(input, hash);
		hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
		hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

		return new Guid(hash[..16], bigEndian: true);
	}
}
