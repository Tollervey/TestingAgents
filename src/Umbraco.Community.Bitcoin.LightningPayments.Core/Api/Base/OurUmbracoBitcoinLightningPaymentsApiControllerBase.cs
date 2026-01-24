using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base
{
    [ApiController]
    [BackOfficeRoute("ourumbracobitcoinlightningpayments/api/v{version:apiVersion}")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
    [MapToApi(Constants.ApiName)]
    public class OurUmbracoBitcoinLightningPaymentsApiControllerBase : ControllerBase
    {
    }
}



