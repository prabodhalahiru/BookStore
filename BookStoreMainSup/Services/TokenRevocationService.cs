namespace BookStoreMainSup.Services
{
    public interface ITokenRevocationService
    {
        void RevokeToken(string token);
        bool IsTokenRevoked(string token);
    }

    // This class is used to revoke tokens. It is used by the TokenRevocationController.
    public class TokenRevocationService : ITokenRevocationService
    {
        private readonly List<string> _revokedTokens = new List<string>();

        // Revoke a token
        public void RevokeToken(string token)
        {
            _revokedTokens.Add(token);
        }

        // Check if a token is revoked
        public bool IsTokenRevoked(string token)
        {
            return _revokedTokens.Contains(token);
        }
    }
}
