using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using ContosoPackagePoject;

namespace ContosoPackageProject
{
    public class PreOperationPermitCreate : PluginBase
    {

        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            base.ExecuteCDSPlugin(localcontext);

            var permitEntity = localcontext.PluginExecutionContext.InputParameters["Target"] as Entity;
            var buildSiteRef = permitEntity["contoso_buildsite"] as EntityReference;

            localcontext.Trace("Primary Entity Id: " + permitEntity.Id);
            localcontext.Trace("Build Site Entity Id: " + buildSiteRef.Id);

            string fetchString = "<fetch output-format='xml-platform' distinct='false' version='1.0' mapping='logical' aggregate='true'>" +
                "<entity name='contoso_permit'>" +
                "<attribute name='contoso_permitid' alias='Count' aggregate='count' />" +
                "<filter type='and' >" +
                "<condition attribute='contoso_buildsite' uitype='contoso_buildsite' operator='eq' value='{" + buildSiteRef.Id + "}'/>" +
                "<condition attribute='statuscode' operator='eq' value='100000000'/>" +
                "</filter>" +
                "</entity>" +
                "</fetch>";

            localcontext.Trace("Calling RetrieveMultiple for locked permits");
            var response = localcontext.OrganizationService.RetrieveMultiple(new FetchExpression(fetchString));
            int lockedPermitCount = (int)((AliasedValue)response.Entities[0]["Count"]).Value;
            localcontext.Trace("Locket Permit count : " + lockedPermitCount);

            if (lockedPermitCount > 0)
            {
                throw new InvalidPluginExecutionException("Too many locked permits for build site");
            }
        }//

    }
}
