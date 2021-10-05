using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.Text.RegularExpressions;
using ContosoPackagePoject;
using Microsoft.Xrm.Sdk.Query;

namespace ContosoPackageProject
{
    public class LockPermitCancelInspections : PluginBase
    {
        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            base.ExecuteCDSPlugin(localcontext);

            var permitEntityRef = localcontext.PluginExecutionContext.InputParameters["Target"] as EntityReference;
            Entity permitEntity = new Entity(permitEntityRef.LogicalName, permitEntityRef.Id);

            localcontext.Trace("Updating Permit Id : " + permitEntityRef.Id);
            permitEntity["statuscode"] = new OptionSetValue(100000000);//463270000

            localcontext.OrganizationService.Update(permitEntity);
            localcontext.Trace("Updated Permit Id " + permitEntityRef.Id);

            QueryExpression qe = new QueryExpression();
            qe.EntityName = "contoso_inspection";
            qe.ColumnSet = new ColumnSet("statuscode");

            ConditionExpression condition = new ConditionExpression();
            condition.Operator = ConditionOperator.Equal;
            condition.AttributeName = "contoso_permit";
            condition.Values.Add(permitEntityRef.Id);

            qe.Criteria = new FilterExpression(LogicalOperator.And);
            qe.Criteria.Conditions.Add(condition);

            localcontext.Trace("Retrieving inspections for Permit Id " + permitEntityRef.Id);
            var inspectionsResult = localcontext.OrganizationService.RetrieveMultiple(qe);
            localcontext.Trace("Retrievied " + inspectionsResult.TotalRecordCount + " inspection records");

            int canceledInspectionsCount = 0;
            foreach (var inspection in inspectionsResult.Entities)
            {
                var currentValue = inspection.GetAttributeValue<OptionSetValue>("statuscode");

                if (currentValue.Value == 1 || currentValue.Value == 100000000) //463270000
                {
                    canceledInspectionsCount++;
                    inspection["statuscode"] = new OptionSetValue(100000003);//463270003
                    localcontext.Trace("Canceling inspection Id : " + inspection.Id);
                    localcontext.OrganizationService.Update(inspection);
                    localcontext.Trace("Canceled inspection Id : " + inspection.Id);
                }

            }

            if (canceledInspectionsCount > 0)
            {
                localcontext.PluginExecutionContext.OutputParameters["CanceledInspectionsCount"] = canceledInspectionsCount + " Inspections were canceled";
            }

            if (localcontext.PluginExecutionContext.InputParameters.ContainsKey("Reason"))
            {

                localcontext.Trace("building a note reocord");
                Entity note = new Entity("annotation");
                note["subject"] = "Permit Locked";
                note["notetext"] = "Reason for locking this permit: " + localcontext.PluginExecutionContext.InputParameters["Reason"];
                note["objectid"] = permitEntityRef;
                note["objecttypecode"] = permitEntityRef.LogicalName;

                localcontext.Trace("Creating a note reocord");
                var createdNoteId = localcontext.OrganizationService.Create(note);

                if (createdNoteId != Guid.Empty)
                    localcontext.Trace("Note record was created");

            }//


        }
    }
}
