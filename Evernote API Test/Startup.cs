using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Evernote_API_Test.Startup))]
namespace Evernote_API_Test
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
