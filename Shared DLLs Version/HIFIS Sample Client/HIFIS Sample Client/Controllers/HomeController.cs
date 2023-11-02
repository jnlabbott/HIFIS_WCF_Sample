using HIFIS.CONTRACTS.WCFContracts.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HIFIS_Sample_Client.Controllers
{
    public class HomeController : Controller
    {
        private static Authentication authenticationToken;

        private async Task UpdateAuthenticationTokenAsync(string sessionID)
        {
            if (authenticationToken == null)
            {
                var authenticationClient = new API.Authentication.AuthenticationServiceClient();
                // Get an authentication token with credentials - like the demo site credentials! (demo.hifis.ca)
                authenticationToken = await authenticationClient.ValidateUserAsync("admin", "123456", 1, "OptionalTwoFactorToken", sessionID);
                authenticationToken.CurrentCulture = "en_CA";
            }
        }

        public async Task<ActionResult> Index()
        {
            var organizationClient = new API.Organization.OrganizationServiceClient();

            // Ensure the static token in the base class is initialized:
            await UpdateAuthenticationTokenAsync(HttpContext.Session.SessionID);

            // Now use that token to make calls on the Organization endpoint:
            ListItem[] orgNames = await organizationClient.GetOrganizationListAsync(null, authenticationToken);

            return View(orgNames);
        }

        public async Task<ActionResult> Pit()
        {
            var pitEventClient = new API.PITEvent.PitEventServiceClient();

            // Create token for calling Pit Event Service
            await UpdateAuthenticationTokenAsync(HttpContext.Session.SessionID);

            // Get the list of PIT Templates (does not include the questions which keeps it a "lite" call)
            PitTemplate[] templates = await pitEventClient.GetPitTemplatesListAsync(authenticationToken, false);

            // We are interested in the questions, so let's take the extra time to get them for each template in our list...

            // Loop the list of templates that we got above:
            foreach (var template in templates)
            {
                // Call "GetPitTemplateAsync" to get the full record for this template (when you know
                // the template ID, you could just call this and get a single template and return it, 
                // it comes with the questions populated when you get one by ID):
                var fullTemplate = await pitEventClient.GetPitTemplateAsync(template.PitTemplateID, authenticationToken, false);

                // Now we copy the questions from the complete template, to the "lite" one in our list:
                template.Questions = fullTemplate.Questions;
            }

            // Return the list of templates, now with their questions included, to the view
            return View(templates);
        }
    }
}