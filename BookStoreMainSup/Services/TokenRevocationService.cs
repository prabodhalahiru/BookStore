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

        public void RevokeToken(string token)
        {
            if (!_revokedTokens.Contains(token))
            {
                _revokedTokens.Add(token);
            }
        }

        public bool IsTokenRevoked(string token)
        {
            return _revokedTokens.Contains(token);
        }
    }

}
