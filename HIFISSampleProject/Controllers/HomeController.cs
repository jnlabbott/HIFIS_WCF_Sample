using HIFIS.Organization;
using HIFISSampleProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HIFISSampleProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private HIFIS.Authentication.AuthenticationServiceClient authenticationClient;
        private static HIFIS.Authentication.Authentication authenticationToken;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            // Authenticate with the HIFIS System, this example uses the public facing
            // HIFIS Demo Site at https://demo.hifis.ca
            // See the Connected Services folder for the WCF references.
            authenticationClient = new HIFIS.Authentication.AuthenticationServiceClient();
        }

        private async Task UpdateAuthenticationTokenAsync(string sessionID)
        {
            if (authenticationToken == null)
            {
                // Get an authentication token with credentials - like the demo site credentials! (demo.hifis.ca)
                authenticationToken = await authenticationClient.ValidateUserAsync("admin", "123456", 1, "OptionalTwoFactorToken", sessionID);
            }
        }

        public async Task<IActionResult> IndexAsync()
        {
            HIFIS.Organization.OrganizationServiceClient organizationClient = new HIFIS.Organization.OrganizationServiceClient();

            // Create token for calling Organization Service
            await UpdateAuthenticationTokenAsync(HttpContext.Session.Id);
            
            // If you don't have the HIFIS libraries you will have to create an authentication token
            // for each service you want to call in the namespace for that service. You then have to
            // copy the required properties from the one in the Authentication namespace (see above)
            // that was returned from ValidateUser method.
            HIFIS.Organization.Authentication organizationToken = new HIFIS.Organization.Authentication();
            
            // Copy the token value, the current organization ID, and the username:
            organizationToken.AuthenticationToken = authenticationToken.AuthenticationToken;
            organizationToken.CurrentOrgID = authenticationToken.CurrentOrgID;
            organizationToken.userName = authenticationToken.userName;

            // Now use that token to make calls on the HIFIS.Organization endpoint:
            HIFIS.Organization.ListItem[] orgNames = await organizationClient.GetOrganizationListAsync(1, organizationToken);
            
            return View(orgNames);
        }

        public async Task<IActionResult> PitAsync()
        {
            HIFIS.PitEvent.PitEventServiceClient pitEventClient = new HIFIS.PitEvent.PitEventServiceClient();

            // Create token for calling Pit Event Service
            await UpdateAuthenticationTokenAsync(HttpContext.Session.Id);

            // Copy the properties:
            HIFIS.PitEvent.Authentication pitEventToken = new HIFIS.PitEvent.Authentication();
            pitEventToken.AuthenticationToken = authenticationToken.AuthenticationToken;
            pitEventToken.CurrentOrgID = authenticationToken.CurrentOrgID;
            pitEventToken.userName = authenticationToken.userName;

            // Additional token properties required for Pit:
            pitEventToken.userID = authenticationToken.userID;
            pitEventToken.userCache = authenticationToken.userCache;
            pitEventToken.CurrentCulture = "en_CA";

            // Get the list of PIT Templates (does not include the questions which keeps it a "lite" call)
            HIFIS.PitEvent.PitTemplate[] templates = await pitEventClient.GetPitTemplatesListAsync(pitEventToken, false);

            // We are interested in the questions, so let's take the extra time to get them for each template in our list...

            // Loop the list of templates that we got above:
            foreach (var template in templates)
            {
                // Call "GetPitTemplateAsync" to get the full record for this template (when you know
                // the template ID, you could just call this and get a single template and return it, 
                // it comes with the questions populated when you get one by ID):
                var fullTemplate = await pitEventClient.GetPitTemplateAsync(template.PitTemplateID, pitEventToken, false);
                
                // Now we copy the questions from the complete template, to the "lite" one in our list:
                template.Questions = fullTemplate.Questions;
            }

            // Return the list of templates, now with their questions included, to the view
            return View(templates);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}