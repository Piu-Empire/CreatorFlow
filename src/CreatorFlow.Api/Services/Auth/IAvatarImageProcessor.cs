using CreatorFlow.Api.Models.Auth;

namespace CreatorFlow.Api.Services.Auth;

public interface IAvatarImageProcessor
{
    UserAvatar Normalize(byte[] input);
}
