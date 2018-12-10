using Microsoft.Extensions.DependencyInjection;
using System;

public static class SendGridExtentions
{
    public static IServiceCollection AddSendGrid(this IServiceCollection me, string apiKey = null)
    {
        if (apiKey == null)
        {
            apiKey = System.Environment.GetEnvironmentVariable("SendGrid:ApiKey");
        }

        me.AddSingleton<SendGrid.ISendGridClient>((sp) => new SendGrid.SendGridClient(apiKey));

        return me;

    }
}
