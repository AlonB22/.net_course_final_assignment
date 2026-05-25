using Game.Server.Pages;

namespace Game.Tests;

public sealed class RegistrationInputTests
{
    [Fact]
    public void ToRequest_UsesSelectedMoveTimer()
    {
        var form = new RegistrationModel.RegistrationInputModel
        {
            ParticipantCount = 1,
            MoveTimeLimitSeconds = 15,
            Participants =
            [
                new RegistrationModel.ParticipantInputModel
                {
                    FirstName = "Dana",
                    ExternalId = 123,
                    PhoneNumber = "0501234567",
                    CountryId = 1
                }
            ]
        };

        var request = form.ToRequest();

        Assert.Equal(15, request.MoveTimeLimitSeconds);
        Assert.Single(request.Participants);
    }
}
