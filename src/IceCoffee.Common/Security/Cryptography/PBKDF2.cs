using System.Security.Cryptography;

namespace IceCoffee.Common.Security.Cryptography
{
    /// <summary>
    /// PBKDF2 不可逆加密
    /// <para>通过一个伪随机函数（例如HMAC函数）, 把明文和一个盐值作为输入参数, 然后重复进行运算, 并最终产生密钥</para>
    /// <para>Size of PBKDF2-HMAC Hash: SHA-1 为 20 字节，SHA-224 为 28 字节，SHA-256 为 32 字节，SHA-384 为 48 字节，SHA-512 为 64 字节</para>
    /// </summary>
    public static class PBKDF2
    {
    #if NET8_0_OR_GREATER
        // --- 安全参数 ---
        // 迭代次数。这个值越高，哈希越慢，暴力破解也越难。
        // OWASP 2024年推荐值：对于 PBKDF2-SHA256，至少 600,000 次。
        private const int Iterations = 600_000;

        // 盐 (Salt) 的大小。16 字节 (128 bits) 是一个安全的长度。
        private const int SaltSize = 16;

        // 哈希 (Hash) 的大小。32 字节 (256 bits) 对应 SHA-256。
        private const int HashSize = 32;

        // 使用的哈希算法。SHA-256 或 SHA-512 是现代标准。
        private static readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// 为新密码生成盐和哈希。
        /// </summary>
        /// <param name="plaintext">用户输入的明文密码。</param>
        /// <param name="hashValue">输出生成的哈希值（Base64字符串）。</param>
        /// <param name="saltBase64">输出生成的盐（Base64字符串）。</param>
        public static void HashPassword(string plaintext, out string hashValue, out string saltBase64)
        {
            // 1. 生成一个加密安全的随机盐 (Salt)
            // 必须为每个密码生成一个唯一的、新的盐
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // 2. 使用 PBKDF2 计算哈希
            // 这就是你提问的核心方法
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: plaintext,   // 原始密码
                salt: salt,           // 随机盐
                iterations: Iterations,     // 迭代次数
                hashAlgorithm: _hashAlgorithm, // 哈希算法
                outputLength: HashSize      // 期望的哈希长度
            );

            hashValue = Convert.ToBase64String(hash);
            saltBase64 = Convert.ToBase64String(salt);
        }

        /// <summary>
        /// 验证用户登录时输入的密码是否正确。
        /// </summary>
        /// <param name="plaintext">用户登录时输入的密码。</param>
        /// <param name="hashValue">从数据库中取出的、与该用户对应的哈希。</param>
        /// <param name="saltBase64">从数据库中取出的、与该用户对应的盐。</param>
        /// <returns>如果密码匹配则返回 true，否则返回 false。</returns>
        public static bool VerifyPassword(string plaintext, string hashValue, string saltBase64)
        {
            // 1. 使用完全相同的参数（相同的盐、迭代次数、算法、长度）
            //    来哈希用户本次输入的密码。
            byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
                password: Convert.FromBase64String(plaintext),
                salt: Convert.FromBase64String(saltBase64),
                iterations: Iterations,
                hashAlgorithm: _hashAlgorithm,
                outputLength: HashSize
            );

            // 2. 比较两个哈希值
            // 必须使用“恒定时间”比较，以防止“计时攻击” (Timing Attack)。
            // CryptographicOperations.FixedTimeEquals 专门用于此目的。
            //
            return Convert.ToBase64String(newHash) == hashValue;
        }
#else

        /// <summary>
        /// 使用PBKDF2加密密码
        /// <para>此实现使用 Rfc2898DeriveBytes 基于 SHA1 算法进行 1000 次迭代</para>
        /// </summary>
        /// <param name="plaintext">明文</param>
        /// <param name="hashValue">哈希值</param>
        /// <param name="saltBase64">盐</param>
        public static void HashPassword(string plaintext, out string hashValue, out string saltBase64)
        {
            byte[] salt = new byte[24];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(plaintext, salt, 1000);
            hashValue = Convert.ToBase64String(pbkdf2.GetBytes(20));// Size of PBKDF2-HMAC-SHA-1 Hash
            saltBase64 = Convert.ToBase64String(salt);
        }

        /// <summary>
        /// 使用PBKDF2验证密码
        /// <para>此实现使用 Rfc2898DeriveBytes 基于 SHA1 算法进行 1000 次迭代</para>
        /// </summary>
        /// <param name="plaintext">明文</param>
        /// <param name="hashValue">哈希值</param>
        /// <param name="saltBase64">盐</param>
        /// <returns></returns>
        public static bool VerifyPassword(string plaintext, string hashValue, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            using var pbkdf2 = new Rfc2898DeriveBytes(plaintext, salt, 1000);
            return hashValue == Convert.ToBase64String(pbkdf2.GetBytes(20)); // Size of PBKDF2-HMAC-SHA-1 Hash
        }

        private static int GetSize(HashAlgorithmName hashAlgorithm)
        {
            int size;
            if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                size = 20;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                size = 32;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                size = 48;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                size = 64;
            }
            else
            {
                throw new ArgumentException("Onlyu support SHA1/SHA256/SHA384/SHA512.");
            }

            return size;
        }
#endif
    }
}