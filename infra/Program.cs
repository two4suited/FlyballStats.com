using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using AzureNative = Pulumi.AzureNative;


return await Pulumi.Deployment.RunAsync(() =>
{
    // Create an Azure Resource Group
    var resourceGroup = new ResourceGroup("FlyballStats.com", new ResourceGroupArgs()
    {
        ResourceGroupName = "FlyballStats.com"
    });

    var staticWebApp = new AzureNative.Web.StaticSite("staticSite", new()
    {
        Location = resourceGroup.Location,
        Name = "FlyballStatscom",
        BuildProperties = new AzureNative.Web.Inputs.StaticSiteBuildPropertiesArgs
        {
            SkipGithubActionWorkflowGeneration = true
        },
        ResourceGroupName = resourceGroup.Name,
        Sku = new AzureNative.Web.Inputs.SkuDescriptionArgs
        {
            Name = "Free",
            Tier = "Free",
        },
    });

});