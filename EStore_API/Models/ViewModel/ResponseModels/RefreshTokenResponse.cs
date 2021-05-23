using EStore_API.Models.ViewModel.DTO;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class RefreshTokenResponse : ResponseBase
    {
        public string AccessToken { get; set; }
        public int AccessTokenExpireMinutes { get; set; }
        public string RefreshToken { get; set; }
        public int RefreshTokenExpireMinutes { get; set; }
    }
}
